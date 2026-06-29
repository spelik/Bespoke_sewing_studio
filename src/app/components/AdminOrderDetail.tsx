import { type FormEvent, useEffect, useState } from "react";
import { Download, FileText, Image as ImageIcon, LoaderCircle, ShieldCheck, ShieldAlert, X } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  getAdminAttachmentFile,
  ORDER_STATUSES,
  type AdminOrderDetail as AdminOrderDetailModel,
  type AdminOrderStatus,
} from "../../api/ordersApi";
import {
  ADMIN_STATUS_LABELS,
  formatAdminDate,
} from "./adminOrderFormatting";

interface AdminOrderDetailProps {
  order: AdminOrderDetailModel | null;
  isLoading: boolean;
  isSaving: boolean;
  onClose(): void;
  onStatusChange(status: AdminOrderStatus): Promise<void>;
  onAddNote(text: string): Promise<boolean>;
}

export function AdminOrderDetail({
  order,
  isLoading,
  isSaving,
  onClose,
  onStatusChange,
  onAddNote,
}: AdminOrderDetailProps) {
  const [note, setNote] = useState("");
  const [downloadingAttachmentId, setDownloadingAttachmentId] = useState<string | null>(null);
  const [attachmentError, setAttachmentError] = useState<string | null>(null);

  useEffect(() => {
    setNote("");
  }, [order?.id]);

  if (!order && !isLoading) {
    return null;
  }

  async function handleNoteSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await onAddNote(note)) {
      setNote("");
    }
  }

  async function handleAttachmentDownload(uploadedFileId: string, originalFileName: string) {
    setDownloadingAttachmentId(uploadedFileId);
    setAttachmentError(null);
    try {
      const blob = await getAdminAttachmentFile(uploadedFileId);
      const objectUrl = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = objectUrl;
      link.download = originalFileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    } catch (error) {
      setAttachmentError(
        error instanceof ApiError
          ? error.message
          : "The attachment could not be downloaded.",
      );
    } finally {
      setDownloadingAttachmentId(null);
    }
  }

  return (
    <div className="fixed inset-0 z-[70] bg-foreground/30 flex justify-end" role="presentation">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close enquiry details"
      />
      <aside
        className="relative w-full max-w-xl h-full bg-[#F5F0E8] border-l border-border overflow-y-auto shadow-2xl"
        aria-label="Enquiry details"
      >
        <div className="sticky top-0 z-10 bg-[#F5F0E8]/95 backdrop-blur border-b border-border px-6 py-4 flex items-center justify-between">
          <div>
            <p className="text-[9px] tracking-[0.2em] uppercase text-muted-foreground">{order?.referenceNumber ?? "Enquiry details"}</p>
            <h2 className="font-serif text-[1.35rem] font-light mt-1">
              {order?.client.fullName ?? "Loading..."}
            </h2>
          </div>
          <button type="button" onClick={onClose} className="p-2 text-muted-foreground hover:text-foreground" aria-label="Close">
            <X size={17} />
          </button>
        </div>

        {isLoading || !order ? (
          <div className="min-h-[320px] flex items-center justify-center text-muted-foreground">
            <LoaderCircle size={20} className="animate-spin" />
          </div>
        ) : (
          <div className="p-6 space-y-6">
            <section className="bg-card border border-border p-5 grid grid-cols-1 sm:grid-cols-2 gap-4 text-[11px]">
              <Detail label="Reference" value={order.referenceNumber} />
              <Detail label="Email" value={order.client.email ?? "Not provided"} />
              <Detail label="Phone" value={order.client.phone ?? "Not provided"} />
              <Detail label="Service" value={order.serviceName} />
              <Detail label="Created" value={formatAdminDate(order.createdAt)} />
              <Detail label="Updated" value={formatAdminDate(order.updatedAt)} />
              <Detail label="Preferred date" value={order.preferredDate ?? "Not specified"} />
            </section>

            <section className="bg-card border border-border p-5">
              <label htmlFor="order-status" className="block text-[10px] tracking-wider uppercase text-muted-foreground mb-2">
                Status
              </label>
              <select
                id="order-status"
                value={order.status}
                disabled={isSaving}
                onChange={(event) => void onStatusChange(event.target.value as AdminOrderStatus)}
                className="w-full border border-border bg-background px-3 py-2.5 text-[12px] focus:outline-none focus:border-accent disabled:opacity-50"
              >
                {ORDER_STATUSES.map((status) => (
                  <option key={status} value={status}>
                    {ADMIN_STATUS_LABELS[status]}
                  </option>
                ))}
              </select>
            </section>

            <section className="bg-card border border-border p-5">
              <h3 className="text-[10px] tracking-wider uppercase text-muted-foreground mb-3">Client message</h3>
              <p className="text-[12px] leading-6 text-foreground whitespace-pre-wrap">{order.description}</p>
            </section>

            <section className="bg-card border border-border p-5">
              <h3 className="text-[10px] tracking-wider uppercase text-muted-foreground mb-3">Attachments</h3>
              {order.attachments.length === 0 ? (
                <p className="text-[11px] text-muted-foreground">No attachments were included with this enquiry.</p>
              ) : (
                <div className="grid grid-cols-1 gap-3">
                  {order.attachments.map((attachment) => (
                    <AdminAttachmentCard
                      key={attachment.id}
                      attachment={attachment}
                      isDownloading={downloadingAttachmentId === attachment.uploadedFileId}
                      onDownload={() => void handleAttachmentDownload(
                        attachment.uploadedFileId,
                        attachment.originalFileName,
                      )}
                    />
                  ))}
                </div>
              )}
              {attachmentError ? (
                <p role="alert" className="text-[10px] text-destructive mt-3">{attachmentError}</p>
              ) : null}
            </section>

            <section className="bg-card border border-border p-5">
              <h3 className="text-[10px] tracking-wider uppercase text-muted-foreground mb-4">Internal notes</h3>
              {order.notes.length === 0 ? (
                <p className="text-[11px] text-muted-foreground mb-4">No internal notes yet.</p>
              ) : (
                <div className="space-y-3 mb-5">
                  {order.notes.map((item) => (
                    <div key={item.id} className="border-l-2 border-accent/40 pl-3">
                      <p className="text-[11px] text-foreground whitespace-pre-wrap">{item.text}</p>
                      <p className="text-[9px] text-muted-foreground mt-1">{formatAdminDate(item.createdAt)}</p>
                    </div>
                  ))}
                </div>
              )}

              <form onSubmit={handleNoteSubmit} className="space-y-3">
                <label htmlFor="internal-note" className="sr-only">New internal note</label>
                <textarea
                  id="internal-note"
                  rows={3}
                  required
                  value={note}
                  onChange={(event) => setNote(event.target.value)}
                  placeholder="Add an internal note..."
                  className="w-full border border-border bg-background px-3 py-2.5 text-[11px] focus:outline-none focus:border-accent resize-none"
                />
                <button
                  type="submit"
                  disabled={isSaving || !note.trim()}
                  className="bg-foreground text-primary-foreground px-4 py-2.5 text-[10px] tracking-wide hover:bg-accent disabled:opacity-50 transition-colors"
                >
                  {isSaving ? "Saving..." : "Add note"}
                </button>
              </form>
            </section>
          </div>
        )}
      </aside>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-[9px] tracking-wider uppercase text-muted-foreground mb-1">{label}</div>
      <div className="text-foreground break-words">{value}</div>
    </div>
  );
}

