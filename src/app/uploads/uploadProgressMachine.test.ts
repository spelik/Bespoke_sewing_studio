import { describe, expect, it, vi } from "vitest";
import { runSequentialUploads } from "./runSequentialUploads";
import {
  createEmptyUploadQueue,
  createUploadItem,
  getQueueStatusLabel,
  getUploadButtonLabel,
  isUploadBusy,
  reduceUploadQueue,
  UPLOAD_LABEL_SAVING,
  UPLOAD_LABEL_SCANNING,
  UPLOAD_LABEL_UPLOADED,
  type UploadItemState,
  type UploadQueueState,
} from "./uploadProgressMachine";

function item(
  id: string,
  phase: UploadItemState["phase"],
  fileName = `${id}.jpg`,
): UploadItemState {
  return {
    ...createUploadItem(id, fileName),
    phase,
    percent: phase === "uploading" ? 10 : phase === "idle" ? 0 : 100,
    errorMessage: phase === "error" ? "failed" : null,
  };
}

function apply(
  state: UploadQueueState,
  event: Parameters<typeof reduceUploadQueue>[1],
): UploadQueueState {
  return reduceUploadQueue(state, event);
}

describe("uploadProgressMachine labels", () => {
  it("has no mojibake in scanning/saving/uploaded labels", () => {
    const labels = [
      UPLOAD_LABEL_SCANNING,
      UPLOAD_LABEL_SAVING,
      UPLOAD_LABEL_UPLOADED,
      getUploadButtonLabel(item("a", "scanning"), "Choose"),
      getUploadButtonLabel(item("a", "processing"), "Choose"),
    ];

    for (const label of labels) {
      expect(label).not.toMatch(/вЂ|â€|\uFFFD|Р‚|Сљ/);
      expect(label.includes("\u2026") || label === UPLOAD_LABEL_UPLOADED).toBe(true);
    }

    expect(UPLOAD_LABEL_SCANNING).toBe("Scanning file\u2026");
    expect(UPLOAD_LABEL_SAVING).toBe("Saving\u2026");
  });
});

