import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AlertTriangle, CheckCircle2, Download, RefreshCw, Search, X } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  getEmailDeliveryLog,
  getEmailDeliveryLogErrorMessage,
  type EmailDeliveryLogEntry,
} from "../../api/emailDeliveryLogApi";
import { createCsvFileName, downloadCsv } from "../utils/csvExport";
import { formatAdminDate } from "./adminOrderFormatting";

interface AdminEmailLogPanelProps {
  onUnauthorized(): void;
  realtimeRefreshKey?: number;
}

interface EmailLogFilters {
  search: string;
  messageType: string;
  status: string;
  recipientEmail: string;
  provider: string;
  take: number;
}

interface FilterOption {
  value: string;
  label: string;
  meta?: string;
}

const DEFAULT_FILTERS: EmailLogFilters = {
  search: "",
  messageType: "",
  status: "",
  recipientEmail: "",
  provider: "",
  take: 100,
};

const LIMIT_OPTIONS: FilterOption[] = [
  { value: "50", label: "50 latest" },
  { value: "100", label: "100 latest" },
  { value: "200", label: "200 latest" },
];

const STATUS_OPTIONS: FilterOption[] = [
  { value: "Sent", label: "Sent" },
  { value: "Failed", label: "Failed" },
];

const MESSAGE_TYPE_LABELS: Record<string, string> = {
  owner_order_notification: "Owner · Order notification",
  customer_order_confirmation: "Customer · Order confirmation",
  owner_contact_notification: "Owner · Contact notification",
  customer_contact_confirmation: "Customer · Contact confirmation",
  test_email: "Test email",
};