function getScanStatusLabel(attachment: AdminOrderDetailModel["attachments"][number]): string {
  if (attachment.scanStatus === "Clean") {
    return `Security scan completed${attachment.scanProvider ? ` · ${attachment.scanProvider}` : ""}`;
  }

  if (attachment.scanStatus === "Skipped") {
    return "Security scan not configured for this upload";
  }

  if (attachment.scanStatus === "Pending") {
    return "Security scan pending";
  }

  return "Security scan did not pass";
}

function isPositiveScanStatus(status: AdminOrderDetailModel["attachments"][number]["scanStatus"]): boolean {
  return status === "Clean" || status === "Skipped";
}

function formatFileSize(sizeBytes: number): string {
  return `${(sizeBytes / 1024).toFixed(1)} KB`;
}

function AdminAttachmentCard({
  attachment,
  isDownloading,
  onDownload,
}: {
  attachment: AdminOrderDetailModel["attachments"][number];
  isDownloading: boolean;
  onDownload(): void;
}) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [previewFailed, setPreviewFailed] = useState(false);
  const isImage = attachment.contentType.startsWith("image/");

  useEffect(() => {
    if (!isImage) {
      setPreviewUrl(null);
      setIsPreviewLoading(false);
      setPreviewFailed(false);
      return undefined;
    }

    let isCancelled = false;
    let objectUrl: string | null = null;

    setPreviewUrl(null);
    setPreviewFailed(false);
    setIsPreviewLoading(true);

    void getAdminAttachmentFile(attachment.uploadedFileId)
      .then((blob) => {
        if (isCancelled) {
          return;
        }
        objectUrl = URL.createObjectURL(blob);
        setPreviewUrl(objectUrl);
      })
      .catch(() => {
        if (!isCancelled) {
          setPreviewFailed(true);
        }
      })
      .finally(() => {
        if (!isCancelled) {
          setIsPreviewLoading(false);
        }
      });

    return () => {
      isCancelled = true;
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [attachment.uploadedFileId, isImage]);

  return (
    <article className="border border-border bg-background p-3 text-[11px] text-foreground">
      <div className="flex items-start gap-3">
        <div className="h-20 w-20 shrink-0 overflow-hidden border border-border bg-secondary/50 flex items-center justify-center">
          {previewUrl ? (
            <img
              src={previewUrl}
              alt={`Preview of ${attachment.originalFileName}`}
              className="h-full w-full object-cover"
            />
          ) : isPreviewLoading ? (
            <LoaderCircle size={16} className="animate-spin text-muted-foreground/70" aria-label="Loading preview" />
          ) : attachment.contentType === "application/pdf" ? (
            <FileText size={20} className="text-muted-foreground/70" aria-hidden="true" />
          ) : (
            <ImageIcon size={20} className="text-muted-foreground/70" aria-hidden="true" />
          )}
        </div>
        <div className="min-w-0 flex-1">
          <h4 className="truncate text-[12px] text-foreground">{attachment.originalFileName}</h4>
          <p className="mt-1 text-[9px] text-muted-foreground">
            {attachment.contentType} &middot; {formatFileSize(attachment.sizeBytes)}
          </p>
          <p className="mt-2 inline-flex items-center gap-1.5 text-[9px] text-muted-foreground">
            {isPositiveScanStatus(attachment.scanStatus) ? (
              <ShieldCheck size={11} className="text-accent" aria-hidden="true" />
            ) : (
              <ShieldAlert size={11} className="text-destructive" aria-hidden="true" />
            )}
            {getScanStatusLabel(attachment)}
          </p>
          {attachment.scannedAt ? (
            <p className="mt-1 text-[9px] text-muted-foreground/70">
              Checked {formatAdminDate(attachment.scannedAt)}
            </p>
          ) : null}
          {previewFailed ? (
            <p className="mt-2 text-[9px] text-muted-foreground">Preview unavailable. Download the file to view it.</p>
          ) : null}
          <button
            type="button"
            disabled={isDownloading}
            onClick={onDownload}
            className="mt-3 inline-flex items-center gap-1.5 text-[10px] text-accent hover:text-foreground disabled:opacity-50 transition-colors"
          >
            <Download size={12} />
            {isDownloading ? "Downloading..." : "Download"}
          </button>
        </div>
      </div>
    </article>
  );
}
