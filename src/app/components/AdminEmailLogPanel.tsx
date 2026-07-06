import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { AlertTriangle, CheckCircle2, Clock3, Download, RefreshCw, Search, X } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  getEmailDeliveryLog,
  getEmailDeliveryLogErrorMessage,
  retryEmailDeliveryLogEntry,
  type EmailDeliveryLogEntry,
  type EmailOutboxMonitoringSummary,
  type EmailOutboxRetentionCleanupResult,
  type EmailOutboxRetentionSummary,
} from "../../api/emailDeliveryLogApi";
import { type AdminPageSize } from "../../api/pagination";
import { createCsvFileName, downloadCsv } from "../utils/csvExport";
import { formatAdminDate } from "./adminOrderFormatting";
import { AdminServerPagination } from "./AdminUi";

interface AdminEmailLogPanelProps {
  onUnauthorized(): void;
  realtimeRefreshKey?: number;
  monitoringSummary?: EmailOutboxMonitoringSummary | null;
  monitoringSummaryError?: string | null;
  isMonitoringSummaryLoading?: boolean;
  onRefreshMonitoringSummary?: () => void;
  retentionSummary?: EmailOutboxRetentionSummary | null;
  retentionSummaryError?: string | null;
  isRetentionSummaryLoading?: boolean;
  onRefreshRetentionSummary?: () => void;
  onRunRetentionCleanup?: () => Promise<EmailOutboxRetentionCleanupResult>;
}

interface EmailLogFilters {
  search: string;
  messageType: string;
  status: string;
  recipientEmail: string;
  provider: string;
  pageSize: AdminPageSize;
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
  pageSize: 25,
};

