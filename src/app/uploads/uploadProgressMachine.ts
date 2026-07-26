export type UploadPhase =
  | "idle"
  | "uploading"
  | "scanning"
  | "processing"
  | "success"
  | "error"
  | "cancelled";

export interface UploadItemState {
  id: string;
  fileName: string;
  phase: UploadPhase;
  percent: number;
  errorMessage: string | null;
}

export type UploadMachineEvent =
  | { type: "START"; id: string; fileName: string }
  | { type: "PROGRESS"; id: string; percent: number }
  | { type: "TRANSFER_COMPLETE"; id: string }
  | { type: "PROCESSING"; id: string }
  | { type: "SUCCESS"; id: string }
  | { type: "ERROR"; id: string; message: string }
  | { type: "CANCEL"; id: string }
  | { type: "RETRY"; id: string }
  | { type: "RESET"; id: string }
  | { type: "CLEAR_ALL" };

export interface UploadQueueState {
  items: UploadItemState[];
}

/** Ellipsis via unicode escape so labels stay valid UTF-8 in every editor/locale. */
export const UPLOAD_LABEL_SCANNING = "Scanning file\u2026";
export const UPLOAD_LABEL_SAVING = "Saving\u2026";
export const UPLOAD_LABEL_UPLOADED = "Uploaded";

const ACTIVE_PHASES: ReadonlySet<UploadPhase> = new Set([
  "uploading",
  "scanning",
  "processing",
]);

export function createEmptyUploadQueue(): UploadQueueState {
  return { items: [] };
}

export function createUploadItem(id: string, fileName: string): UploadItemState {
  return {
    id,
    fileName,
    phase: "idle",
    percent: 0,
    errorMessage: null,
  };
}

function clampPercent(percent: number): number {
  return Math.min(100, Math.max(0, percent));
}

function reduceItem(
  item: UploadItemState,
  event: Exclude<UploadMachineEvent, { type: "CLEAR_ALL" } | { type: "START" }>,
): UploadItemState {
  switch (event.type) {
    case "PROGRESS":
      if (item.phase !== "uploading") {
        return item;
      }
      return {
        ...item,
        percent: clampPercent(event.percent),
        errorMessage: null,
      };

    case "TRANSFER_COMPLETE":
      if (item.phase !== "uploading") {
        return item;
      }
      return {
        ...item,
        phase: "scanning",
        percent: 100,
        errorMessage: null,
      };

    case "PROCESSING":
      if (item.phase !== "scanning") {
        return item;
      }
      return {
        ...item,
        phase: "processing",
        percent: 100,
        errorMessage: null,
      };

    case "SUCCESS":
      if (item.phase !== "scanning" && item.phase !== "processing") {
        return item;
      }
      return {
        ...item,
        phase: "success",
        percent: 100,
        errorMessage: null,
      };

    case "ERROR":
      if (!ACTIVE_PHASES.has(item.phase)) {
        return item;
      }
      return {
        ...item,
        phase: "error",
        errorMessage: event.message,
      };

    case "CANCEL":
      if (!ACTIVE_PHASES.has(item.phase)) {
        return item;
      }
      return {
        ...item,
        phase: "cancelled",
        errorMessage: null,
      };

    case "RETRY":
      if (item.phase !== "error" && item.phase !== "cancelled") {
        return item;
      }
      return {
        ...item,
        phase: "uploading",
        percent: 0,
        errorMessage: null,
      };

    case "RESET":
      if (item.phase !== "success" && item.phase !== "cancelled") {
        return item;
      }
      return createUploadItem(item.id, item.fileName);

    default:
      return item;
  }
}

export function reduceUploadQueue(
  state: UploadQueueState,
  event: UploadMachineEvent,
): UploadQueueState {
  if (event.type === "CLEAR_ALL") {
    return createEmptyUploadQueue();
  }

  if (event.type === "START") {
    const existingIndex = state.items.findIndex((item) => item.id === event.id);
    if (existingIndex >= 0) {
      const existing = state.items[existingIndex]!;
      // idle -> uploading only; keep stable queue order.
      if (existing.phase !== "idle") {
        return state;
      }

      return {
        items: state.items.map((item, index) =>
          index === existingIndex
            ? {
                ...item,
                fileName: event.fileName,
                phase: "uploading",
                percent: 0,
                errorMessage: null,
              }
            : item,
        ),
      };
    }

    return {
      items: [
        ...state.items,
        {
          ...createUploadItem(event.id, event.fileName),
          phase: "uploading",
          percent: 0,
        },
      ],
    };
  }

  return {
    items: state.items.map((item) => {
      if (item.id !== event.id) {
        return item;
      }

      return reduceItem(item, event);
    }),
  };
}

export function getUploadButtonLabel(
  item: UploadItemState | null | undefined,
  idleLabel: string,
): string {
  if (!item || item.phase === "idle") {
    return idleLabel;
  }

  switch (item.phase) {
    case "uploading":
      return `Uploading ${item.percent}%`;
    case "scanning":
      return UPLOAD_LABEL_SCANNING;
    case "processing":
      return UPLOAD_LABEL_SAVING;
    case "success":
      return UPLOAD_LABEL_UPLOADED;
    case "error":
    case "cancelled":
      return idleLabel;
    default:
      return idleLabel;
  }
}

export function isUploadBusy(item: UploadItemState | null | undefined): boolean {
  if (!item) {
    return false;
  }

  return ACTIVE_PHASES.has(item.phase);
}

/**
 * Queue status uses the active item's stable index in the original items array.
 * Failed/success/cancelled predecessors count toward position; total is queue length.
 */
export function getQueueStatusLabel(items: readonly UploadItemState[]): string | null {
  const activeIndex = items.findIndex((item) => ACTIVE_PHASES.has(item.phase));
  if (activeIndex < 0) {
    return null;
  }

  const total = items.length;
  if (total <= 1) {
    return null;
  }

  return `Uploading ${activeIndex + 1} of ${total}`;
}

export function prefersReducedMotion(): boolean {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return false;
  }

  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}
