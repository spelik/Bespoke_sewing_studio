import { appConfig } from "../config/appConfig";
import { clearAccessToken, getAccessToken, setAccessToken } from "./authTokenStore";

export interface ApiClient {
  readonly baseUrl: string;
  readonly mode: "hybrid";
  resolve<T>(mockData: T): T;
  get<TResponse>(path: string): Promise<TResponse>;
  getBlob(path: string): Promise<Blob>;
  post<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse>;
  postForm<TResponse>(path: string, body: FormData): Promise<TResponse>;
  patch<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse>;
  delete<TResponse>(path: string): Promise<TResponse>;
}

interface ApiProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

interface RefreshResponse {
  accessToken: string;
}

let refreshPromise: Promise<boolean> | null = null;

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly errors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

type ApiMethod = "GET" | "POST" | "PATCH" | "DELETE";

function buildUrl(path: string): string {
  return `${appConfig.apiBaseUrl.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
}

function getAuthorizationHeaders(): Record<string, string> {
  const token = getAccessToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function throwApiError(response: Response): Promise<never> {
  const contentType = response.headers.get("content-type") ?? "";
  const responseBody = contentType.includes("json")
    ? ((await response.json()) as unknown)
    : undefined;
  const problem = responseBody as ApiProblemDetails | undefined;
  const retryAfter = response.headers.get("retry-after");
  const fallbackMessage =
    response.status === 429
      ? "Too many requests were submitted. Please wait before trying again."
      : "The request could not be completed.";
  const retryHint =
    response.status === 429 && retryAfter
      ? ` Try again in about ${retryAfter} seconds.`
      : "";
  throw new ApiError(
    `${problem?.detail ?? problem?.title ?? fallbackMessage}${retryHint}`,
    response.status,
    problem?.errors,
  );
}

async function request<TResponse>(
  method: ApiMethod,
  path: string,
  body?: unknown,
  allowRefresh = true,
): Promise<TResponse> {
  const url = buildUrl(path);

  let response: Response;
  try {
    response = await fetch(url, {
      method,
      headers: {
        Accept: "application/json",
        ...(body === undefined ? {} : { "Content-Type": "application/json" }),
        ...getAuthorizationHeaders(),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      credentials: "include",
    });
  } catch {
    throw new ApiError(
      "The server could not be reached. Check that the backend is running and try again.",
      0,
    );
  }

  const skipsRefresh = [
    "/auth/login",
    "/auth/2fa/verify",
    "/auth/refresh",
    "/auth/logout",
  ].includes(path);
  if (response.status === 401 && allowRefresh && !skipsRefresh) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      return request<TResponse>(method, path, body, false);
    }
  }

  if (!response.ok) {
    await throwApiError(response);
  }

  const contentType = response.headers.get("content-type") ?? "";
  const responseBody = contentType.includes("json")
    ? ((await response.json()) as unknown)
    : undefined;

  return responseBody as TResponse;
}

export function refreshAccessToken(): Promise<boolean> {
  if (refreshPromise) {
    return refreshPromise;
  }

  refreshPromise = fetch(buildUrl("/auth/refresh"), {
    method: "POST",
    headers: { Accept: "application/json" },
    credentials: "include",
  })
    .then(async (response) => {
      if (!response.ok) {
        clearAccessToken();
        return false;
      }
      const result = (await response.json()) as RefreshResponse;
      setAccessToken(result.accessToken);
      return true;
    })
    .catch(() => {
      clearAccessToken();
      return false;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

function assertPrototypeMode() {
  if (!appConfig.isPrototypeMode) {
    throw new Error("A real API client has not been configured yet.");
  }
}

export const apiClient: ApiClient = {
  baseUrl: appConfig.apiBaseUrl,
  mode: "hybrid",
  resolve<T>(mockData: T) {
    assertPrototypeMode();
    return mockData;
  },
  get<TResponse>(path: string) {
    return request<TResponse>("GET", path);
  },
  async getBlob(path: string) {
    return requestBlob(path);
  },
  async post<TRequest, TResponse>(path: string, body: TRequest) {
    return request<TResponse>("POST", path, body);
  },
  async postForm<TResponse>(path: string, body: FormData) {
    return requestForm<TResponse>(path, body);
  },
  patch<TRequest, TResponse>(path: string, body: TRequest) {
    return request<TResponse>("PATCH", path, body);
  },
  delete<TResponse>(path: string) {
    return request<TResponse>("DELETE", path);
  },
};

async function requestBlob(path: string, allowRefresh = true): Promise<Blob> {
  let response: Response;
  try {
    response = await fetch(buildUrl(path), {
      headers: {
        Accept: "*/*",
        ...getAuthorizationHeaders(),
      },
      credentials: "include",
    });
  } catch {
    throw new ApiError("The server could not be reached. Check that the backend is running and try again.", 0);
  }

  if (response.status === 401 && allowRefresh && getAccessToken() && await refreshAccessToken()) {
    return requestBlob(path, false);
  }

  if (!response.ok) {
    await throwApiError(response);
  }

  return response.blob();
}

async function requestForm<TResponse>(
  path: string,
  body: FormData,
  allowRefresh = true,
): Promise<TResponse> {
  let response: Response;
  try {
    response = await fetch(buildUrl(path), {
      method: "POST",
      headers: {
        Accept: "application/json",
        ...getAuthorizationHeaders(),
      },
      body,
      credentials: "include",
    });
  } catch {
    throw new ApiError("The server could not be reached. Check that the backend is running and try again.", 0);
  }

  if (response.status === 401 && allowRefresh && getAccessToken() && await refreshAccessToken()) {
    return requestForm<TResponse>(path, body, false);
  }

  if (!response.ok) {
    await throwApiError(response);
  }

  return (await response.json()) as TResponse;
}