const STATUS_OPTIONS: FilterOption[] = [
  { value: "Queued", label: "Queued" },
  { value: "Retrying", label: "Retrying" },
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

const KNOWN_EMAIL_PROVIDERS = ["Outbox", "Logging", "LoggingFallback", "SMTP", "GmailSmtp"];

export function AdminEmailLogPanel({
  onUnauthorized,
  realtimeRefreshKey = 0,
  monitoringSummary = null,
  monitoringSummaryError = null,
  isMonitoringSummaryLoading = false,
  onRefreshMonitoringSummary,
  retentionSummary = null,
  retentionSummaryError = null,
  isRetentionSummaryLoading = false,
  onRefreshRetentionSummary,
  onRunRetentionCleanup,
}: AdminEmailLogPanelProps) {
  const [entries, setEntries] = useState<EmailDeliveryLogEntry[]>([]);
  const [filters, setFilters] = useState<EmailLogFilters>(DEFAULT_FILTERS);
  const [page, setPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [retryingId, setRetryingId] = useState<string | null>(null);
  const [isRetentionCleanupRunning, setIsRetentionCleanupRunning] = useState(false);
  const [openDropdown, setOpenDropdown] = useState<string | null>(null);
  const [debouncedSearch, setDebouncedSearch] = useState(filters.search);
  const [debouncedRecipientEmail, setDebouncedRecipientEmail] = useState(
    filters.recipientEmail,
  );
  const latestRequestIdRef = useRef(0);

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
    const requestId = ++latestRequestIdRef.current;
    setIsLoading(true);
    setError(null);

    try {
      const result = await getEmailDeliveryLog({
        page,
        pageSize: filters.pageSize,
        search: debouncedSearch,
        messageType: filters.messageType,
        status: filters.status,
        recipientEmail: debouncedRecipientEmail,
        provider: filters.provider,
      });
      if (requestId !== latestRequestIdRef.current) {
        return;
      }

      setEntries(result.items);
      setTotalItems(result.totalItems);
      setTotalPages(result.totalPages);
      if (result.page > result.totalPages) {
        setPage(result.totalPages);
      }
    } catch (reason: unknown) {
      if (requestId !== latestRequestIdRef.current) {
        return;
      }

      if (
        reason instanceof ApiError &&
        (reason.status === 401 || reason.status === 403)
      ) {
        onUnauthorized();
        return;
      }

      setError(getEmailDeliveryLogErrorMessage(reason));
    } finally {
      if (requestId === latestRequestIdRef.current) {
        setIsLoading(false);
      }
    }
  }, [
    debouncedRecipientEmail,
    debouncedSearch,
    filters.messageType,
    filters.provider,
    filters.status,
    filters.pageSize,
    onUnauthorized,
    page,
  ]);

  const refreshEmailLogOperations = useCallback(async () => {
    await loadEntries();
    onRefreshMonitoringSummary?.();
    onRefreshRetentionSummary?.();
  }, [loadEntries, onRefreshMonitoringSummary, onRefreshRetentionSummary]);

  useEffect(() => {
    void loadEntries();
  }, [loadEntries, realtimeRefreshKey]);

  const handleRetry = useCallback(
    async (id: string) => {
      setRetryingId(id);
      setError(null);
      setInfo(null);

      try {
        await retryEmailDeliveryLogEntry(id);
        setInfo("Manual retry queued.");
        await loadEntries();
        onRefreshMonitoringSummary?.();
      } catch (reason: unknown) {
        if (
          reason instanceof ApiError &&
          (reason.status === 401 || reason.status === 403)
        ) {
          onUnauthorized();
          return;
        }

        if (reason instanceof ApiError && reason.status === 409) {
          setError("This email is not eligible for manual retry anymore.");
          return;
        }

        setError(getEmailDeliveryLogErrorMessage(reason));
      } finally {
        setRetryingId(null);
      }
    },
    [loadEntries, onRefreshMonitoringSummary, onUnauthorized],
  );

  const retentionCandidateCount = useMemo(() => {
    if (!retentionSummary) {
      return 0;
    }

    return (
      retentionSummary.succeededBodyPurgeCandidateCount +
      retentionSummary.skippedBodyPurgeCandidateCount +
      retentionSummary.succeededDeleteCandidateCount +
      retentionSummary.skippedDeleteCandidateCount
    );
  }, [retentionSummary]);

  const handleRetentionCleanup = useCallback(async () => {
    if (!onRunRetentionCleanup) {
      return;
    }

    setIsRetentionCleanupRunning(true);
    setError(null);
    setInfo(null);

    try {
      const result = await onRunRetentionCleanup();
      setInfo(formatRetentionCleanupMessage(result));
      await loadEntries();
      onRefreshRetentionSummary?.();
      onRefreshMonitoringSummary?.();
    } catch (reason: unknown) {
      if (
        reason instanceof ApiError &&
        (reason.status === 401 || reason.status === 403)
      ) {
        onUnauthorized();
        return;
      }

      setError("Email outbox retention cleanup could not be completed.");
    } finally {
      setIsRetentionCleanupRunning(false);
    }
  }, [
    loadEntries,
    onRefreshMonitoringSummary,
    onRefreshRetentionSummary,
    onRunRetentionCleanup,
    onUnauthorized,
  ]);

  const messageTypeOptions = useMemo(
    () =>
      buildDropdownOptions(
        [...Object.keys(MESSAGE_TYPE_LABELS), filters.messageType, ...entries.map((entry) => entry.messageType)],
        formatMessageTypeLabel,
      ),
    [entries, filters.messageType],
  );
  const providerOptions = useMemo(
    () =>
      buildDropdownOptions(
        [...KNOWN_EMAIL_PROVIDERS, filters.provider, ...entries.map((entry) => entry.provider)],
        prettifyToken,
      ),
    [entries, filters.provider],
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
      setPage(1);
      setFilters((current) => ({ ...current, [key]: value }));
    },
    [],
  );

  const clearFilters = () => {
    setOpenDropdown(null);
    setPage(1);
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
      <OutboxMonitoringSummary
        summary={monitoringSummary}
        error={monitoringSummaryError}
        isLoading={isMonitoringSummaryLoading}
        onRefresh={onRefreshMonitoringSummary}
      />

      <OutboxRetentionSummary
        summary={retentionSummary}
        error={retentionSummaryError}
        isLoading={isRetentionSummaryLoading}
        candidateCount={retentionCandidateCount}
        isCleanupRunning={isRetentionCleanupRunning}
        onRunCleanup={() => void handleRetentionCleanup()}
        onRefresh={onRefreshRetentionSummary}
      />

      <div className="space-y-2">
        <EmailLogSectionHeader
          title="Current page"
          subtitle="These cards summarize only the loaded page below."
        />
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <EmailLogStatCard label="Page entries" value={entries.length} tone="neutral" />
          <EmailLogStatCard label="Sent on page" value={sentCount} tone="success" />
          <EmailLogStatCard label="Failed on page" value={failedCount} tone="danger" />
        </div>
      </div>

      <div className="bg-card border border-border">
        <div className="px-5 py-4 border-b border-border flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              Email delivery log
            </h2>
            <p className="text-[10px] text-muted-foreground font-sans mt-0.5">
              Review queued owner notifications, customer confirmations and test email attempts. Email bodies are not exposed here and secrets are never stored in the log.
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
              onClick={() => void refreshEmailLogOperations()}
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
            <div className="relative xl:col-span-3">
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
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-[10px] text-muted-foreground font-sans">
              Filters apply automatically · Showing {entries.length} of {totalItems} email entr{totalItems === 1 ? "y" : "ies"} · Live updates enabled
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
          <EmailLogInlineNotice tone="error">{error}</EmailLogInlineNotice>
        ) : null}

        {info ? (
          <EmailLogInlineNotice tone="success">{info}</EmailLogInlineNotice>
        ) : null}

        <div className="min-w-0 max-w-full overflow-hidden">
          <table className="w-full table-fixed text-left text-[11px] font-sans">
            <colgroup>
              <col className="w-[9%]" />
              <col className="w-[8%]" />
              <col className="w-[12%]" />
              <col className="w-[13%]" />
              <col className="w-[15%]" />
              <col className="w-[9%]" />
              <col className="w-[11%]" />
              <col className="w-[23%]" />
            </colgroup>
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
                    {activeFilterCount > 0 ? (
                      <div className="space-y-2">
                        <p>No email attempts match the current filters.</p>
                        <button
                          type="button"
                          onClick={clearFilters}
                          className="text-[10px] border border-border bg-background px-3 py-1 hover:border-foreground"
                        >
                          Clear filters
                        </button>
                      </div>
                    ) : (
                      "No email attempts recorded yet."
                    )}
                  </td>
                </tr>
              ) : (
                entries.map((entry) => (
                  <EmailLogRow
                    key={entry.id}
                    entry={entry}
                    isRetrying={retryingId === entry.id}
                    onRetry={handleRetry}
                  />
                ))
              )}
            </tbody>
          </table>
        </div>
        <AdminServerPagination
          page={page}
          pageSize={filters.pageSize}
          totalItems={totalItems}
          totalPages={totalPages}
          isLoading={isLoading}
          onPageChange={setPage}
          onPageSizeChange={(value) => updateFilter("pageSize", value)}
        />
      </div>
    </section>
  );
}

