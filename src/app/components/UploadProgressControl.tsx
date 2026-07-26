import { useId, useRef, type ChangeEvent, type ReactNode } from "react";
import {
  getUploadButtonLabel,
  isUploadBusy,
  prefersReducedMotion,
  UPLOAD_LABEL_UPLOADED,
  type UploadItemState,
  type UploadPhase,
} from "../uploads/uploadProgressMachine";

/** Phase-only announcements; percent stays on progressbar, not aria-live. */
const PHASE_LIVE_LABEL: Record<UploadPhase, string> = {
  idle: "",
  uploading: "Uploading file",
  scanning: "Scanning file",
  processing: "Saving file",
  success: UPLOAD_LABEL_UPLOADED,
  error: "Upload failed",
  cancelled: "Upload cancelled",
};

export function UploadProgressControl({
  idleLabel = "Choose image",
  accept = "image/jpeg,image/png,image/webp",
  multiple = false,
  disabled = false,
  item,
  queueStatus,
  errorText,
  onFilesSelected,
  onRetry,
  className = "",
  icon,
}: {
  idleLabel?: string;
  accept?: string;
  multiple?: boolean;
  disabled?: boolean;
  item?: UploadItemState | null;
  queueStatus?: string | null;
  errorText?: string | null;
  onFilesSelected(files: File[]): void;
  onRetry?(): void;
  className?: string;
  icon?: ReactNode;
}) {
  const inputId = useId();
  const inputRef = useRef<HTMLInputElement | null>(null);
  const busy = isUploadBusy(item);
  const label = getUploadButtonLabel(item, idleLabel);
  const percent = item?.phase === "uploading" ? item.percent : item?.phase === "scanning" || item?.phase === "processing" || item?.phase === "success" ? 100 : 0;
  const showProgressBar = item?.phase === "uploading";
  const showPulse =
    (item?.phase === "scanning" || item?.phase === "processing") &&
    !prefersReducedMotion();
  const phase = item?.phase ?? "idle";
  const liveMessage = PHASE_LIVE_LABEL[phase];

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(event.target.files ?? []);
    event.target.value = "";
    if (files.length > 0) {
      onFilesSelected(files);
    }
  }

  return (
    <div className={`space-y-2 ${className}`}>
      {queueStatus ? (
        <p className="text-[10px] text-muted-foreground font-sans">
          {queueStatus}
        </p>
      ) : null}

      <div className="flex flex-wrap items-center gap-2">
        <label
          htmlFor={inputId}
          className={`relative inline-flex min-w-[11rem] overflow-hidden border border-border bg-background px-4 py-2.5 text-[10px] font-sans ${
            busy || disabled
              ? "cursor-not-allowed opacity-80"
              : "cursor-pointer hover:border-foreground"
          }`}
        >
          {showProgressBar || showPulse || item?.phase === "success" ? (
            <span
              aria-hidden="true"
              className={`absolute inset-y-0 left-0 bg-emerald-200/80 ${
                showPulse ? "upload-progress-pulse w-full" : ""
              }`}
              style={
                showProgressBar || item?.phase === "success"
                  ? { width: `${percent}%` }
                  : undefined
              }
            />
          ) : null}

          <span className="relative z-[1] inline-flex items-center gap-2 text-foreground">
            {icon}
            <span>{label}</span>
          </span>

          {showProgressBar ? (
            <span
              className="sr-only"
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={percent}
              aria-label={`Upload progress ${percent} percent`}
            />
          ) : null}

          <input
            id={inputId}
            ref={inputRef}
            type="file"
            accept={accept}
            multiple={multiple}
            disabled={disabled || busy}
            onChange={handleChange}
            className="sr-only"
          />
        </label>

        {item?.phase === "error" && onRetry ? (
          <button
            type="button"
            onClick={onRetry}
            className="border border-border px-3 py-2 text-[10px] font-sans hover:border-foreground"
          >
            Retry
          </button>
        ) : null}
      </div>

      {/* Keyed by phase so percent ticks do not spam screen readers. */}
      <div className="sr-only" aria-live="polite" key={phase}>
        {liveMessage}
      </div>

      {(errorText || item?.errorMessage) && item?.phase === "error" ? (
        <p role="alert" className="text-[11px] text-destructive font-sans">
          {errorText ?? item.errorMessage}
        </p>
      ) : null}

      <style>{`
        @keyframes upload-progress-pulse {
          0%, 100% { opacity: 0.55; }
          50% { opacity: 0.95; }
        }
        .upload-progress-pulse {
          animation: upload-progress-pulse 1.2s ease-in-out infinite;
        }
        @media (prefers-reduced-motion: reduce) {
          .upload-progress-pulse {
            animation: none;
          }
        }
      `}</style>
    </div>
  );
}
