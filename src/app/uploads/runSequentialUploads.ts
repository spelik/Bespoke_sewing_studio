import { ApiError } from "../../api/apiClient";
import type { UploadProgressEvent } from "../../api/uploadTransport";
import {
  createUploadItem,
  getQueueStatusLabel,
  isUploadBusy,
  reduceUploadQueue,
  type UploadItemState,
  type UploadQueueState,
} from "./uploadProgressMachine";

export interface SequentialUploadFile<TResult> {
  id: string;
  file: File;
  upload: (helpers: {
    onProgress: (event: UploadProgressEvent) => void;
    signal: AbortSignal;
  }) => Promise<TResult>;
}

export interface SequentialUploadCallbacks {
  onQueueChange?(queue: UploadQueueState): void;
  onQueueStatus?(label: string | null): void;
  onItemSuccess?(id: string, result: unknown): void;
}

export interface SequentialUploadResult<TResult> {
  results: Array<{ id: string; result: TResult }>;
  failures: Array<{ id: string; error: unknown }>;
  cancelled: boolean;
}

function assertUniqueUploadIds(files: readonly { id: string }[]): void {
  const seen = new Set<string>();
  for (const entry of files) {
    if (seen.has(entry.id)) {
      throw new Error(`Duplicate upload id: ${entry.id}`);
    }
    seen.add(entry.id);
  }
}

function invokeObserver(label: string, fn: (() => void) | undefined): void {
  if (!fn) {
    return;
  }

  try {
    fn();
  } catch (error) {
    console.error(`[runSequentialUploads] ${label} observer failed`, error);
  }
}

/**
 * Uploads files one-by-one (default) so ClamAV is not overloaded.
 * Failed items do not clear successful ones; callers can retry a single failed id.
 */
export async function runSequentialUploads<TResult>(
  files: readonly SequentialUploadFile<TResult>[],
  options: SequentialUploadCallbacks & { signal?: AbortSignal } = {},
): Promise<SequentialUploadResult<TResult>> {
  assertUniqueUploadIds(files);

  if (files.length === 0) {
    return { results: [], failures: [], cancelled: false };
  }

  let queue: UploadQueueState = {
    items: files.map((entry) => createUploadItem(entry.id, entry.file.name)),
  };
  const results: Array<{ id: string; result: TResult }> = [];
  const failures: Array<{ id: string; error: unknown }> = [];

  const publish = (next: UploadQueueState) => {
    queue = next;
    invokeObserver("onQueueChange", () => options.onQueueChange?.(queue));
    invokeObserver("onQueueStatus", () =>
      options.onQueueStatus?.(getQueueStatusLabel(queue.items)),
    );
  };

  // Register the full selection once so queue status totals are correct from file 1.
  publish(queue);

  for (const entry of files) {
    if (options.signal?.aborted) {
      const active = queue.items.find((item) => isUploadBusy(item));
      if (active) {
        publish(reduceUploadQueue(queue, { type: "CANCEL", id: active.id }));
      }
      return { results, failures, cancelled: true };
    }

    publish(
      reduceUploadQueue(queue, {
        type: "START",
        id: entry.id,
        fileName: entry.file.name,
      }),
    );

    const controller = new AbortController();
    const parentSignal = options.signal;
    const onAbort = () => {
      controller.abort();
    };
    // once:true still requires an explicit remove on early exit / finally —
    // attaching to an already-aborted signal does not invoke the callback.
    parentSignal?.addEventListener("abort", onAbort, { once: true });

    try {
      if (parentSignal?.aborted) {
        controller.abort();
        publish(reduceUploadQueue(queue, { type: "CANCEL", id: entry.id }));
        return { results, failures, cancelled: true };
      }

      let reachedScanning = false;
      const result = await entry.upload({
        signal: controller.signal,
        onProgress: (event) => {
          publish(
            reduceUploadQueue(queue, {
              type: "PROGRESS",
              id: entry.id,
              percent: event.percent,
            }),
          );
          if (event.percent >= 100 && !reachedScanning) {
            reachedScanning = true;
            publish(
              reduceUploadQueue(queue, {
                type: "TRANSFER_COMPLETE",
                id: entry.id,
              }),
            );
          }
        },
      });

      if (!reachedScanning) {
        publish(
          reduceUploadQueue(queue, {
            type: "TRANSFER_COMPLETE",
            id: entry.id,
          }),
        );
      }

      publish(reduceUploadQueue(queue, { type: "PROCESSING", id: entry.id }));
      publish(reduceUploadQueue(queue, { type: "SUCCESS", id: entry.id }));
      results.push({ id: entry.id, result });
      invokeObserver("onItemSuccess", () =>
        options.onItemSuccess?.(entry.id, result),
      );
    } catch (error) {
      if (parentSignal?.aborted || controller.signal.aborted) {
        const stillPresent = queue.items.some((item) => item.id === entry.id);
        if (stillPresent) {
          publish(reduceUploadQueue(queue, { type: "CANCEL", id: entry.id }));
        }
        return { results, failures, cancelled: true };
      }

      const message =
        error instanceof ApiError
          ? error.message
          : error instanceof Error
            ? error.message
            : "The upload could not be completed.";
      publish(
        reduceUploadQueue(queue, {
          type: "ERROR",
          id: entry.id,
          message,
        }),
      );
      failures.push({ id: entry.id, error });
    } finally {
      parentSignal?.removeEventListener("abort", onAbort);
    }
  }

  return { results, failures, cancelled: false };
}

export function findUploadItem(
  queue: UploadQueueState,
  id: string,
): UploadItemState | undefined {
  return queue.items.find((item) => item.id === id);
}
