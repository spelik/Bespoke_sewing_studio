import { appConfig } from "../config/appConfig";
import { getAccessToken } from "./authTokenStorage";

export interface ApiClient {
  readonly baseUrl: string;
  readonly mode: "hybrid";
  resolve<T>(mockData: T): T;
  get<TResponse>(path: string): Promise<TResponse>;
  getBlob(path: string): Promise<Blob>;
  post<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse>;
  postForm<TResponse>(path: string, body: FormData): Promise<TResponse>;
  patch<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse>;
}

interface ApiProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

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

type ApiMethod = "GET" | "POST" | "PATCH";

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
    });
  } catch {
    throw new ApiError(
      "The server could not be reached. Check that the backend is running and try again.",
      0,
    );
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
    let response: Response;
    try {
      response = await fetch(buildUrl(path), {
        headers: {
          Accept: "*/*",
          ...getAuthorizationHeaders(),
        },
      });
    } catch {
      throw new ApiError("The server could not be reached. Check that the backend is running and try again.", 0);
    }

    if (!response.ok) {
      await throwApiError(response);
    }

    return response.blob();
  },
  async post<TRequest, TResponse>(path: string, body: TRequest) {
    return request<TResponse>("POST", path, body);
  },
  async postForm<TResponse>(path: string, body: FormData) {
    let response: Response;
    try {
      response = await fetch(buildUrl(path), {
        method: "POST",
        headers: {
          Accept: "application/json",
          ...getAuthorizationHeaders(),
        },
        body,
      });
    } catch {
      throw new ApiError("The server could not be reached. Check that the backend is running and try again.", 0);
    }

    if (!response.ok) {
      await throwApiError(response);
    }

    return (await response.json()) as TResponse;
  },
  patch<TRequest, TResponse>(path: string, body: TRequest) {
    return request<TResponse>("PATCH", path, body);
  },
};
