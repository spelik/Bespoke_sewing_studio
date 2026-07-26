import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../config/appConfig", () => ({
  appConfig: {
    apiBaseUrl: "/api",
    publicSiteUrl: null,
  },
}));

const refreshAccessToken = vi.fn();
vi.mock("./apiClient", async () => {
  const actual = await vi.importActual<typeof import("./apiClient")>("./apiClient");
  return {
    ...actual,
    refreshAccessToken: (...args: unknown[]) => refreshAccessToken(...args),
  };
});

const getAccessToken = vi.fn(() => "test-token");
vi.mock("./authTokenStore", () => ({
  getAccessToken: () => getAccessToken(),
  setAccessToken: vi.fn(),
  clearAccessToken: vi.fn(),
}));

import { ApiError } from "./apiClient";
import { __uploadTransportTestUtils, uploadWithProgress } from "./uploadTransport";

type XhrHandler = (() => void) | null;

class MockXHR {
  static instances: MockXHR[] = [];
  static openImpl: ((method: string, url: string) => void) | null = null;
  static setRequestHeaderImpl: ((key: string, value: string) => void) | null = null;
  static sendImpl: ((body?: Document | XMLHttpRequestBodyInit | null) => void) | null = null;

  upload = { onprogress: null as ((event: ProgressEvent<EventTarget>) => void) | null };
  status = 200;
  responseText = "";
  timeout = 0;
  withCredentials = false;
  onload: XhrHandler = null;
  onerror: XhrHandler = null;
  ontimeout: XhrHandler = null;
  onabort: XhrHandler = null;
  headers: Record<string, string> = {};

  constructor() {
    MockXHR.instances.push(this);
  }

  open = vi.fn((method: string, url: string) => {
    MockXHR.openImpl?.(method, url);
  });

  send = vi.fn((body?: Document | XMLHttpRequestBodyInit | null) => {
    MockXHR.sendImpl?.(body);
  });

  abort = vi.fn(() => {
    this.onabort?.();
  });

  setRequestHeader = vi.fn((key: string, value: string) => {
    MockXHR.setRequestHeaderImpl?.(key, value);
    this.headers[key] = value;
  });

  triggerProgress(loaded: number, total: number | null) {
    this.upload.onprogress?.({
      loaded,
      total: total ?? 0,
      lengthComputable: total != null,
    } as ProgressEvent<EventTarget>);
  }
}

describe("uploadTransport helpers", () => {
  it("computePercent handles 0-100 and missing total", () => {
    expect(__uploadTransportTestUtils.computePercent(0, 100)).toBe(0);
    expect(__uploadTransportTestUtils.computePercent(37, 100)).toBe(37);
    expect(__uploadTransportTestUtils.computePercent(100, 100)).toBe(100);
    expect(__uploadTransportTestUtils.computePercent(50, null)).toBe(0);
  });

  it("parseSuccessBody supports JSON and empty body", () => {
    expect(__uploadTransportTestUtils.parseSuccessBody<{ id: string }>('{"id":"1"}')).toEqual({
      id: "1",
    });
    expect(__uploadTransportTestUtils.parseSuccessBody<undefined>("")).toBeUndefined();
  });

  it("parseProblem maps ProblemDetails and ValidationProblem", () => {
    const problem = __uploadTransportTestUtils.parseProblem(
      JSON.stringify({ detail: "Nope", errors: { title: ["Required"] } }),
      400,
    );
    expect(problem).toBeInstanceOf(ApiError);
    expect(problem.message).toBe("Nope");
    expect(problem.errors).toEqual({ title: ["Required"] });
  });

  it("malformed error JSON returns fallback ApiError", () => {
    const problem = __uploadTransportTestUtils.parseProblem("{not-json", 500);
    expect(problem).toBeInstanceOf(ApiError);
    expect(problem.message).toBe("The request could not be completed.");
    expect(problem.status).toBe(500);
  });
});