function OutboxRetentionSummary({
  summary,
  error,
  isLoading,
  candidateCount,
  isCleanupRunning,
  onRunCleanup,
  onRefresh,
}: {
  summary: EmailOutboxRetentionSummary | null;
  error: string | null;
  isLoading: boolean;
  candidateCount: number;
  isCleanupRunning: boolean;
  onRunCleanup(): void;
  onRefresh?: () => void;
}) {
  if (error) {
    return (
      <EmailLogInlineNotice tone="warning">
        Email outbox retention status is unavailable right now.
      </EmailLogInlineNotice>
    );
  }

  if (!summary) {
    return (
      <div className="border border-border bg-card px-4 py-3 text-[11px] text-muted-foreground">
        {isLoading ? "Checking retention cleanup status…" : "Retention cleanup status is unavailable."}
      </div>
    );
  }

  const bodyPurgeCandidates =
    summary.succeededBodyPurgeCandidateCount + summary.skippedBodyPurgeCandidateCount;
  const deleteCandidates =
    summary.succeededDeleteCandidateCount + summary.skippedDeleteCandidateCount;

  return (
    <div className="border border-border bg-card px-5 py-4 space-y-3">
      <EmailLogSectionHeader
        title="Retention cleanup"
        subtitle="Replace old succeeded/skipped bodies with a placeholder and remove very old outbox rows. Failed messages stay for review."
        action={
          onRefresh ? (
            <EmailLogOperationButton onClick={onRefresh} disabled={isLoading}>
              Refresh status
            </EmailLogOperationButton>
          ) : null
        }
      />

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-3 text-[10px] font-sans">
        <div>
          <p className="text-muted-foreground uppercase tracking-[0.18em] text-[9px]">
            Worker status
          </p>
          <p className="mt-1 text-foreground">
            {summary.workerEnabled ? "Enabled" : "Disabled"}
          </p>
        </div>
        <div>
          <p className="text-muted-foreground uppercase tracking-[0.18em] text-[9px]">
            Body purge candidates
          </p>
          <p className="mt-1 text-foreground">
            {summary.succeededBodyPurgeCandidateCount} succeeded · {summary.skippedBodyPurgeCandidateCount} skipped
            {bodyPurgeCandidates > 0 ? ` (${bodyPurgeCandidates} total)` : ""}
          </p>
        </div>
        <div>
          <p className="text-muted-foreground uppercase tracking-[0.18em] text-[9px]">
            Delete candidates
          </p>
          <p className="mt-1 text-foreground">
            {summary.succeededDeleteCandidateCount} succeeded · {summary.skippedDeleteCandidateCount} skipped
            {deleteCandidates > 0 ? ` (${deleteCandidates} total)` : ""}
          </p>
        </div>
        <div>
          <p className="text-muted-foreground uppercase tracking-[0.18em] text-[9px]">
            Failed retained
          </p>
          <p className="mt-1 text-foreground">{summary.failedRetainedCount}</p>
        </div>
      </div>

      <div className="text-[10px] text-muted-foreground font-sans space-y-1">
        <p>Checked {formatAdminDate(summary.generatedAt)}</p>
        <p>
          Body retention: {summary.succeededBodyRetentionDays}d succeeded · {summary.skippedBodyRetentionDays}d skipped
        </p>
        <p>
          Message retention: {summary.succeededMessageRetentionDays}d succeeded · {summary.skippedMessageRetentionDays}d skipped
        </p>
        {summary.workerEnabled ? (
          <p>Worker interval: every {summary.workerIntervalHours}h</p>
        ) : null}
      </div>

      <p className="text-[10px] text-muted-foreground font-sans">{summary.summaryMessage}</p>

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="button"
          onClick={onRunCleanup}
          disabled={isCleanupRunning || candidateCount === 0}
          className="inline-flex items-center gap-2 px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50"
        >
          <RefreshCw
            size={12}
            className={isCleanupRunning ? "animate-spin" : undefined}
            aria-hidden="true"
          />
          {isCleanupRunning ? "Cleaning…" : "Run cleanup"}
        </button>
        {candidateCount === 0 ? (
          <p className="text-[10px] text-muted-foreground font-sans">
            No cleanup candidates right now.
          </p>
        ) : null}
      </div>
    </div>
  );
}

