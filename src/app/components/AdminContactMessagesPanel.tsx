import { useEffect, useMemo, useState } from "react";
import { Eye, LoaderCircle, Mail, Search, X } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  CONTACT_MESSAGE_STATUSES,
  getAdminContactMessage,
  getAdminContactMessages,
  updateAdminContactMessageStatus,
} from "../../api/contactMessagesApi";
import type {
  AdminContactMessageDetail,
  AdminContactMessageListItem,
  ContactMessageStatus,
} from "../types";
import { formatAdminDate } from "./adminOrderFormatting";

interface AdminContactMessagesPanelProps {
  onUnauthorized(): void;
}

type StatusFilter = "All" | ContactMessageStatus;

const STATUS_LABELS: Readonly<Record<ContactMessageStatus, string>> = {
  New: "New",
  Read: "Read",
  Replied: "Replied",
  Archived: "Archived",
};

const STATUS_COLORS: Readonly<Record<ContactMessageStatus, string>> = {
  New: "bg-rose-100 text-rose-700",
  Read: "bg-blue-100 text-blue-700",
  Replied: "bg-emerald-100 text-emerald-700",
  Archived: "bg-slate-100 text-slate-700",
};

export function AdminContactMessagesPanel({ onUnauthorized }: AdminContactMessagesPanelProps) {
  const [messages, setMessages] = useState<AdminContactMessageListItem[]>([]);
  const [selectedMessage, setSelectedMessage] = useState<AdminContactMessageDetail | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [searchQuery, setSearchQuery] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void loadMessages();
  }, []);

  const filteredMessages = useMemo(() => {
    const normalizedQuery = normalizeSearchValue(searchQuery);
    return messages.filter((item) => {
      if (statusFilter !== "All" && item.status !== statusFilter) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      return [
        item.referenceNumber,
        item.fullName,
        item.email,
        item.phone,
        item.subject,
        item.messagePreview,
      ]
        .map((value) => normalizeSearchValue(value))
        .some((value) => value.includes(normalizedQuery));
    });
  }, [messages, searchQuery, statusFilter]);

  async function loadMessages() {
    setIsLoading(true);
    setError(null);
    try {
      setMessages(await getAdminContactMessages());
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setIsLoading(false);
    }
  }

  async function selectMessage(id: string) {
    setIsDetailLoading(true);
    setError(null);
    setMessage(null);
    try {
      setSelectedMessage(await getAdminContactMessage(id));
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setIsDetailLoading(false);
    }
  }

  async function changeSelectedStatus(status: ContactMessageStatus) {
    if (!selectedMessage) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);
    try {
      const saved = await updateAdminContactMessageStatus(selectedMessage.id, status);
      setSelectedMessage(saved);
      setMessages((current) =>
        current.map((item) =>
          item.id === saved.id
            ? {
                ...item,
                fullName: saved.fullName,
                referenceNumber: saved.referenceNumber,
                email: saved.email,
                phone: saved.phone,
                subject: saved.subject,
                messagePreview: toPreview(saved.message),
                status: saved.status,
                updatedAt: saved.updatedAt,
              }
            : item,
        ),
      );
      setMessage(`Contact message marked as ${STATUS_LABELS[saved.status]}.`);
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setIsSaving(false);
    }
  }

  function handleRequestError(reason: unknown) {
    if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) {
      onUnauthorized();
      return;
    }

    setError(getErrorMessage(reason));
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <p className="text-[11px] text-muted-foreground font-sans">
            Review messages submitted through the public Contact page.
          </p>
          <p className="text-[10px] text-muted-foreground/80 font-sans">
            Contact messages are separate from order enquiries and can be marked as read, replied or archived.
          </p>
        </div>
        <button
          type="button"
          onClick={() => void loadMessages()}
          disabled={isLoading}
          className="px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50 font-sans"
        >
          Refresh
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
          Status
          <select
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value as StatusFilter)}
            className="ml-3 px-3 py-2 text-[10px] border border-border bg-background focus:outline-none focus:border-accent"
          >
            <option value="All">All statuses</option>
            {CONTACT_MESSAGE_STATUSES.map((status) => (
              <option key={status} value={status}>{STATUS_LABELS[status]}</option>
            ))}
          </select>
        </label>
        <div className="relative w-full sm:w-[320px]">
          <Search size={13} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
          <input
            type="text"
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Search reference, sender, email, subject..."
            className="w-full border border-border bg-background pl-8 pr-8 py-2 text-[10px] font-sans focus:outline-none focus:border-accent"
            aria-label="Search contact messages"
          />
          {searchQuery ? (
            <button
              type="button"
              onClick={() => setSearchQuery("")}
              className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-muted-foreground hover:text-foreground"
              aria-label="Clear contact message search"
            >
              <X size={12} />
            </button>
          ) : null}
        </div>
        <span className="text-[10px] text-muted-foreground font-sans">
          {filteredMessages.length} visible / {messages.length} total
        </span>
      </div>

      {error ? <p role="alert" className="border border-destructive/30 bg-card px-4 py-3 text-[11px] text-destructive">{error}</p> : null}
      {message ? <p role="status" className="border border-emerald-300 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700">{message}</p> : null}

      <ContactMessagesTable
        messages={filteredMessages}
        isLoading={isLoading}
        onSelect={(id) => void selectMessage(id)}
      />

      <ContactMessageDetailDrawer
        message={selectedMessage}
        isLoading={isDetailLoading}
        isSaving={isSaving}
        onClose={() => setSelectedMessage(null)}
        onStatusChange={changeSelectedStatus}
      />
    </div>
  );
}

