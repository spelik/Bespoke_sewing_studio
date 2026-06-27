import { type FormEvent, useEffect, useState } from "react";
import { LoaderCircle, X } from "lucide-react";
import {
  ORDER_STATUSES,
  type AdminOrderDetail as AdminOrderDetailModel,
  type AdminOrderStatus,
} from "../../api/ordersApi";
import {
  ADMIN_STATUS_LABELS,
  formatAdminDate,
  formatServiceType,
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
            <p className="text-[9px] tracking-[0.2em] uppercase text-muted-foreground">Enquiry details</p>
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
              <Detail label="Email" value={order.client.email ?? "Not provided"} />
              <Detail label="Phone" value={order.client.phone ?? "Not provided"} />
              <Detail label="Service" value={formatServiceType(order.serviceType)} />
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
                <p className="text-[11px] text-muted-foreground">Uploads not connected yet.</p>
              ) : (
                <ul className="space-y-2">
                  {order.attachments.map((attachment) => (
                    <li key={attachment.id} className="text-[11px] text-foreground">
                      {attachment.originalFileName}
                    </li>
                  ))}
                </ul>
              )}
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
