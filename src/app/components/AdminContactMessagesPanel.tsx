import { useCallback, useEffect, useRef, useState } from "react";
import { Download, Eye, LoaderCircle, Mail, Trash2, X } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  CONTACT_MESSAGE_STATUSES,
  deleteAdminContactMessage,
  getAdminContactMessage,
  getAdminContactMessages,
  updateAdminContactMessageStatus,
} from "../../api/contactMessagesApi";
import type {
  AdminContactMessageDetail,
  AdminContactMessageListItem,
  ContactMessageStatus,
} from "../types";
import type { AdminPageSize } from "../../api/pagination";
import { createCsvFileName, downloadCsv } from "../utils/csvExport";
import {
  AdminActionButton,
  AdminConfirmDialog,
  AdminFilterDropdown,
  AdminServerPagination,
  AdminSearchInput,
  AdminTableState,
  type AdminFilterOption,
} from "./AdminUi";
import { formatAdminDate } from "./adminOrderFormatting";

interface AdminContactMessagesPanelProps {
  onUnauthorized(): void;
  attentionCounts: { newCount: number; totalCount: number } | null;
  onDataChanged?(): void;
  realtimeRefreshKey?: number;
}

type StatusFilter = "All" | ContactMessageStatus;

const STATUS_LABELS: Readonly<Record<ContactMessageStatus, string>> = {
  New: "New",
  Read: "Read",
  Replied: "Replied",
  Archived: "Archived",
};


const STATUS_FILTER_OPTIONS: AdminFilterOption[] = [
  { value: "All", label: "All statuses" },
  ...CONTACT_MESSAGE_STATUSES.map((status) => ({
    value: status,
    label: STATUS_LABELS[status],
  })),
];

const STATUS_COLORS: Readonly<Record<ContactMessageStatus, string>> = {
  New: "bg-rose-100 text-rose-700",
  Read: "bg-blue-100 text-blue-700",
  Replied: "bg-emerald-100 text-emerald-700",
  Archived: "bg-slate-100 text-slate-700",
};

