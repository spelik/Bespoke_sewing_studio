import { appConfig } from "../config/appConfig";
import { getAccessToken } from "./authTokenStorage";

export interface ApiClient {
  readonly baseUrl: string;
  readonly mode: "hybrid";
  resolve<T>(mockData: T): T;
  get<TResponse>(path: string): Promise<TResponse>;
  post<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse>;
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

async function request<TResponse>(
  method: ApiMethod,
  path: string,
  body?: unknown,
): Promise<TResponse> {
  const url = `${appConfig.apiBaseUrl.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
  const token = getAccessToken();

  let response: Response;
  try {
    response = await fetch(url, {
      method,
      headers: {
        Accept: "application/json",
        ...(body === undefined ? {} : { "Content-Type": "application/json" }),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(
      "The server could not be reached. Check that the backend is running and try again.",
      0,
    );
  }

  const contentType = response.headers.get("content-type") ?? "";
  const responseBody = contentType.includes("json")
    ? ((await response.json()) as unknown)
    : undefined;

  if (!response.ok) {
    const problem = responseBody as ApiProblemDetails | undefined;
    throw new ApiError(
      problem?.detail ?? problem?.title ?? "The request could not be completed.",
      response.status,
      problem?.errors,
    );
  }

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
  async post<TRequest, TResponse>(path: string, body: TRequest) {
    return request<TResponse>("POST", path, body);
  },
  patch<TRequest, TResponse>(path: string, body: TRequest) {
    return request<TResponse>("PATCH", path, body);
  },
};