describe("uploadProgressMachine transitions", () => {
  it("moves idle -> uploading -> scanning -> success", () => {
    let state = createEmptyUploadQueue();
    state = apply(state, { type: "START", id: "a", fileName: "a.jpg" });
    expect(state.items[0]?.phase).toBe("uploading");
    state = apply(state, { type: "PROGRESS", id: "a", percent: 40 });
    expect(getUploadButtonLabel(state.items[0], "Choose")).toBe("Uploading 40%");
    state = apply(state, { type: "TRANSFER_COMPLETE", id: "a" });
    expect(state.items[0]?.phase).toBe("scanning");
    expect(getUploadButtonLabel(state.items[0], "Choose")).toBe(UPLOAD_LABEL_SCANNING);
    state = apply(state, { type: "SUCCESS", id: "a" });
    expect(state.items[0]?.phase).toBe("success");
    expect(getUploadButtonLabel(state.items[0], "Choose")).toBe(UPLOAD_LABEL_UPLOADED);
  });

  it("allows scanning -> success without processing", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "TRANSFER_COMPLETE", id: "a" });
    state = apply(state, { type: "SUCCESS", id: "a" });
    expect(state.items[0]?.phase).toBe("success");
  });

  it("allows processing -> success", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "TRANSFER_COMPLETE", id: "a" });
    state = apply(state, { type: "PROCESSING", id: "a" });
    expect(state.items[0]?.phase).toBe("processing");
    expect(getUploadButtonLabel(state.items[0], "Choose")).toBe(UPLOAD_LABEL_SAVING);
    state = apply(state, { type: "SUCCESS", id: "a" });
    expect(state.items[0]?.phase).toBe("success");
  });

  it("keeps cancelled after late SUCCESS", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "CANCEL", id: "a" });
    state = apply(state, { type: "SUCCESS", id: "a" });
    expect(state.items[0]?.phase).toBe("cancelled");
  });

  it("keeps error after late TRANSFER_COMPLETE", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "ERROR", id: "a", message: "boom" });
    state = apply(state, { type: "TRANSFER_COMPLETE", id: "a" });
    expect(state.items[0]?.phase).toBe("error");
  });

  it("keeps success after late ERROR", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "TRANSFER_COMPLETE", id: "a" });
    state = apply(state, { type: "SUCCESS", id: "a" });
    state = apply(state, { type: "ERROR", id: "a", message: "late" });
    expect(state.items[0]?.phase).toBe("success");
  });

  it("keeps cancelled after late PROGRESS", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "PROGRESS", id: "a", percent: 55 });
    state = apply(state, { type: "CANCEL", id: "a" });
    state = apply(state, { type: "PROGRESS", id: "a", percent: 90 });
    expect(state.items[0]?.phase).toBe("cancelled");
    expect(state.items[0]?.percent).toBe(55);
  });

  it("keeps error after late PROCESSING", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "ERROR", id: "a", message: "fail" });
    state = apply(state, { type: "PROCESSING", id: "a" });
    expect(state.items[0]?.phase).toBe("error");
  });

  it("allows error -> RETRY -> uploading", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "ERROR", id: "a", message: "Failed" });
    expect(isUploadBusy(state.items[0])).toBe(false);
    state = apply(state, { type: "RETRY", id: "a" });
    expect(state.items[0]?.phase).toBe("uploading");
    expect(state.items[0]?.percent).toBe(0);
  });

  it("allows cancelled -> RETRY -> uploading", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "CANCEL", id: "a" });
    state = apply(state, { type: "RETRY", id: "a" });
    expect(state.items[0]?.phase).toBe("uploading");
  });

  it("allows success/cancelled -> RESET -> idle", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "TRANSFER_COMPLETE", id: "a" });
    state = apply(state, { type: "SUCCESS", id: "a" });
    state = apply(state, { type: "RESET", id: "a" });
    expect(state.items[0]?.phase).toBe("idle");

    state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "b",
      fileName: "b.jpg",
    });
    state = apply(state, { type: "CANCEL", id: "b" });
    state = apply(state, { type: "RESET", id: "b" });
    expect(state.items[0]?.phase).toBe("idle");
  });

  it("CLEAR_ALL clears terminal and active items", () => {
    let state: UploadQueueState = {
      items: [
        item("a", "success"),
        item("b", "error"),
        item("c", "uploading"),
        item("d", "cancelled"),
      ],
    };
    state = apply(state, { type: "CLEAR_ALL" });
    expect(state.items).toEqual([]);
  });
});

describe("uploadProgressMachine queue order and status", () => {
  it("keeps stable item order when START targets an existing idle item", () => {
    let state: UploadQueueState = {
      items: [item("a", "idle"), item("b", "idle"), item("c", "idle")],
    };
    state = apply(state, { type: "START", id: "b", fileName: "b-renamed.jpg" });
    expect(state.items.map((entry) => entry.id)).toEqual(["a", "b", "c"]);
    expect(state.items[1]?.phase).toBe("uploading");
    expect(state.items[1]?.fileName).toBe("b-renamed.jpg");
  });

  it("does not move an existing non-idle item to the end on START", () => {
    let state = apply(createEmptyUploadQueue(), {
      type: "START",
      id: "a",
      fileName: "a.jpg",
    });
    state = apply(state, { type: "START", id: "b", fileName: "b.jpg" });
    state = apply(state, { type: "TRANSFER_COMPLETE", id: "a" });
    state = apply(state, { type: "SUCCESS", id: "a" });
    const before = state.items.map((entry) => entry.id);
    state = apply(state, { type: "START", id: "a", fileName: "a2.jpg" });
    expect(state.items.map((entry) => entry.id)).toEqual(before);
    expect(state.items[0]?.phase).toBe("success");
  });

  it("shows Uploading 2 of 5 after the first file failed", () => {
    const state: UploadQueueState = {
      items: [
        item("1", "error"),
        item("2", "uploading"),
        item("3", "idle"),
        item("4", "idle"),
        item("5", "idle"),
      ],
    };
    expect(getQueueStatusLabel(state.items)).toBe("Uploading 2 of 5");
  });

  it("returns null for a single active file", () => {
    expect(getQueueStatusLabel([item("only", "uploading")])).toBeNull();
  });

  it("returns null when no file is active", () => {
    expect(
      getQueueStatusLabel([item("a", "success"), item("b", "error"), item("c", "cancelled")]),
    ).toBeNull();
  });
});