function OutboxMonitoringSummary({
  summary,
  error,
  isLoading,
  onRefresh,
}: {
  summary: EmailOutboxMonitoringSummary | null;
  error: string | null;
  isLoading: boolean;
  onRefresh?: () => void;
}) {
  if (error) {
    return (
      <EmailLogInlineNotice tone="warning">
        Email outbox monitoring is unavailable right now. The page entries below
        still reflect recent attempts.
      </EmailLogInlineNotice>
    );
  }

  if (!summary) {
    return (
      <div className="border border-border bg-card px-4 py-3 text-[11px] text-muted-foreground">
        {isLoading ? "Checking email outbox health…" : "Email outbox health is unavailable."}
      </div>
    );
  }

  const healthTone =
    summary.healthStatus === "Healthy"
      ? "success"
      : summary.healthStatus === "Critical"
        ? "danger"
        : "warning";
  const healthLabel =
    summary.healthStatus === "Healthy"
      ? "Healthy"
      : summary.healthStatus === "Critical"
        ? "Issues"
        : "Needs review";
  const showOldestDetails =
    summary.healthStatus === "Warning" || summary.healthStatus === "Critical";

  return (
    <div className="border border-border bg-card px-5 py-4 space-y-3">
      <EmailLogSectionHeader
        title="Global outbox health"
        subtitle="Counts are calculated across the whole email outbox, not only the current page."
        action={
          onRefresh ? (
            <EmailLogOperationButton onClick={onRefresh} disabled={isLoading}>
              Refresh status
            </EmailLogOperationButton>
          ) : null
        }
      />

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        <EmailLogStatCard
          label="Outbox health"
          value={healthLabel}
          tone={healthTone}
        />
        <EmailLogStatCard label="Failed" value={summary.failedCount} tone="danger" />
        <EmailLogStatCard label="Retrying" value={summary.retryingCount} tone="warning" />
        <EmailLogStatCard
          label="Pending / stale"
          value={`${summary.pendingCount} / ${summary.stalePendingCount}`}
          tone={summary.stalePendingCount > 0 ? "warning" : "neutral"}
        />
        <EmailLogStatCard
          label="Sent 24h"
          value={summary.sentLast24HoursCount}
          tone="success"
        />
      </div>

      <div className="text-[10px] text-muted-foreground font-sans space-y-0.5">
        <p>Checked {formatAdminDate(summary.generatedAt)}</p>
        {showOldestDetails && summary.oldestPendingCreatedAt ? (
          <p>Oldest pending: {formatAdminDate(summary.oldestPendingCreatedAt)}</p>
        ) : null}
        {showOldestDetails && summary.oldestFailedUpdatedAt ? (
          <p>Oldest failed update: {formatAdminDate(summary.oldestFailedUpdatedAt)}</p>
        ) : null}
      </div>

      {summary.exhaustedFailedCount > 0 ? (
        <EmailLogInlineNotice tone="error" embedded>
          There are failed email messages that need review. Fix SMTP/provider
          settings, then use Retry on failed rows.
        </EmailLogInlineNotice>
      ) : null}

      {summary.stalePendingCount > 0 ? (
        <EmailLogInlineNotice tone="warning" embedded>
          Some pending messages appear stale. Check the background worker.
        </EmailLogInlineNotice>
      ) : null}

      {summary.retryingCount > 0 ? (
        <EmailLogInlineNotice tone="neutral" embedded>
          Some messages are scheduled for automatic retry.
        </EmailLogInlineNotice>
      ) : null}
    </div>
  );
}

function EmailLogStatCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number | string;
  tone: "neutral" | "success" | "danger" | "warning";
}) {
  const toneClass: Record<"neutral" | "success" | "danger" | "warning", string> = {
    neutral: "border-border bg-card text-foreground",
    success: "border-emerald-200 bg-emerald-50 text-emerald-700",
    danger: "border-destructive/30 bg-destructive/5 text-destructive",
    warning: "border-amber-200 bg-amber-50 text-amber-700",
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

function EmailLogRow({
  entry,
  isRetrying,
  onRetry,
}: {
  entry: EmailDeliveryLogEntry;
  isRetrying: boolean;
  onRetry(id: string): void;
}) {
  const isSent = entry.status === "Sent";
  const isPending = entry.status === "Queued" || entry.status === "Retrying";
  const canRetry = entry.status === "Failed";
  const statusClass = isSent
    ? "border-emerald-200 bg-emerald-50 text-emerald-700"
    : isPending
      ? "border-amber-200 bg-amber-50 text-amber-700"
      : "border-destructive/30 bg-destructive/5 text-destructive";
  const StatusIcon = isSent ? CheckCircle2 : isPending ? Clock3 : AlertTriangle;
  const relatedLabel = entry.relatedEntityLabel ?? entry.relatedEntityId ?? "—";

  return (
    <tr className="border-t border-border align-top hover:bg-muted/30">
      <td className="min-w-0 whitespace-normal break-words px-4 py-3 text-muted-foreground [overflow-wrap:anywhere]">
        {formatAdminDate(entry.createdAt)}
      </td>
      <td className="min-w-0 whitespace-normal break-words px-4 py-3 [overflow-wrap:anywhere]">
        <span className={`inline-flex max-w-full flex-wrap items-center gap-1.5 whitespace-normal break-words rounded-full border px-2 py-1 text-[9px] [overflow-wrap:anywhere] ${statusClass}`}>
          <StatusIcon size={11} className="shrink-0" aria-hidden="true" /> {entry.status}
        </span>
      </td>
      <td className="min-w-0 whitespace-normal break-words px-4 py-3 [overflow-wrap:anywhere]">
        <span className="whitespace-normal break-words text-foreground [overflow-wrap:anywhere]">{formatMessageTypeLabel(entry.messageType)}</span>
        <span className="mt-1 block min-w-0 whitespace-normal break-words font-mono text-[9px] text-muted-foreground [overflow-wrap:anywhere]">
          {entry.messageType}
        </span>
      </td>
      <td className="min-w-0 whitespace-normal break-words px-4 py-3 text-muted-foreground [overflow-wrap:anywhere]">
        {entry.recipientEmail}
      </td>
      <td className="min-w-0 whitespace-normal break-words px-4 py-3 text-foreground [overflow-wrap:anywhere]">{entry.subject}</td>
      <td className="min-w-0 whitespace-normal break-words px-4 py-3 [overflow-wrap:anywhere]">
        <span className="whitespace-normal break-words text-foreground [overflow-wrap:anywhere]">{entry.provider}</span>
        <span className="mt-1 block min-w-0 whitespace-normal break-words text-[9px] text-muted-foreground [overflow-wrap:anywhere]">
          {entry.sentExternally ? "External SMTP" : "Not external"}
        </span>
      </td>
      <td className="min-w-0 whitespace-normal break-words px-4 py-3 text-muted-foreground [overflow-wrap:anywhere]">
        <span className="block text-foreground">{entry.relatedEntityType ?? "—"}</span>
        <span className="block font-mono text-[9px]">{relatedLabel}</span>
      </td>
      <td className="min-w-0 whitespace-normal break-words px-4 py-3 text-muted-foreground [overflow-wrap:anywhere]">
        <span className="block min-w-0 whitespace-normal break-words [overflow-wrap:anywhere]">{entry.resultMessage}</span>
        {entry.errorMessage ? (
          <span className="mt-1 block min-w-0 whitespace-normal break-words text-destructive [overflow-wrap:anywhere]">{entry.errorMessage}</span>
        ) : null}
        {canRetry ? (
          <button
            type="button"
            onClick={() => onRetry(entry.id)}
            disabled={isRetrying}
            title="Queue manual retry for this failed email"
            aria-label="Queue manual retry for this failed email"
            className="mt-2 inline-flex items-center gap-1.5 border border-border bg-background px-3 py-1 text-[9px] uppercase tracking-wide hover:border-foreground disabled:opacity-50"
          >
            <RefreshCw size={11} className={isRetrying ? "animate-spin" : ""} aria-hidden="true" />
            {isRetrying ? "Retrying…" : "Retry"}
          </button>
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

function formatRetentionCleanupMessage(
  result: EmailOutboxRetentionCleanupResult,
): string {
  const bodiesPurged =
    result.succeededBodyPurgedCount + result.skippedBodyPurgedCount;
  const rowsDeleted =
    result.succeededDeletedCount + result.skippedDeletedCount;
  return `Retention cleanup completed: ${bodiesPurged} bodies purged, ${rowsDeleted} outbox rows deleted.`;
}

function EmailLogSectionHeader({
  title,
  subtitle,
  action,
}: {
  title: string;
  subtitle?: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-3">
      <div>
        <h3 className="font-serif text-[1.05rem] font-light text-foreground">{title}</h3>
        {subtitle ? (
          <p className="text-[10px] text-muted-foreground font-sans mt-0.5">{subtitle}</p>
        ) : null}
      </div>
      {action}
    </div>
  );
}

function EmailLogOperationButton({
  onClick,
  disabled,
  children,
}: {
  onClick(): void;
  disabled?: boolean;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="text-[10px] text-muted-foreground hover:text-foreground disabled:opacity-50 font-sans"
    >
      {children}
    </button>
  );
}

function EmailLogInlineNotice({
  tone,
  children,
  embedded = false,
}: {
  tone: "error" | "warning" | "success" | "neutral";
  children: ReactNode;
  embedded?: boolean;
}) {
  const toneClass = {
    error: "border-destructive/30 bg-destructive/5 text-destructive",
    warning: "border-amber-200 bg-amber-50 text-amber-700",
    success: "border-emerald-200 bg-emerald-50 text-emerald-700",
    neutral: "border-border bg-muted/40 text-muted-foreground",
  }[tone];
  const Icon =
    tone === "success"
      ? CheckCircle2
      : tone === "neutral"
        ? Clock3
        : AlertTriangle;

  return (
    <div
      className={`${embedded ? "" : "m-5 "}border px-4 py-3 text-[11px] flex items-start gap-2 ${toneClass}`}
    >
      <Icon size={14} className="mt-0.5 shrink-0" aria-hidden="true" />
      <span>{children}</span>
    </div>
  );
}