function ContactMessagesTable({
  messages,
  isLoading,
  onSelect,
}: {
  messages: AdminContactMessageListItem[];
  isLoading: boolean;
  onSelect(id: string): void;
}) {
  return (
    <div className="bg-card border border-border overflow-x-auto">
      <table className="w-full min-w-[900px]">
        <thead>
          <tr className="border-b border-border bg-secondary/40">
            {["Sender", "Contact", "Subject", "Message", "Created", "Status", ""].map((heading) => (
              <th
                key={heading || "actions"}
                className="px-5 py-3 text-left text-[10px] tracking-wider text-muted-foreground font-sans font-normal"
              >
                {heading}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {isLoading ? (
            <tr>
              <td colSpan={7} className="px-5 py-10 text-center text-[11px] text-muted-foreground">
                Loading contact messages...
              </td>
            </tr>
          ) : null}
          {!isLoading && messages.length === 0 ? (
            <tr>
              <td colSpan={7} className="px-5 py-10 text-center text-[11px] text-muted-foreground">
                No contact messages match this status or search.
              </td>
            </tr>
          ) : null}
          {!isLoading
            ? messages.map((item) => (
                <tr key={item.id} className="border-b border-border/40 hover:bg-secondary/25 transition-colors">
                  <td className="px-5 py-3.5 text-[12px] text-foreground font-sans">
                    <div className="flex items-center gap-2">
                      {item.status === "New" ? <Mail size={12} className="text-accent" /> : null}
                      <span>{item.fullName}</span>
                    </div>
                    <div className="text-[9px] text-muted-foreground font-mono mt-0.5">
                      {item.referenceNumber}
                    </div>
                  </td>
                  <td className="px-5 py-3.5 text-[10px] text-muted-foreground font-sans max-w-[180px]">
                    <div className="truncate">{item.email}</div>
                    <div className="truncate mt-0.5">{item.phone ?? "No phone"}</div>
                  </td>
                  <td className="px-5 py-3.5 text-[11px] text-muted-foreground font-sans max-w-[180px]">
                    <span className="line-clamp-2">{item.subject ?? "No subject"}</span>
                  </td>
                  <td className="px-5 py-3.5 text-[10px] text-muted-foreground font-sans max-w-[260px]">
                    <p className="line-clamp-2">{item.messagePreview}</p>
                  </td>
                  <td className="px-5 py-3.5 text-[10px] text-muted-foreground font-sans whitespace-nowrap">
                    {formatAdminDate(item.createdAt)}
                  </td>
                  <td className="px-5 py-3.5">
                    <StatusBadge status={item.status} />
                  </td>
                  <td className="px-5 py-3.5">
                    <button
                      type="button"
                      onClick={() => onSelect(item.id)}
                      className="inline-flex items-center gap-1.5 text-[10px] text-muted-foreground hover:text-foreground transition-colors"
                      aria-label={`View contact message ${item.referenceNumber} from ${item.fullName}`}
                    >
                      <Eye size={13} /> View
                    </button>
                  </td>
                </tr>
              ))
            : null}
        </tbody>
      </table>
    </div>
  );
}

function ContactMessageDetailDrawer({
  message,
  isLoading,
  isSaving,
  onClose,
  onStatusChange,
}: {
  message: AdminContactMessageDetail | null;
  isLoading: boolean;
  isSaving: boolean;
  onClose(): void;
  onStatusChange(status: ContactMessageStatus): Promise<void>;
}) {
  if (!message && !isLoading) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-[70] bg-foreground/30 flex justify-end" role="presentation">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close contact message details"
      />
      <aside
        className="relative w-full max-w-xl h-full bg-[#F5F0E8] border-l border-border overflow-y-auto shadow-2xl"
        aria-label="Contact message details"
      >
        <div className="sticky top-0 z-10 bg-[#F5F0E8]/95 backdrop-blur border-b border-border px-6 py-4 flex items-center justify-between">
          <div>
            <p className="text-[9px] tracking-[0.2em] uppercase text-muted-foreground">{message?.referenceNumber ?? "Contact message"}</p>
            <h2 className="font-serif text-[1.35rem] font-light mt-1">
              {message?.fullName ?? "Loading..."}
            </h2>
          </div>
          <button type="button" onClick={onClose} className="p-2 text-muted-foreground hover:text-foreground" aria-label="Close">
            <X size={17} />
          </button>
        </div>

        {isLoading || !message ? (
          <div className="min-h-[320px] flex items-center justify-center text-muted-foreground">
            <LoaderCircle size={20} className="animate-spin" />
          </div>
        ) : (
          <div className="p-6 space-y-6">
            <section className="bg-card border border-border p-5 grid grid-cols-1 sm:grid-cols-2 gap-4 text-[11px]">
              <Detail label="Reference" value={message.referenceNumber} />
              <Detail label="Email" value={message.email} />
              <Detail label="Phone" value={message.phone ?? "Not provided"} />
              <Detail label="Subject" value={message.subject ?? "Not provided"} />
              <Detail label="Created" value={formatAdminDate(message.createdAt)} />
              <Detail label="Updated" value={formatAdminDate(message.updatedAt)} />
              <Detail label="Consent" value={message.consentGiven ? "Given" : "Not given"} />
            </section>

            <section className="bg-card border border-border p-5">
              <label htmlFor="contact-message-status" className="block text-[10px] tracking-wider uppercase text-muted-foreground mb-2">
                Status
              </label>
              <select
                id="contact-message-status"
                value={message.status}
                disabled={isSaving}
                onChange={(event) => void onStatusChange(event.target.value as ContactMessageStatus)}
                className="w-full border border-border bg-background px-3 py-2.5 text-[12px] focus:outline-none focus:border-accent disabled:opacity-50"
              >
                {CONTACT_MESSAGE_STATUSES.map((status) => (
                  <option key={status} value={status}>{STATUS_LABELS[status]}</option>
                ))}
              </select>
            </section>

            <section className="bg-card border border-border p-5">
              <h3 className="text-[10px] tracking-wider uppercase text-muted-foreground mb-3">Message</h3>
              <p className="text-[12px] leading-6 text-foreground whitespace-pre-wrap">{message.message}</p>
            </section>

            <section className="bg-card border border-border p-5">
              <h3 className="text-[10px] tracking-wider uppercase text-muted-foreground mb-3">Privacy consent</h3>
              <p className="text-[11px] leading-5 text-muted-foreground">
                Consent was {message.consentGiven ? "given" : "not recorded"}
                {message.consentRecordedAt ? ` on ${formatAdminDate(message.consentRecordedAt)}.` : "."}
              </p>
            </section>
          </div>
        )}
      </aside>
    </div>
  );
}

function StatusBadge({ status }: { status: ContactMessageStatus }) {
  return (
    <span className={`text-[10px] px-2 py-0.5 whitespace-nowrap font-sans ${STATUS_COLORS[status]}`}>
      {STATUS_LABELS[status]}
    </span>
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

function toPreview(value: string): string {
  const trimmed = value.trim();
  return trimmed.length <= 180 ? trimmed : `${trimmed.slice(0, 180)}…`;
}

function getErrorMessage(reason: unknown): string {
  if (reason instanceof ApiError) {
    const validationMessages = reason.errors ? Object.values(reason.errors).flat() : [];
    if (validationMessages.length > 0) {
      return validationMessages.join(" ");
    }

    if (reason.status === 403) {
      return "Administrator authorization is required to manage contact messages.";
    }

    return reason.message;
  }

  return "The contact messages request could not be completed.";
}


function normalizeSearchValue(value: string | null | undefined): string {
  return (value ?? "").trim().toLocaleLowerCase();
}
