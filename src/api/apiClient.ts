import { appConfig } from "../config/appConfig";

export interface ApiClient {
  readonly baseUrl: string;
  readonly mode: "hybrid";
  resolve<T>(mockData: T): T;
  post<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse>;
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
  async post<TRequest, TResponse>(path: string, body: TRequest) {
    const url = `${appConfig.apiBaseUrl.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
    const response = await fetch(url, {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });

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
  },
};