export function AdminEmailLogPanel({
  onUnauthorized,
  realtimeRefreshKey = 0,
}: AdminEmailLogPanelProps) {
  const [entries, setEntries] = useState<EmailDeliveryLogEntry[]>([]);
  const [filters, setFilters] = useState<EmailLogFilters>(DEFAULT_FILTERS);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);
  const [debouncedSearch, setDebouncedSearch] = useState(filters.search);
  const [debouncedRecipientEmail, setDebouncedRecipientEmail] = useState(
    filters.recipientEmail,
  );

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setDebouncedSearch(filters.search);
    }, 250);

    return () => window.clearTimeout(timeout);
  }, [filters.search]);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setDebouncedRecipientEmail(filters.recipientEmail);
    }, 250);

    return () => window.clearTimeout(timeout);
  }, [filters.recipientEmail]);

  const loadEntries = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await getEmailDeliveryLog({
        take: filters.take,
        search: debouncedSearch,
        messageType: filters.messageType,
        status: filters.status,
        recipientEmail: debouncedRecipientEmail,
        provider: filters.provider,
      });
      setEntries(result);
    } catch (reason: unknown) {
      if (
        reason instanceof ApiError &&
        (reason.status === 401 || reason.status === 403)
      ) {
        onUnauthorized();
        return;
      }

      setError(getEmailDeliveryLogErrorMessage(reason));
    } finally {
      setIsLoading(false);
    }
  }, [
    debouncedRecipientEmail,
    debouncedSearch,
    filters.messageType,
    filters.provider,
    filters.status,
    filters.take,
    onUnauthorized,
  ]);

  useEffect(() => {
    void loadEntries();
  }, [loadEntries, realtimeRefreshKey]);

  const messageTypeOptions = useMemo(
    () =>
      buildDropdownOptions(
        entries.map((entry) => entry.messageType),
        formatMessageTypeLabel,
      ),
    [entries],
  );
  const providerOptions = useMemo(
    () =>
      buildDropdownOptions(
        entries.map((entry) => entry.provider),
        prettifyToken,
      ),
    [entries],
  );
  const sentCount = entries.filter((entry) => entry.status === "Sent").length;
  const failedCount = entries.filter((entry) => entry.status === "Failed").length;
  const activeFilterCount = [
    filters.search,
    filters.messageType,
    filters.status,
    filters.recipientEmail,
    filters.provider,
  ].filter((value) => value.trim().length > 0).length;

  const updateFilter = useCallback(
    <TKey extends keyof EmailLogFilters>(key: TKey, value: EmailLogFilters[TKey]) => {
      setFilters((current) => ({ ...current, [key]: value }));
    },
    [],
  );

  const clearFilters = () => {
    setOpenDropdown(null);
    setFilters(DEFAULT_FILTERS);
    setDebouncedSearch("");
    setDebouncedRecipientEmail("");
  };

  const handleExport = () => {
    downloadCsv(createCsvFileName("email-log"), entries, [
      { header: "Created", value: (entry) => entry.createdAt },
      { header: "Completed", value: (entry) => entry.completedAt },
      { header: "Status", value: (entry) => entry.status },
      { header: "Type", value: (entry) => entry.messageType },
      { header: "Recipient", value: (entry) => entry.recipientEmail },
      { header: "Subject", value: (entry) => entry.subject },
      { header: "Provider", value: (entry) => entry.provider },
      { header: "Sent externally", value: (entry) => (entry.sentExternally ? "Yes" : "No") },
      { header: "Related entity", value: (entry) => entry.relatedEntityType },
      { header: "Related ID", value: (entry) => entry.relatedEntityId },
      { header: "Related label", value: (entry) => entry.relatedEntityLabel },
      { header: "Result", value: (entry) => entry.resultMessage },
      { header: "Error", value: (entry) => entry.errorMessage },
    ]);
  };

  return (
    <section className="space-y-5">
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <EmailLogStatCard label="Visible entries" value={entries.length} tone="neutral" />
        <EmailLogStatCard label="Sent" value={sentCount} tone="success" />
        <EmailLogStatCard label="Failed" value={failedCount} tone="danger" />
      </div>

      <div className="bg-card border border-border">
        <div className="px-5 py-4 border-b border-border flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              Email delivery log
            </h2>
            <p className="text-[10px] text-muted-foreground font-sans mt-0.5">
              Review owner notifications, customer confirmations and test email attempts. Email bodies and secrets are not stored.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              onClick={handleExport}
              disabled={entries.length === 0}
              className="inline-flex items-center gap-2 px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50"
            >
              <Download size={12} aria-hidden="true" /> Export CSV
            </button>
            <button
              type="button"
              onClick={() => void loadEntries()}
              disabled={isLoading}
              className="inline-flex items-center gap-2 px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50"
            >
              <RefreshCw size={12} aria-hidden="true" /> Refresh
            </button>
          </div>
        </div>

        <div className="p-5 border-b border-border space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-12 gap-3 items-end">
            <div className="relative xl:col-span-3">
              <span className="mb-1 block text-[9px] uppercase tracking-[0.18em] text-muted-foreground">
                Search
              </span>
              <div className="relative">
                <Search
                  size={13}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground"
                  aria-hidden="true"
                />
                <input
                  type="text"
                  value={filters.search}
                  onChange={(event) => updateFilter("search", event.target.value)}
                  placeholder="Subject, recipient, result..."
                  className="w-full border border-border bg-background pl-8 pr-8 py-2 text-[10px] font-sans focus:outline-none focus:border-accent"
                  aria-label="Search email log"
                />
                {filters.search ? (
                  <button
                    type="button"
                    onClick={() => updateFilter("search", "")}
                    className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-muted-foreground hover:text-foreground"
                    aria-label="Clear search"
                  >
                    <X size={12} />
                  </button>
                ) : null}
              </div>
            </div>
            <div className="xl:col-span-2">
              <StyledFilterDropdown
                id="messageType"
                label="Type"
                value={filters.messageType}
                placeholder="Any type"
                options={messageTypeOptions}
                isOpen={openDropdown === "messageType"}
                onToggle={() => setOpenDropdown((current) => (current === "messageType" ? null : "messageType"))}
                onClose={() => setOpenDropdown(null)}
                onChange={(value) => updateFilter("messageType", value)}
              />
            </div>
            <div className="xl:col-span-2">
              <StyledFilterDropdown
                id="status"
                label="Status"
                value={filters.status}
                placeholder="Any status"
                options={STATUS_OPTIONS}
                isOpen={openDropdown === "status"}
                onToggle={() => setOpenDropdown((current) => (current === "status" ? null : "status"))}
                onClose={() => setOpenDropdown(null)}
                onChange={(value) => updateFilter("status", value)}
              />
            </div>
            <div className="xl:col-span-2">
              <StyledFilterDropdown
                id="provider"
                label="Provider"
                value={filters.provider}
                placeholder="Any provider"
                options={providerOptions}
                isOpen={openDropdown === "provider"}
                onToggle={() => setOpenDropdown((current) => (current === "provider" ? null : "provider"))}
                onClose={() => setOpenDropdown(null)}
                onChange={(value) => updateFilter("provider", value)}
              />
            </div>
            <div className="relative xl:col-span-2">
              <span className="mb-1 block text-[9px] uppercase tracking-[0.18em] text-muted-foreground">
                Recipient
              </span>
              <input
                type="text"
                value={filters.recipientEmail}
                onChange={(event) => updateFilter("recipientEmail", event.target.value)}
                placeholder="admin@example.com"
                className="w-full border border-border bg-background px-3 py-2 pr-8 text-[10px] font-sans focus:outline-none focus:border-accent"
                aria-label="Filter by recipient email"
              />
              {filters.recipientEmail ? (
                <button
                  type="button"
                  onClick={() => updateFilter("recipientEmail", "")}
                  className="absolute right-2 bottom-2 p-1 text-muted-foreground hover:text-foreground"
                  aria-label="Clear recipient email filter"
                >
                  <X size={12} />
                </button>
              ) : null}
            </div>
            <div className="xl:col-span-1">
              <StyledFilterDropdown
                id="take"
                label="Limit"
                value={String(filters.take)}
                placeholder="100 latest"
                options={LIMIT_OPTIONS}
                isOpen={openDropdown === "take"}
                onToggle={() => setOpenDropdown((current) => (current === "take" ? null : "take"))}
                onClose={() => setOpenDropdown(null)}
                onChange={(value) => updateFilter("take", Number(value))}
                hideEmptyOption
              />
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-[10px] text-muted-foreground font-sans">
              Filters apply automatically. New email attempts refresh through live updates. Showing {entries.length} entries.
            </p>
            {activeFilterCount > 0 ? (
              <button
                type="button"
                onClick={clearFilters}
                className="text-[10px] text-muted-foreground hover:text-foreground"
              >
                Clear filters ({activeFilterCount})
              </button>
            ) : null}
          </div>
        </div>

        {error ? (
          <div className="m-5 border border-destructive/30 bg-destructive/5 px-4 py-3 text-[11px] text-destructive flex items-start gap-2">
            <AlertTriangle size={14} className="mt-0.5" aria-hidden="true" />
            <span>{error}</span>
          </div>
        ) : null}

        <div className="overflow-x-auto">
          <table className="w-full text-left text-[11px] font-sans">
            <thead className="bg-muted/50 text-[9px] uppercase tracking-[0.2em] text-muted-foreground">
              <tr>
                <th className="px-4 py-3 font-normal">Time</th>
                <th className="px-4 py-3 font-normal">Status</th>
                <th className="px-4 py-3 font-normal">Type</th>
                <th className="px-4 py-3 font-normal">Recipient</th>
                <th className="px-4 py-3 font-normal">Subject</th>
                <th className="px-4 py-3 font-normal">Provider</th>
                <th className="px-4 py-3 font-normal">Related</th>
                <th className="px-4 py-3 font-normal">Result</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td colSpan={8} className="px-4 py-10 text-center text-muted-foreground">
                    Loading email log…
                  </td>
                </tr>
              ) : entries.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-4 py-10 text-center text-muted-foreground">
                    No email attempts match the current filters.
                  </td>
                </tr>
              ) : (
                entries.map((entry) => <EmailLogRow key={entry.id} entry={entry} />)
              )}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

function EmailLogStatCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: "neutral" | "success" | "danger";
}) {
  const toneClass: Record<"neutral" | "success" | "danger", string> = {
    neutral: "border-border bg-card text-foreground",
    success: "border-emerald-200 bg-emerald-50 text-emerald-700",
    danger: "border-destructive/30 bg-destructive/5 text-destructive",
  };

  return (
    <div className={`border px-5 py-4 ${toneClass[tone]}`}>
      <p className="text-[9px] uppercase tracking-[0.24em] font-sans opacity-70">
        {label}
      </p>
      <p className="mt-2 font-serif text-[1.6rem] font-light">{value}</p>
    </div>
  );
}

function EmailLogRow({ entry }: { entry: EmailDeliveryLogEntry }) {
  const isSent = entry.status === "Sent";
  const statusClass = isSent
    ? "border-emerald-200 bg-emerald-50 text-emerald-700"
    : "border-destructive/30 bg-destructive/5 text-destructive";
  const StatusIcon = isSent ? CheckCircle2 : AlertTriangle;
  const relatedLabel = entry.relatedEntityLabel ?? entry.relatedEntityId ?? "—";

  return (
    <tr className="border-t border-border align-top hover:bg-muted/30">
      <td className="px-4 py-3 whitespace-nowrap text-muted-foreground">
        {formatAdminDate(entry.createdAt)}
      </td>
      <td className="px-4 py-3 whitespace-nowrap">
        <span className={`inline-flex items-center gap-1.5 rounded-full border px-2 py-1 text-[9px] ${statusClass}`}>
          <StatusIcon size={11} aria-hidden="true" /> {entry.status}
        </span>
      </td>
      <td className="px-4 py-3 min-w-[170px]">
        <span className="text-foreground">{formatMessageTypeLabel(entry.messageType)}</span>
        <span className="mt-1 block font-mono text-[9px] text-muted-foreground">
          {entry.messageType}
        </span>
      </td>
      <td className="px-4 py-3 min-w-[190px] break-all text-muted-foreground">
        {entry.recipientEmail}
      </td>
      <td className="px-4 py-3 min-w-[240px] text-foreground">{entry.subject}</td>
      <td className="px-4 py-3 whitespace-nowrap">
        <span className="text-foreground">{entry.provider}</span>
        <span className="mt-1 block text-[9px] text-muted-foreground">
          {entry.sentExternally ? "External SMTP" : "Not external"}
        </span>
      </td>
      <td className="px-4 py-3 min-w-[160px] text-muted-foreground">
        <span className="block text-foreground">{entry.relatedEntityType ?? "—"}</span>
        <span className="block font-mono text-[9px]">{relatedLabel}</span>
      </td>
      <td className="px-4 py-3 min-w-[260px] text-muted-foreground">
        <span className="block">{entry.resultMessage}</span>
        {entry.errorMessage ? (
          <span className="mt-1 block text-destructive">{entry.errorMessage}</span>
        ) : null}
      </td>
    </tr>
  );
}