describe("runSequentialUploads", () => {
  function makeFile(id: string, name = `${id}.jpg`) {
    return {
      id,
      file: new File([id], name, { type: "image/jpeg" }),
    };
  }

  it("registers all five files in queue before the first upload starts", async () => {
    const statuses: Array<string | null> = [];
    const registeredBeforeStart: UploadItemState[] = [];
    let uploadsStarted = 0;

    await runSequentialUploads(
      Array.from({ length: 5 }, (_, index) => {
        const id = String(index + 1);
        return {
          ...makeFile(id),
          upload: async ({ onProgress }) => {
            uploadsStarted += 1;
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return `ok-${id}`;
          },
        };
      }),
      {
        onQueueChange(queue) {
          if (uploadsStarted === 0 && registeredBeforeStart.length === 0) {
            registeredBeforeStart.push(
              ...queue.items.map((entry) => ({ ...entry })),
            );
          }
          statuses.push(getQueueStatusLabel(queue.items));
        },
      },
    );

    expect(registeredBeforeStart).toHaveLength(5);
    expect(registeredBeforeStart.every((entry) => entry.phase === "idle")).toBe(true);
    expect(registeredBeforeStart.map((entry) => entry.id)).toEqual([
      "1",
      "2",
      "3",
      "4",
      "5",
    ]);
    expect(statuses).toContain("Uploading 1 of 5");
  });

  it("shows Uploading 1 of 5 then Uploading 2 of 5 after the first fails", async () => {
    const statuses: Array<string | null> = [];

    await runSequentialUploads(
      Array.from({ length: 5 }, (_, index) => {
        const id = String(index + 1);
        return {
          ...makeFile(id),
          upload: async ({ onProgress }) => {
            if (id === "1") {
              throw new Error("fail-1");
            }
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return `ok-${id}`;
          },
        };
      }),
      {
        onQueueStatus(label) {
          statuses.push(label);
        },
      },
    );

    expect(statuses).toContain("Uploading 1 of 5");
    expect(statuses).toContain("Uploading 2 of 5");
    const firstOfFive = statuses.indexOf("Uploading 1 of 5");
    const secondOfFive = statuses.indexOf("Uploading 2 of 5");
    expect(firstOfFive).toBeGreaterThanOrEqual(0);
    expect(secondOfFive).toBeGreaterThan(firstOfFive);
  });

  it("keeps queue order equal to the input files order", async () => {
    let registeredIds: string[] = [];
    await runSequentialUploads(
      ["c", "a", "b"].map((id) => ({
        ...makeFile(id, `${id}.png`),
        upload: async () => id,
      })),
      {
        onQueueChange(queue) {
          if (registeredIds.length === 0) {
            registeredIds = queue.items.map((entry) => entry.id);
          }
        },
      },
    );
    expect(registeredIds).toEqual(["c", "a", "b"]);
  });

  it("starts each upload only after the previous one finishes", async () => {
    const order: string[] = [];
    let active = 0;
    let maxActive = 0;

    await runSequentialUploads(
      ["1", "2", "3"].map((id) => ({
        ...makeFile(id),
        upload: async ({ onProgress }) => {
          active += 1;
          maxActive = Math.max(maxActive, active);
          order.push(`${id}-start`);
          await Promise.resolve();
          onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
          order.push(`${id}-end`);
          active -= 1;
          return `ok-${id}`;
        },
      })),
    );

    expect(maxActive).toBe(1);
    expect(order).toEqual([
      "1-start",
      "1-end",
      "2-start",
      "2-end",
      "3-start",
      "3-end",
    ]);
  });

  it("uploads sequentially and isolates failures", async () => {
    const order: string[] = [];
    const outcome = await runSequentialUploads([
      {
        ...makeFile("1"),
        upload: async ({ onProgress }) => {
          order.push("1-start");
          onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
          order.push("1-end");
          return "ok-1";
        },
      },
      {
        ...makeFile("2"),
        upload: async () => {
          order.push("2-start");
          throw new Error("fail-2");
        },
      },
      {
        ...makeFile("3"),
        upload: async ({ onProgress }) => {
          order.push("3-start");
          onProgress({ percent: 50, loadedBytes: 1, totalBytes: 2 });
          onProgress({ percent: 100, loadedBytes: 2, totalBytes: 2 });
          order.push("3-end");
          return "ok-3";
        },
      },
    ]);

    expect(order).toEqual(["1-start", "1-end", "2-start", "3-start", "3-end"]);
    expect(outcome.results.map((entry) => entry.id)).toEqual(["1", "3"]);
    expect(outcome.failures.map((entry) => entry.id)).toEqual(["2"]);
    expect(outcome.cancelled).toBe(false);
  });

  it("does not put a successful item into failures when onItemSuccess throws", async () => {
    const outcome = await runSequentialUploads(
      [
        {
          ...makeFile("1"),
          upload: async ({ onProgress }) => {
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return "ok-1";
          },
        },
        {
          ...makeFile("2"),
          upload: async ({ onProgress }) => {
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return "ok-2";
          },
        },
      ],
      {
        onItemSuccess(id) {
          if (id === "1") {
            throw new Error("observer boom");
          }
        },
      },
    );

    expect(outcome.results.map((entry) => entry.id)).toEqual(["1", "2"]);
    expect(outcome.failures).toEqual([]);
    expect(outcome.cancelled).toBe(false);
  });

  it("never lists the same id in both results and failures", async () => {
    const outcome = await runSequentialUploads(
      [
        {
          ...makeFile("1"),
          upload: async ({ onProgress }) => {
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return "ok-1";
          },
        },
        {
          ...makeFile("2"),
          upload: async () => {
            throw new Error("fail-2");
          },
        },
      ],
      {
        onItemSuccess() {
          throw new Error("should not fail the upload");
        },
      },
    );

    const resultIds = new Set(outcome.results.map((entry) => entry.id));
    const failureIds = new Set(outcome.failures.map((entry) => entry.id));
    for (const id of resultIds) {
      expect(failureIds.has(id)).toBe(false);
    }
  });

  it("keeps uploading when onQueueChange throws", async () => {
    const outcome = await runSequentialUploads(
      [
        {
          ...makeFile("1"),
          upload: async ({ onProgress }) => {
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return "ok-1";
          },
        },
        {
          ...makeFile("2"),
          upload: async ({ onProgress }) => {
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return "ok-2";
          },
        },
      ],
      {
        onQueueChange() {
          throw new Error("queue observer boom");
        },
      },
    );

    expect(outcome.results.map((entry) => entry.id)).toEqual(["1", "2"]);
    expect(outcome.failures).toEqual([]);
  });

  it("keeps uploading when onQueueStatus throws", async () => {
    const outcome = await runSequentialUploads(
      [
        {
          ...makeFile("1"),
          upload: async ({ onProgress }) => {
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return "ok-1";
          },
        },
        {
          ...makeFile("2"),
          upload: async ({ onProgress }) => {
            onProgress({ percent: 100, loadedBytes: 1, totalBytes: 1 });
            return "ok-2";
          },
        },
      ],
      {
        onQueueStatus() {
          throw new Error("status observer boom");
        },
      },
    );

    expect(outcome.results.map((entry) => entry.id)).toEqual(["1", "2"]);
    expect(outcome.failures).toEqual([]);
  });

  it("marks active item cancelled on abort and skips remaining uploads", async () => {
    const controller = new AbortController();
    const called: string[] = [];
    let queueSnapshot: UploadItemState[] = [];
    let resolveStarted!: () => void;
    const started = new Promise<void>((resolve) => {
      resolveStarted = resolve;
    });

    const outcomePromise = runSequentialUploads(
      [
        {
          ...makeFile("1"),
          upload: async ({ signal }) => {
            called.push("1");
            resolveStarted();
            await new Promise<void>((_resolve, reject) => {
              if (signal.aborted) {
                reject(new Error("aborted"));
                return;
              }
              signal.addEventListener("abort", () => reject(new Error("aborted")));
            });
            return "never";
          },
        },
        {
          ...makeFile("2"),
          upload: async () => {
            called.push("2");
            return "never-2";
          },
        },
        {
          ...makeFile("3"),
          upload: async () => {
            called.push("3");
            return "never-3";
          },
        },
      ],
      {
        signal: controller.signal,
        onQueueChange(queue) {
          queueSnapshot = queue.items.map((entry) => ({ ...entry }));
        },
      },
    );

    await started;
    controller.abort();
    const outcome = await outcomePromise;

    expect(outcome.cancelled).toBe(true);
    expect(outcome.results).toEqual([]);
    expect(called).toEqual(["1"]);
    expect(queueSnapshot.find((entry) => entry.id === "1")?.phase).toBe("cancelled");
    expect(queueSnapshot.find((entry) => entry.id === "2")?.phase).toBe("idle");
    expect(queueSnapshot.find((entry) => entry.id === "3")?.phase).toBe("idle");
  });

  it("cancels after START when parent signal is already aborted before upload", async () => {
    const controller = new AbortController();
    const called: string[] = [];
    let queueSnapshot: UploadItemState[] = [];
    const removeSpy = vi.spyOn(controller.signal, "removeEventListener");

    const outcome = await runSequentialUploads(
      [
        {
          ...makeFile("1"),
          upload: async () => {
            called.push("1");
            return "never-1";
          },
        },
        {
          ...makeFile("2"),
          upload: async () => {
            called.push("2");
            return "never-2";
          },
        },
      ],
      {
        signal: controller.signal,
        onQueueChange(queue) {
          queueSnapshot = queue.items.map((entry) => ({ ...entry }));
          // Abort in the gap after START publish and before entry.upload —
          // addEventListener on an already-aborted signal does not fire.
          if (
            !controller.signal.aborted &&
            queue.items.some((entry) => entry.id === "1" && entry.phase === "uploading")
          ) {
            controller.abort();
          }
        },
      },
    );

    expect(outcome).toEqual({
      results: [],
      failures: [],
      cancelled: true,
    });
    expect(called).toEqual([]);
    expect(queueSnapshot.find((entry) => entry.id === "1")?.phase).toBe("cancelled");
    expect(queueSnapshot.find((entry) => entry.id === "2")?.phase).toBe("idle");
    expect(removeSpy).toHaveBeenCalledWith("abort", expect.any(Function));
    removeSpy.mockRestore();
  });

  it("returns an empty successful result for an empty files array", async () => {
    const outcome = await runSequentialUploads([]);
    expect(outcome).toEqual({
      results: [],
      failures: [],
      cancelled: false,
    });
  });

  it("rejects duplicate upload ids before starting any upload", async () => {
    let started = false;
    await expect(
      runSequentialUploads([
        {
          ...makeFile("dup"),
          upload: async () => {
            started = true;
            return "a";
          },
        },
        {
          ...makeFile("dup", "dup-2.jpg"),
          upload: async () => {
            started = true;
            return "b";
          },
        },
      ]),
    ).rejects.toThrow(/Duplicate upload id: dup/);
    expect(started).toBe(false);
  });
});
