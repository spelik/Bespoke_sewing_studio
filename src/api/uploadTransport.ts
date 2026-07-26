import { appConfig } from "../config/appConfig";
import { getAccessToken } from "./authTokenStore";
import { ApiError, refreshAccessToken } from "./apiClient";

export type UploadHttpMethod = "POST" | "PUT" | "PATCH";

export interface UploadProgressEvent {
  percent: number;
  loadedBytes: number;
  totalBytes: number | null;
}

export interface UploadTransportOptions {
  path: string;
  method?: UploadHttpMethod;
  body: FormData;
  onProgress?: (event: UploadProgressEvent) => void;
  signal?: AbortSignal;
  timeoutMs?: number;
  /** When false, skip 401 refresh retry (internal). */
  allowRefresh?: boolean;
}

interface ApiProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

const DEFAULT_TIMEOUT_MS = 120_000;
const CANCELLED_MESSAGE = "The upload was cancelled.";
const AUTH_SKIP_REFRESH_PATHS = new Set([
  "/auth/login",
  "/auth/2fa/verify",
  "/auth/refresh",
  "/auth/logout",
]);

function buildUrl(path: string): string {
  return `${appConfig.apiBaseUrl.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
}

function computePercent(loaded: number, total: number | null): number {
  if (total == null || total <= 0) {
    return 0;
  }

  return Math.min(100, Math.max(0, Math.round((loaded / total) * 100)));
}

function parseProblem(responseText: string, status: number): ApiError {
  let problem: ApiProblemDetails | undefined;
  if (responseText) {
    try {
      problem = JSON.parse(responseText) as ApiProblemDetails;
    } catch {
      problem = undefined;
    }
  }

  const fallbackMessage =
    status === 429
      ? "Too many requests were submitted. Please wait before trying again."
      : status === 0
        ? "The server could not be reached. Check that the backend is running and try again."
        : "The request could not be completed.";

  return new ApiError(
    problem?.detail ?? problem?.title ?? fallbackMessage,
    status,
    problem?.errors,
  );
}

function parseSuccessBody<TResponse>(responseText: string): TResponse {
  const trimmed = responseText.trim();
  if (!trimmed) {
    return undefined as TResponse;
  }

  return JSON.parse(trimmed) as TResponse;
}

function cancelledError(): ApiError {
  return new ApiError(CANCELLED_MESSAGE, 0);
}

function toApiError(error: unknown, fallbackMessage: string, status = 0): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  return new ApiError(fallbackMessage, status);
}

/**
 * Multipart upload transport with real browser upload progress via XMLHttpRequest.
 * Does not contain business/API endpoint knowledge - callers pass path + FormData.
 */
export function uploadWithProgress<TResponse>(
  options: UploadTransportOptions,
): Promise<TResponse> {
  if (options.signal?.aborted) {
    return Promise.reject(cancelledError());
  }

  const method = options.method ?? "POST";
  const allowRefresh = options.allowRefresh !== false;
  const url = buildUrl(options.path);
  const timeoutMs = options.timeoutMs ?? DEFAULT_TIMEOUT_MS;

  return new Promise<TResponse>((resolve, reject) => {
    let xhr: XMLHttpRequest;
    try {
      xhr = new XMLHttpRequest();
    } catch (error) {
      reject(
        toApiError(error, "The upload could not be started."),
      );
      return;
    }

    let settled = false;
    let abortedBySignal = false;
    let refreshInFlight = false;
    let signalListenerAttached = false;

    const isCancelled = () =>
      abortedBySignal || Boolean(options.signal?.aborted);

    const onAbortSignal = () => {
      abortedBySignal = true;
      if (refreshInFlight) {
        fail(cancelledError());
        return;
      }

      try {
        xhr.abort();
      } catch {
        fail(cancelledError());
      }
    };

    const cleanup = () => {
      xhr.upload.onprogress = null;
      xhr.onload = null;
      xhr.onerror = null;
      xhr.ontimeout = null;
      xhr.onabort = null;
      if (signalListenerAttached) {
        options.signal?.removeEventListener("abort", onAbortSignal);
        signalListenerAttached = false;
      }
    };

    const fail = (error: unknown) => {
      if (settled) {
        return;
      }
      settled = true;
      cleanup();
      reject(
        toApiError(error, "The upload could not be completed."),
      );
    };

    const succeed = (value: TResponse) => {
      if (settled) {
        return;
      }
      settled = true;
      cleanup();
      resolve(value);
    };

    const detachXhrHandlers = () => {
      xhr.upload.onprogress = null;
      xhr.onload = null;
      xhr.onerror = null;
      xhr.ontimeout = null;
      xhr.onabort = null;
    };

    const handleUnauthorizedRefresh = async (responseText: string) => {
      refreshInFlight = true;
      detachXhrHandlers();

      try {
        if (isCancelled()) {
          fail(cancelledError());
          return;
        }

        let refreshed: boolean;
        try {
          refreshed = await refreshAccessToken();
        } catch (error) {
          if (isCancelled()) {
            fail(cancelledError());
            return;
          }

          fail(
            toApiError(
              error,
              "Authentication could not be refreshed. Please sign in again.",
              401,
            ),
          );
          return;
        }

        if (isCancelled()) {
          fail(cancelledError());
          return;
        }

        if (!refreshed) {
          fail(parseProblem(responseText, 401));
          return;
        }

        if (settled) {
          return;
        }

        // This attempt is finished; hand off to a single retry with the same FormData.
        settled = true;
        cleanup();

        try {
          const retryResult = await uploadWithProgress<TResponse>({
            ...options,
            allowRefresh: false,
          });
          resolve(retryResult);
        } catch (error) {
          reject(toApiError(error, "The upload could not be completed."));
        }
      } finally {
        refreshInFlight = false;
      }
    };

    try {
      if (options.signal) {
        if (options.signal.aborted) {
          fail(cancelledError());
          return;
        }
        options.signal.addEventListener("abort", onAbortSignal);
        signalListenerAttached = true;
      }

      xhr.open(method, url);
      xhr.withCredentials = true;
      xhr.timeout = timeoutMs;
      // Do not set Content-Type for FormData; the browser must add the multipart boundary.
      xhr.setRequestHeader("Accept", "application/json");

      const token = getAccessToken();
      if (token) {
        xhr.setRequestHeader("Authorization", `Bearer ${token}`);
      }

      xhr.upload.onprogress = (event) => {
        if (!options.onProgress || settled) {
          return;
        }

        const totalBytes = event.lengthComputable ? event.total : null;
        options.onProgress({
          percent: computePercent(event.loaded, totalBytes),
          loadedBytes: event.loaded,
          totalBytes,
        });
      };

      xhr.onload = () => {
        const status = xhr.status;
        const responseText = xhr.responseText ?? "";

        if (
          status === 401 &&
          allowRefresh &&
          !AUTH_SKIP_REFRESH_PATHS.has(options.path)
        ) {
          void handleUnauthorizedRefresh(responseText);
          return;
        }

        if (status < 200 || status >= 300) {
          fail(parseProblem(responseText, status));
          return;
        }

        try {
          const parsed = parseSuccessBody<TResponse>(responseText);
          succeed(parsed);
        } catch {
          fail(
            new ApiError(
              "The server returned an unexpected response.",
              status,
            ),
          );
        }
      };

      xhr.onerror = () => {
        fail(
          new ApiError(
            "The server could not be reached. Check that the backend is running and try again.",
            0,
          ),
        );
      };

      xhr.ontimeout = () => {
        fail(new ApiError("The upload timed out. Please try again.", 0));
      };

      xhr.onabort = () => {
        fail(
          new ApiError(
            abortedBySignal ? CANCELLED_MESSAGE : "The upload was aborted.",
            0,
          ),
        );
      };

      xhr.send(options.body);
    } catch (error) {
      fail(toApiError(error, "The upload could not be started."));
    }
  });
}

export const __uploadTransportTestUtils = {
  computePercent,
  parseProblem,
  parseSuccessBody,
  buildUrl,
  CANCELLED_MESSAGE,
};