function StyledFilterDropdown({
  id,
  label,
  value,
  placeholder,
  options,
  isOpen,
  hideEmptyOption = false,
  onToggle,
  onClose,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  placeholder: string;
  options: readonly FilterOption[];
  isOpen: boolean;
  hideEmptyOption?: boolean;
  onToggle(): void;
  onClose(): void;
  onChange(value: string): void;
}) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const selected = options.find((option) => option.value === value);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    function handlePointerDown(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        onClose();
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    return () => document.removeEventListener("mousedown", handlePointerDown);
  }, [isOpen, onClose]);

  return (
    <div ref={containerRef} className="relative min-w-0">
      <span className="mb-1 block text-[9px] uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </span>
      <button
        type="button"
        aria-expanded={isOpen}
        aria-controls={`${id}-dropdown`}
        onClick={onToggle}
        className="w-full border border-border bg-background px-3 py-2 text-left text-[10px] font-sans text-foreground hover:border-foreground focus:outline-none focus:border-accent"
      >
        <span className="flex items-center justify-between gap-2">
          <span className={selected ? "truncate" : "truncate text-muted-foreground"}>
            {selected?.label ?? placeholder}
          </span>
          <span className="text-muted-foreground" aria-hidden="true">▾</span>
        </span>
      </button>
      {isOpen ? (
        <div
          id={`${id}-dropdown`}
          className="absolute z-30 mt-1 max-h-72 w-full min-w-[220px] overflow-auto border border-border bg-card shadow-lg"
        >
          {!hideEmptyOption ? (
            <button
              type="button"
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => {
                onChange("");
                onClose();
              }}
              className={`w-full px-3 py-2 text-left text-[10px] hover:bg-muted ${value === "" ? "bg-muted/50 text-foreground" : "text-muted-foreground"}`}
            >
              <span className="flex items-center justify-between gap-2">
                {placeholder}
                {value === "" ? <span aria-hidden="true">✓</span> : null}
              </span>
            </button>
          ) : null}

          {options.map((option) => (
            <button
              key={option.value}
              type="button"
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => {
                onChange(option.value);
                onClose();
              }}
              className={`w-full border-t border-border/60 px-3 py-2 text-left text-[10px] hover:bg-muted ${
                option.value === value ? "bg-muted/50 text-foreground" : "text-foreground"
              }`}
            >
              <span className="flex items-center justify-between gap-2">
                <span className="truncate">{option.label}</span>
                {option.value === value ? <span aria-hidden="true">✓</span> : null}
              </span>
              {option.meta ? (
                <span className="mt-0.5 block truncate font-mono text-[8px] text-muted-foreground">
                  {option.meta}
                </span>
              ) : null}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function buildDropdownOptions(
  values: readonly string[],
  getLabel: (value: string) => string,
): FilterOption[] {
  return getUniqueValues(values)
    .filter((value) => value.trim().length > 0)
    .map((value) => ({ value, label: getLabel(value), meta: value }));
}

function formatMessageTypeLabel(value: string): string {
  return MESSAGE_TYPE_LABELS[value] ?? prettifyToken(value);
}

function prettifyToken(value: string): string {
  return value
    .replace(/_/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .split(" ")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function getUniqueValues(values: readonly string[]): string[] {
  return Array.from(new Set(values.filter(Boolean))).sort((left, right) =>
    left.localeCompare(right),
  );
}