export function AdminContactMessagesPanel({
  onUnauthorized,
  attentionCounts,
  onDataChanged,
  realtimeRefreshKey = 0,
}: AdminContactMessagesPanelProps) {
  const [messages, setMessages] = useState<AdminContactMessageListItem[]>([]);
  const [selectedMessage, setSelectedMessage] =
    useState<AdminContactMessageDetail | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearchQuery, setDebouncedSearchQuery] = useState("");
  const [statusFilterOpen, setStatusFilterOpen] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState<AdminPageSize>(25);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [deleteCandidate, setDeleteCandidate] = useState<AdminContactMessageListItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [deletingMessageId, setDeletingMessageId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const latestRequestIdRef = useRef(0);

  const handleRequestError = useCallback(
    (reason: unknown) => {
      if (
        reason instanceof ApiError &&
        (reason.status === 401 || reason.status === 403)
      ) {
        onUnauthorized();
        return;
      }

      setError(getErrorMessage(reason));
    },
    [onUnauthorized],
  );

  const loadMessages = useCallback(async () => {
    const requestId = ++latestRequestIdRef.current;
    setIsLoading(true);
    setError(null);
    try {
      const result = await getAdminContactMessages({
        page: currentPage,
        pageSize,
        search: debouncedSearchQuery.trim() || undefined,
        status: statusFilter === "All" ? undefined : statusFilter,
      });
      if (requestId !== latestRequestIdRef.current) {
        return;
      }

      setMessages(result.items);
      setTotalItems(result.totalItems);
      setTotalPages(result.totalPages);
      if (result.page > result.totalPages) {
        setCurrentPage(result.totalPages);
      }
    } catch (reason: unknown) {
      if (requestId !== latestRequestIdRef.current) {
        return;
      }

      handleRequestError(reason);
    } finally {
      if (requestId === latestRequestIdRef.current) {
        setIsLoading(false);
      }
    }
  }, [currentPage, debouncedSearchQuery, handleRequestError, pageSize, statusFilter]);

  useEffect(() => {
    const delay = searchQuery.trim() ? 300 : 0;
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearchQuery(searchQuery);
    }, delay);

    return () => window.clearTimeout(timeoutId);
  }, [searchQuery]);

  useEffect(() => {
    void loadMessages();
  }, [loadMessages, realtimeRefreshKey]);

  const messageCounts = attentionCounts ?? {
    newCount: 0,
    totalCount: 0,
  };

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
      const saved = await updateAdminContactMessageStatus(
        selectedMessage.id,
        status,
      );
      setSelectedMessage(saved);
      await loadMessages();
      onDataChanged?.();
      setMessage(`Contact message marked as ${STATUS_LABELS[saved.status]}.`);
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setIsSaving(false);
    }
  }

  async function confirmDeleteMessage() {
    if (!deleteCandidate) {
      return;
    }

    const candidate = deleteCandidate;
    setDeletingMessageId(candidate.id);
    setError(null);
    setMessage(null);
    try {
      await deleteAdminContactMessage(candidate.id);
      setSelectedMessage((current) =>
        current?.id === candidate.id ? null : current,
      );
      setDeleteCandidate(null);
      await loadMessages();
      onDataChanged?.();
      setMessage(`Contact message ${candidate.referenceNumber} was deleted.`);
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setDeletingMessageId(null);
    }
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <p className="text-[11px] text-muted-foreground font-sans">
            Review messages submitted through the public Contact page.
          </p>
          <p className="text-[10px] text-muted-foreground/80 font-sans">
            Contact messages are separate from order enquiries and can be marked
            as read, replied or archived.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <AttentionSummaryCard
          label="New messages"
          value={messageCounts.newCount}
          tone="accent"
        />
        <AttentionSummaryCard
          label="Total messages"
          value={messageCounts.totalCount}
        />
      </div>

      <div className="bg-card border border-border p-5 space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-12 gap-3 items-end">
          <AdminFilterDropdown
            id="contact-message-status-filter"
            label="Status"
            value={statusFilter}
            placeholder="All statuses"
            options={STATUS_FILTER_OPTIONS}
            allowEmpty={false}
            isOpen={statusFilterOpen}
            onToggle={() => setStatusFilterOpen((current) => !current)}
            onClose={() => setStatusFilterOpen(false)}
            onChange={(value) => {
              setCurrentPage(1);
              setStatusFilter(value as StatusFilter);
            }}
            className="xl:col-span-2"
          />
          <AdminSearchInput
            label="Search"
            value={searchQuery}
            onChange={(value) => {
              setCurrentPage(1);
              setSearchQuery(value);
            }}
            placeholder="Reference, sender, email, subject..."
            ariaLabel="Search contact messages"
            className="xl:col-span-5"
          />
          <div className="xl:col-span-2 text-[10px] text-muted-foreground font-sans">
            {messages.length} visible / {totalItems} total
          </div>
          <div className="xl:col-span-3 flex flex-wrap items-center justify-start xl:justify-end gap-2">
            <AdminActionButton
              icon={<Download size={12} aria-hidden="true" />}
              onClick={() => exportContactMessagesCsv(messages)}
              disabled={messages.length === 0}
            >
              Export CSV
            </AdminActionButton>
          </div>
        </div>
      </div>

      {error ? (
        <p
          role="alert"
          className="border border-destructive/30 bg-card px-4 py-3 text-[11px] text-destructive"
        >
          {error}
        </p>
      ) : null}
      {message ? (
        <p
          role="status"
          className="border border-emerald-300 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700"
        >
          {message}
        </p>
      ) : null}

      <ContactMessagesTable
        messages={messages}
        isLoading={isLoading}
        deletingMessageId={deletingMessageId}
        onSelect={(id) => void selectMessage(id)}
        onRequestDelete={setDeleteCandidate}
      />
      <AdminServerPagination
        page={currentPage}
        pageSize={pageSize}
        totalItems={totalItems}
        totalPages={totalPages}
        isLoading={isLoading}
        onPageChange={setCurrentPage}
        onPageSizeChange={(value) => {
          setCurrentPage(1);
          setPageSize(value);
        }}
      />


      {deleteCandidate ? (
        <AdminConfirmDialog
          title="Delete contact message?"
          description={
            <>
              This will permanently remove message
              <span className="font-medium text-foreground"> {deleteCandidate.referenceNumber}</span>
              {' '}from
              <span className="font-medium text-foreground"> {deleteCandidate.fullName}</span>.
              This action cannot be undone.
            </>
          }
          confirmLabel="Delete message"
          isBusy={deletingMessageId === deleteCandidate.id}
          onCancel={() => setDeleteCandidate(null)}
          onConfirm={() => void confirmDeleteMessage()}
        />
      ) : null}
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

function AttentionSummaryCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone?: "accent";
}) {
  return (
    <div className="bg-card border border-border px-5 py-4">
      <div className="text-[10px] uppercase tracking-wider text-muted-foreground font-sans">
        {label}
      </div>
      <div
        className={`mt-1 text-[1.45rem] font-serif font-light ${tone === "accent" ? "text-rose-700" : "text-foreground"}`}
      >
        {value}
      </div>
    </div>
  );
}