describe("uploadWithProgress", () => {
  const OriginalXHR = globalThis.XMLHttpRequest;

  beforeEach(() => {
    MockXHR.instances = [];
    MockXHR.openImpl = null;
    MockXHR.setRequestHeaderImpl = null;
    MockXHR.sendImpl = null;
    refreshAccessToken.mockReset();
    getAccessToken.mockReset();
    getAccessToken.mockReturnValue("test-token");
    // @ts-expect-error test double
    globalThis.XMLHttpRequest = MockXHR;
  });

  afterEach(() => {
    globalThis.XMLHttpRequest = OriginalXHR;
  });

  it("reports progress and returns JSON", async () => {
    const onProgress = vi.fn();
    const promise = uploadWithProgress<{ id: string }>({
      path: "admin/portfolio/uploads",
      body: new FormData(),
      onProgress,
    });
    const xhr = MockXHR.instances[0]!;
    expect(xhr.open).toHaveBeenCalledWith("POST", "/api/admin/portfolio/uploads");
    expect(xhr.withCredentials).toBe(true);
    expect(xhr.setRequestHeader).toHaveBeenCalledWith("Authorization", "Bearer test-token");
    expect(xhr.setRequestHeader).not.toHaveBeenCalledWith(
      "Content-Type",
      expect.anything(),
    );

    xhr.triggerProgress(37, 100);
    xhr.status = 200;
    xhr.responseText = JSON.stringify({ id: "img-1" });
    xhr.onload?.();

    await expect(promise).resolves.toEqual({ id: "img-1" });
    expect(onProgress).toHaveBeenCalledWith({
      percent: 37,
      loadedBytes: 37,
      totalBytes: 100,
    });
    expect(xhr.upload.onprogress).toBeNull();
    expect(xhr.onload).toBeNull();
  });

  it("handles missing total without inventing percent", async () => {
    const onProgress = vi.fn();
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
      onProgress,
    });
    const xhr = MockXHR.instances[0]!;
    xhr.triggerProgress(10, null);
    xhr.status = 200;
    xhr.responseText = "{}";
    xhr.onload?.();
    await promise;
    expect(onProgress).toHaveBeenCalledWith({
      percent: 0,
      loadedBytes: 10,
      totalBytes: null,
    });
  });

  it("empty 204 response remains successful", async () => {
    const promise = uploadWithProgress<void>({
      path: "admin/in-stock/items/1/images/2",
      method: "POST",
      body: new FormData(),
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 204;
    xhr.responseText = "";
    xhr.onload?.();
    await expect(promise).resolves.toBeUndefined();
    expect(xhr.onload).toBeNull();
  });

  it("malformed successful JSON rejects and does not hang", async () => {
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 200;
    xhr.responseText = "{not-json";
    xhr.onload?.();

    await expect(promise).rejects.toMatchObject({
      message: "The server returned an unexpected response.",
      status: 200,
    });
    expect(xhr.onload).toBeNull();
    expect(xhr.onerror).toBeNull();
  });

  it("rejects ValidationProblem", async () => {
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 400;
    xhr.responseText = JSON.stringify({
      title: "One or more validation errors occurred.",
      errors: { file: ["File is required."] },
    });
    xhr.onload?.();
    await expect(promise).rejects.toMatchObject({
      message: "One or more validation errors occurred.",
      status: 400,
      errors: { file: ["File is required."] },
    });
  });

  it("malformed error JSON returns fallback ApiError", async () => {
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 502;
    xhr.responseText = "<html>bad gateway</html>";
    xhr.onload?.();
    await expect(promise).rejects.toMatchObject({
      message: "The request could not be completed.",
      status: 502,
    });
    expect(xhr.onload).toBeNull();
  });

  it("rejects network failure", async () => {
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
    });
    MockXHR.instances[0]!.onerror?.();
    await expect(promise).rejects.toMatchObject({ status: 0 });
  });

  it("rejects abort", async () => {
    const controller = new AbortController();
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
      signal: controller.signal,
    });
    controller.abort();
    await expect(promise).rejects.toMatchObject({
      message: __uploadTransportTestUtils.CANCELLED_MESSAGE,
    });
  });

  it("rejects timeout", async () => {
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
      timeoutMs: 1,
    });
    MockXHR.instances[0]!.ontimeout?.();
    await expect(promise).rejects.toMatchObject({
      message: "The upload timed out. Please try again.",
    });
  });

  it("signal already aborted does not create XHR", async () => {
    const controller = new AbortController();
    controller.abort();
    await expect(
      uploadWithProgress({
        path: "admin/portfolio/uploads",
        body: new FormData(),
        signal: controller.signal,
      }),
    ).rejects.toMatchObject({
      message: __uploadTransportTestUtils.CANCELLED_MESSAGE,
    });
    expect(MockXHR.instances).toHaveLength(0);
  });

  it("refreshAccessToken rejected finishes upload with ApiError", async () => {
    refreshAccessToken.mockRejectedValueOnce(new Error("refresh boom"));
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 401;
    xhr.responseText = JSON.stringify({ detail: "Unauthorized" });
    xhr.onload?.();

    await expect(promise).rejects.toMatchObject({
      message: "Authentication could not be refreshed. Please sign in again.",
      status: 401,
    });
    expect(MockXHR.instances).toHaveLength(1);
    expect(xhr.onload).toBeNull();
  });

  it("refresh returns false returns original 401", async () => {
    refreshAccessToken.mockResolvedValueOnce(false);
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 401;
    xhr.responseText = JSON.stringify({ detail: "Unauthorized" });
    xhr.onload?.();

    await expect(promise).rejects.toMatchObject({
      message: "Unauthorized",
      status: 401,
    });
    expect(MockXHR.instances).toHaveLength(1);
  });

  it("signal aborted during refresh does not start retry", async () => {
    let resolveRefresh: ((value: boolean) => void) | undefined;
    refreshAccessToken.mockImplementationOnce(
      () =>
        new Promise<boolean>((resolve) => {
          resolveRefresh = resolve;
        }),
    );

    const controller = new AbortController();
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
      signal: controller.signal,
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 401;
    xhr.responseText = JSON.stringify({ detail: "Unauthorized" });
    xhr.onload?.();

    await vi.waitFor(() => expect(refreshAccessToken).toHaveBeenCalledOnce());
    controller.abort();
    resolveRefresh?.(true);

    await expect(promise).rejects.toMatchObject({
      message: __uploadTransportTestUtils.CANCELLED_MESSAGE,
    });
    expect(MockXHR.instances).toHaveLength(1);
  });

  it("refresh failure after cancellation does not replace cancellation", async () => {
    let rejectRefresh: ((reason?: unknown) => void) | undefined;
    refreshAccessToken.mockImplementationOnce(
      () =>
        new Promise<boolean>((_resolve, reject) => {
          rejectRefresh = reject;
        }),
    );

    const controller = new AbortController();
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
      signal: controller.signal,
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 401;
    xhr.responseText = JSON.stringify({ detail: "Unauthorized" });
    xhr.onload?.();

    await vi.waitFor(() => expect(refreshAccessToken).toHaveBeenCalledOnce());
    controller.abort();
    rejectRefresh?.(new Error("refresh failed later"));

    await expect(promise).rejects.toMatchObject({
      message: __uploadTransportTestUtils.CANCELLED_MESSAGE,
    });
    expect(MockXHR.instances).toHaveLength(1);
  });

  it("retries only once and uses updated Bearer token", async () => {
    refreshAccessToken.mockResolvedValueOnce(true);
    getAccessToken
      .mockReturnValueOnce("token-1")
      .mockReturnValueOnce("token-2");

    const form = new FormData();
    form.append("file", new Blob(["x"]), "x.jpg");
    const promise = uploadWithProgress<{ ok: boolean }>({
      path: "admin/portfolio/uploads",
      body: form,
    });

    const first = MockXHR.instances[0]!;
    expect(first.headers.Authorization).toBe("Bearer token-1");
    first.status = 401;
    first.responseText = JSON.stringify({ detail: "Unauthorized" });
    first.onload?.();

    await vi.waitFor(() => expect(MockXHR.instances.length).toBe(2));
    const second = MockXHR.instances[1]!;
    expect(second.headers.Authorization).toBe("Bearer token-2");
    expect(second.send).toHaveBeenCalledWith(form);
    second.status = 401;
    second.responseText = JSON.stringify({ detail: "Still unauthorized" });
    second.onload?.();

    await expect(promise).rejects.toMatchObject({
      message: "Still unauthorized",
      status: 401,
    });
    expect(refreshAccessToken).toHaveBeenCalledOnce();
    expect(MockXHR.instances).toHaveLength(2);
  });

  it("FormData is unchanged and can be sent again on retry", async () => {
    refreshAccessToken.mockResolvedValueOnce(true);
    const form = new FormData();
    form.append("file", new Blob(["payload"]), "piece.jpg");
    form.append("altText", "Front");

    const promise = uploadWithProgress<{ id: string }>({
      path: "admin/portfolio/uploads",
      body: form,
    });
    const first = MockXHR.instances[0]!;
    first.status = 401;
    first.responseText = "{}";
    first.onload?.();

    await vi.waitFor(() => expect(MockXHR.instances.length).toBe(2));
    const second = MockXHR.instances[1]!;
    expect(second.send.mock.calls[0]?.[0]).toBe(form);
    expect(form.get("altText")).toBe("Front");
    expect(form.get("file")).toBeInstanceOf(Blob);

    second.status = 200;
    second.responseText = JSON.stringify({ id: "ok" });
    second.onload?.();
    await expect(promise).resolves.toEqual({ id: "ok" });
  });

  it("rejects 403 without refresh retry", async () => {
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 403;
    xhr.responseText = JSON.stringify({ detail: "Forbidden" });
    xhr.onload?.();
    await expect(promise).rejects.toMatchObject({ status: 403, message: "Forbidden" });
    expect(refreshAccessToken).not.toHaveBeenCalled();
  });

  it("xhr.open throw cleans signal listener and rejects", async () => {
    MockXHR.openImpl = () => {
      throw new Error("open failed");
    };
    const controller = new AbortController();
    const removeSpy = vi.spyOn(controller.signal, "removeEventListener");

    await expect(
      uploadWithProgress({
        path: "admin/portfolio/uploads",
        body: new FormData(),
        signal: controller.signal,
      }),
    ).rejects.toMatchObject({
      message: "The upload could not be started.",
    });

    expect(removeSpy).toHaveBeenCalled();
    const xhr = MockXHR.instances[0]!;
    expect(xhr.onload).toBeNull();
    expect(xhr.onerror).toBeNull();
  });

  it("setRequestHeader throw cleans handlers and rejects", async () => {
    MockXHR.setRequestHeaderImpl = (key) => {
      if (key === "Accept") {
        throw new Error("header failed");
      }
    };

    await expect(
      uploadWithProgress({
        path: "admin/portfolio/uploads",
        body: new FormData(),
      }),
    ).rejects.toMatchObject({
      message: "The upload could not be started.",
    });

    const xhr = MockXHR.instances[0]!;
    expect(xhr.onload).toBeNull();
    expect(xhr.onerror).toBeNull();
    expect(xhr.ontimeout).toBeNull();
    expect(xhr.onabort).toBeNull();
  });

  it("xhr.send throw cleans handlers and rejects", async () => {
    MockXHR.sendImpl = () => {
      throw new Error("send failed");
    };

    await expect(
      uploadWithProgress({
        path: "admin/portfolio/uploads",
        body: new FormData(),
      }),
    ).rejects.toMatchObject({
      message: "The upload could not be started.",
    });

    const xhr = MockXHR.instances[0]!;
    expect(xhr.onload).toBeNull();
    expect(xhr.upload.onprogress).toBeNull();
  });

  it("cleans up handlers after any result", async () => {
    const promise = uploadWithProgress({
      path: "admin/portfolio/uploads",
      body: new FormData(),
    });
    const xhr = MockXHR.instances[0]!;
    xhr.status = 200;
    xhr.responseText = "{}";
    xhr.onload?.();
    await promise;
    expect(xhr.upload.onprogress).toBeNull();
    expect(xhr.onload).toBeNull();
    expect(xhr.onerror).toBeNull();
    expect(xhr.ontimeout).toBeNull();
    expect(xhr.onabort).toBeNull();
  });
});