function ContactMessagesTable({
  messages,
  isLoading,
  deletingMessageId,
  onSelect,
  onRequestDelete,
}: {
  messages: AdminContactMessageListItem[];
  isLoading: boolean;
  deletingMessageId: string | null;
  onSelect(id: string): void;
  onRequestDelete(message: AdminContactMessageListItem): void;
}) {
  return (
    <div className="bg-card border border-border overflow-hidden">
      <table className="w-full table-fixed">
        <colgroup>
          <col className="w-[18%]" />
          <col className="w-[18%]" />
          <col className="w-[14%]" />
          <col className="w-[17%]" />
          <col className="w-[10%]" />
          <col className="w-[10%]" />
          <col className="w-[13%]" />
        </colgroup>
        <thead>
          <tr className="border-b border-border bg-secondary/40">
            {[
              "Sender",
              "Contact",
              "Subject",
              "Message",
              "Created",
              "Status",
              "Actions",
            ].map((heading) => (
              <th
                key={heading || "actions"}
                className="px-3 py-3 text-left text-[10px] tracking-wider text-muted-foreground font-sans font-normal"
              >
                {heading}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {isLoading ? (
            <tr>
              <td colSpan={7}>
                <AdminTableState message="Loading contact messages..." isLoading />
              </td>
            </tr>
          ) : null}
          {!isLoading && messages.length === 0 ? (
            <tr>
              <td colSpan={7}>
                <AdminTableState message="No contact messages match this status or search." />
              </td>
            </tr>
          ) : null}
          {!isLoading
            ? messages.map((item) => (
                <tr
                  key={item.id}
                  className="border-b border-border/40 hover:bg-secondary/25 transition-colors"
                >
                  <td className="px-3 py-3.5 text-[12px] text-foreground font-sans min-w-0">
                    <div className="flex min-w-0 items-center gap-2">
                      {item.status === "New" ? (
                        <Mail size={12} className="shrink-0 text-accent" />
                      ) : null}
                      <span className="truncate">{item.fullName}</span>
                    </div>
                    <div className="truncate text-[9px] text-muted-foreground font-mono mt-0.5">
                      {item.referenceNumber}
                    </div>
                  </td>
                  <td className="px-3 py-3.5 text-[10px] text-muted-foreground font-sans min-w-0">
                    <div className="truncate">{item.email}</div>
                    <div className="truncate mt-0.5">
                      {item.phone ?? "No phone"}
                    </div>
                  </td>
                  <td className="px-3 py-3.5 text-[11px] text-muted-foreground font-sans min-w-0">
                    <span
                      className="line-clamp-2 overflow-hidden break-words"
                      title={item.subject ?? "No subject"}
                    >
                      {item.subject ?? "No subject"}
                    </span>
                  </td>
                  <td className="px-3 py-3.5 text-[10px] text-muted-foreground font-sans min-w-0">
                    <p
                      className="line-clamp-2 overflow-hidden break-words"
                      title={item.messagePreview}
                    >
                      {item.messagePreview}
                    </p>
                  </td>
                  <td className="px-3 py-3.5 text-[10px] text-muted-foreground font-sans whitespace-nowrap">
                    {formatAdminDate(item.createdAt)}
                  </td>
                  <td className="px-3 py-3.5">
                    <StatusBadge status={item.status} />
                  </td>
                  <td className="px-3 py-3.5">
                    <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                      <button
                        type="button"
                        onClick={() => onSelect(item.id)}
                        className="inline-flex items-center gap-1 text-[10px] text-muted-foreground hover:text-foreground transition-colors"
                        aria-label={`View contact message ${item.referenceNumber} from ${item.fullName}`}
                      >
                        <Eye size={13} /> View
                      </button>
                      <button
                        type="button"
                        onClick={() => onRequestDelete(item)}
                        disabled={deletingMessageId === item.id}
                        className="inline-flex items-center gap-1 text-[10px] text-destructive hover:text-foreground disabled:opacity-50 transition-colors"
                        aria-label={`Delete contact message ${item.referenceNumber} from ${item.fullName}`}
                      >
                        {deletingMessageId === item.id ? (
                          <LoaderCircle size={13} className="animate-spin" />
                        ) : (
                          <Trash2 size={13} />
                        )}
                        Delete
                      </button>
                    </div>
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
    <div
      className="fixed inset-0 z-[70] bg-foreground/30 flex justify-end"
      role="presentation"
    >
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
            <p className="text-[9px] tracking-[0.2em] uppercase text-muted-foreground">
              {message?.referenceNumber ?? "Contact message"}
            </p>
            <h2 className="font-serif text-[1.35rem] font-light mt-1">
              {message?.fullName ?? "Loading..."}
            </h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-2 text-muted-foreground hover:text-foreground"
            aria-label="Close"
          >
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
              <Detail
                label="Subject"
                value={message.subject ?? "Not provided"}
              />
              <Detail
                label="Created"
                value={formatAdminDate(message.createdAt)}
              />
              <Detail
                label="Updated"
                value={formatAdminDate(message.updatedAt)}
              />
              <Detail
                label="Consent"
                value={message.consentGiven ? "Given" : "Not given"}
              />
            </section>

            <section className="bg-card border border-border p-5">
              <label
                htmlFor="contact-message-status"
                className="block text-[10px] tracking-wider uppercase text-muted-foreground mb-2"
              >
                Status
              </label>
              <select
                id="contact-message-status"
                value={message.status}
                disabled={isSaving}
                onChange={(event) =>
                  void onStatusChange(
                    event.target.value as ContactMessageStatus,
                  )
                }
                className="w-full border border-border bg-background px-3 py-2.5 text-[12px] focus:outline-none focus:border-accent disabled:opacity-50"
              >
                {CONTACT_MESSAGE_STATUSES.map((status) => (
                  <option key={status} value={status}>
                    {STATUS_LABELS[status]}
                  </option>
                ))}
              </select>
            </section>

            <section className="bg-card border border-border p-5">
              <h3 className="text-[10px] tracking-wider uppercase text-muted-foreground mb-3">
                Message
              </h3>
              <p className="text-[12px] leading-6 text-foreground whitespace-pre-wrap">
                {message.message}
              </p>
            </section>

            <section className="bg-card border border-border p-5">
              <h3 className="text-[10px] tracking-wider uppercase text-muted-foreground mb-3">
                Privacy consent
              </h3>
              <p className="text-[11px] leading-5 text-muted-foreground">
                Consent was {message.consentGiven ? "given" : "not recorded"}
                {message.consentRecordedAt
                  ? ` on ${formatAdminDate(message.consentRecordedAt)}.`
                  : "."}
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
    <span
      className={`text-[10px] px-2 py-0.5 whitespace-nowrap font-sans ${STATUS_COLORS[status]}`}
    >
      {STATUS_LABELS[status]}
    </span>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-[9px] tracking-wider uppercase text-muted-foreground mb-1">
        {label}
      </div>
      <div className="text-foreground break-words">{value}</div>
    </div>
  );
}

function getErrorMessage(reason: unknown): string {
  if (reason instanceof ApiError) {
    const validationMessages = reason.errors
      ? Object.values(reason.errors).flat()
      : [];
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


function exportContactMessagesCsv(
  messages: readonly AdminContactMessageListItem[],
): void {
  downloadCsv(createCsvFileName("bespoke-contact-messages"), messages, [
    { header: "Reference", value: (message) => message.referenceNumber },
    { header: "Sender", value: (message) => message.fullName },
    { header: "Email", value: (message) => message.email },
    { header: "Phone", value: (message) => message.phone },
    { header: "Subject", value: (message) => message.subject },
    { header: "Status", value: (message) => STATUS_LABELS[message.status] },
    { header: "Created at", value: (message) => message.createdAt },
    { header: "Message preview", value: (message) => message.messagePreview },
  ]);
}
