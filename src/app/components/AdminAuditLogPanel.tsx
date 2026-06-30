import { useCallback, useEffect, useMemo, useState } from "react";
import { Download, History, LoaderCircle, Search, X } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  getAdminAuditLog,
  getAdminAuditLogErrorMessage,
  type AdminAuditLogEntry,
} from "../../api/adminAuditLogApi";
import { formatAdminDate } from "./adminOrderFormatting";
import { createCsvFileName, downloadCsv } from "../utils/csvExport";

interface AdminAuditLogPanelProps {
  onUnauthorized(): void;
}

interface AdminAuditLogFilters {
  search: string;
  action: string;
  entityType: string;
  actorEmail: string;
  take: number;
}

interface FilterOption {
  value: string;
  label: string;
  meta?: string;
}

const TAKE_OPTIONS = [50, 100, 200] as const;

const KNOWN_AUDIT_ACTIONS = [
  "admin_user.created",
  "admin_user.enabled",
  "admin_user.disabled",
  "admin_user.password_reset",
  "admin_user.deleted",
  "order.status_updated",
  "order.note_added",
  "contact_message.status_updated",
  "site_settings.updated",
  "email_delivery_settings.updated",
  "brand_settings.updated",
] as const;

const KNOWN_ENTITY_TYPES = [
  "AdminUser",
  "Order",
  "ContactMessage",
  "SiteSettings",
  "EmailDeliverySettings",
  "BrandSettings",
] as const;

type AuditFilterDropdownId = "action" | "entityType" | "take";

export function AdminAuditLogPanel({ onUnauthorized }: AdminAuditLogPanelProps) {
  const [entries, setEntries] = useState<AdminAuditLogEntry[]>([]);
  const [search, setSearch] = useState("");
  const [action, setAction] = useState("");
  const [entityType, setEntityType] = useState("");
  const [actorEmail, setActorEmail] = useState("");
  const [take, setTake] = useState<(typeof TAKE_OPTIONS)[number]>(100);
  const [openFilter, setOpenFilter] = useState<AuditFilterDropdownId | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const filters = useMemo<AdminAuditLogFilters>(
    () => ({ search, action, entityType, actorEmail, take }),
    [actorEmail, action, entityType, search, take],
  );

  const loadEntries = useCallback(
    async (overrides?: Partial<AdminAuditLogFilters>) => {
      const request = { ...filters, ...overrides };

      setIsLoading(true);
      setError(null);

      try {
        const result = await getAdminAuditLog(request);
        setEntries(result);
      } catch (reason: unknown) {
        if (
          reason instanceof ApiError &&
          (reason.status === 401 || reason.status === 403)
        ) {
          onUnauthorized();
          return;
        }

        setError(getAdminAuditLogErrorMessage(reason));
      } finally {
        setIsLoading(false);
      }
    },
    [filters, onUnauthorized],
  );

  useEffect(() => {
    const delay = search.trim().length > 0 || actorEmail.trim().length > 0 ? 300 : 0;
    const timeoutId = window.setTimeout(() => {
      void loadEntries();
    }, delay);

    return () => window.clearTimeout(timeoutId);
  }, [actorEmail, loadEntries, search]);

  const actionOptions = useMemo(
    () =>
      buildDropdownOptions(
        [...KNOWN_AUDIT_ACTIONS, action, ...entries.map((entry) => entry.action)],
        formatAuditActionLabel,
      ),
    [action, entries],
  );

  const entityTypeOptions = useMemo(
    () =>
      buildDropdownOptions(
        [...KNOWN_ENTITY_TYPES, entityType, ...entries.map((entry) => entry.entityType)],
        formatEntityTypeLabel,
      ),
    [entries, entityType],
  );

  const takeOptions = useMemo(
    () =>
      TAKE_OPTIONS.map((value) => ({
        value: String(value),
        label: String(value),
      })),
    [],
  );

  const visibleFiltersCount = [search, action, entityType, actorEmail].filter(
    (value) => value.trim().length > 0,
  ).length;

  function handleClearFilters() {
    setSearch("");
    setAction("");
    setEntityType("");
    setActorEmail("");
    setOpenFilter(null);
  }

  function handleExportCsv() {
    downloadCsv(createCsvFileName("admin-audit-log"), entries, [
      { header: "Created", value: (entry) => formatAdminDate(entry.createdAt) },
      { header: "Actor", value: (entry) => entry.actorEmail },
      { header: "Action", value: (entry) => entry.action },
      { header: "Entity type", value: (entry) => entry.entityType },
      { header: "Entity label", value: (entry) => entry.entityLabel ?? "" },
      { header: "Entity id", value: (entry) => entry.entityId ?? "" },
      { header: "Summary", value: (entry) => entry.summary },
    ]);
  }

  return (
    <div className="space-y-5">
      <section className="bg-card border border-border">
        <div className="p-5 border-b border-border flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              Admin audit log
            </h2>
            <p className="text-[10px] text-muted-foreground font-sans mt-1 max-w-2xl">
              Review important admin actions such as user management, status changes and settings updates.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={handleExportCsv}
              disabled={entries.length === 0}
              className="inline-flex items-center gap-2 px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50 font-sans"
            >
              <Download size={12} /> Export CSV
            </button>
            <button
              type="button"
              onClick={() => void loadEntries()}
              disabled={isLoading}
              className="px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50 font-sans"
            >
              Refresh
            </button>
          </div>
        </div>

        <div className="p-5 border-b border-border space-y-3">
          <div className="grid grid-cols-1 lg:grid-cols-[1.25fr_0.95fr_0.9fr_0.95fr_0.45fr] gap-3 items-end">
            <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
              Search
              <div className="relative mt-1">
                <Search
                  size={13}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground"
                />
                <input
                  type="text"
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Actor, action, entity, reference, summary..."
                  className="w-full border border-border bg-background pl-8 pr-8 py-2 text-[10px] text-foreground focus:outline-none focus:border-accent"
                />
                {search ? (
                  <button
                    type="button"
                    onClick={() => setSearch("")}
                    className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-muted-foreground hover:text-foreground"
                    aria-label="Clear audit log search"
                  >
                    <X size={12} />
                  </button>
                ) : null}
              </div>
            </label>

            <StyledFilterDropdown
              label="Action"
              value={action}
              placeholder="Any action"
              options={actionOptions}
              isOpen={openFilter === "action"}
              onToggle={() => setOpenFilter((current) => (current === "action" ? null : "action"))}
              onClose={() => setOpenFilter(null)}
              onChange={setAction}
            />

            <StyledFilterDropdown
              label="Entity"
              value={entityType}
              placeholder="Any entity"
              options={entityTypeOptions}
              isOpen={openFilter === "entityType"}
              onToggle={() => setOpenFilter((current) => (current === "entityType" ? null : "entityType"))}
              onClose={() => setOpenFilter(null)}
              onChange={setEntityType}
            />

            <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
              Actor email
              <input
                type="email"
                value={actorEmail}
                onChange={(event) => setActorEmail(event.target.value)}
                placeholder="admin@example.com"
                className="mt-1 w-full border border-border bg-background px-3 py-2 text-[10px] text-foreground focus:outline-none focus:border-accent"
              />
            </label>

            <StyledFilterDropdown
              label="Limit"
              value={String(take)}
              placeholder="Limit"
              options={takeOptions}
              allowEmpty={false}
              isOpen={openFilter === "take"}
              onToggle={() => setOpenFilter((current) => (current === "take" ? null : "take"))}
              onClose={() => setOpenFilter(null)}
              onChange={(value) => setTake(Number(value) as typeof take)}
            />
          </div>

          <div className="flex flex-wrap items-center gap-3 text-[10px] text-muted-foreground font-sans">
            <span>Filters apply automatically.</span>
            {visibleFiltersCount > 0 ? (
              <button
                type="button"
                onClick={handleClearFilters}
                className="hover:text-foreground"
              >
                Clear {visibleFiltersCount} filter{visibleFiltersCount === 1 ? "" : "s"}
              </button>
            ) : null}
          </div>
        </div>

        {error ? (
          <div role="alert" className="mx-5 mt-5 border border-destructive/30 bg-destructive/5 px-4 py-3 text-[11px] text-destructive font-sans">
            {error}
          </div>
        ) : null}

        <div className="p-5">
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2 text-[10px] text-muted-foreground font-sans">
            <span>{entries.length} audit entr{entries.length === 1 ? "y" : "ies"}</span>
            <span>Newest first</span>
          </div>

          {isLoading ? (
            <div className="flex items-center gap-2 text-[11px] text-muted-foreground font-sans">
              <LoaderCircle size={14} className="animate-spin" /> Loading audit log...
            </div>
          ) : null}

          {!isLoading && entries.length === 0 ? (
            <div className="border border-dashed border-border p-6 text-[11px] text-muted-foreground font-sans">
              No audit log entries were found.
            </div>
          ) : null}

          {entries.length > 0 ? (
            <div className="overflow-x-auto border border-border">
              <table className="w-full text-left text-[11px] font-sans">
                <thead className="bg-muted/40 text-muted-foreground uppercase tracking-wide text-[9px]">
                  <tr>
                    <th className="px-3 py-2 font-medium">Time</th>
                    <th className="px-3 py-2 font-medium">Actor</th>
                    <th className="px-3 py-2 font-medium">Action</th>
                    <th className="px-3 py-2 font-medium">Entity</th>
                    <th className="px-3 py-2 font-medium">Summary</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {entries.map((entry) => (
                    <tr key={entry.id} className="align-top">
                      <td className="px-3 py-3 whitespace-nowrap text-muted-foreground">
                        {formatAdminDate(entry.createdAt)}
                      </td>
                      <td className="px-3 py-3 min-w-[180px]">
                        <div className="font-medium text-foreground truncate">
                          {entry.actorEmail}
                        </div>
                      </td>
                      <td className="px-3 py-3 min-w-[180px]">
                        <span className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2 py-1 text-[9px] text-slate-700">
                          <History size={10} /> {entry.action}
                        </span>
                      </td>
                      <td className="px-3 py-3 min-w-[170px] text-muted-foreground">
                        <div className="text-foreground">{entry.entityType}</div>
                        {entry.entityLabel ? (
                          <div className="mt-1 text-[9px] font-mono text-muted-foreground">
                            {entry.entityLabel}
                          </div>
                        ) : null}
                      </td>
                      <td className="px-3 py-3 min-w-[280px] text-foreground">
                        {entry.summary}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </div>
      </section>
    </div>
  );
}

function StyledFilterDropdown({
  label,
  value,
  placeholder,
  options,
  allowEmpty = true,
  isOpen,
  onToggle,
  onClose,
  onChange,
}: {
  label: string;
  value: string;
  placeholder: string;
  options: FilterOption[];
  allowEmpty?: boolean;
  isOpen: boolean;
  onToggle(): void;
  onClose(): void;
  onChange(value: string): void;
}) {
  const selectedOption = options.find((option) => option.value === value);
  const displayLabel = selectedOption?.label ?? (value ? value : placeholder);

  return (
    <div
      className="relative text-[10px] tracking-wide text-muted-foreground font-sans"
      onBlur={(event) => {
        const nextTarget = event.relatedTarget as Node | null;
        if (nextTarget && event.currentTarget.contains(nextTarget)) {
          return;
        }

        onClose();
      }}
    >
      <span>{label}</span>
      <button
        type="button"
        onClick={onToggle}
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            onClose();
          }
        }}
        className="mt-1 flex w-full items-center justify-between gap-3 border border-border bg-background px-3 py-2 text-left text-[10px] text-foreground hover:border-foreground focus:outline-none focus:border-accent"
        aria-expanded={isOpen}
      >
        <span className={value ? "truncate" : "truncate text-muted-foreground"}>
          {displayLabel}
        </span>
        <span className="text-[9px] text-muted-foreground" aria-hidden="true">
          ▾
        </span>
      </button>

      {isOpen ? (
        <div className="absolute left-0 right-0 top-full z-40 mt-1 max-h-64 overflow-y-auto border border-border bg-background shadow-[0_18px_45px_rgba(0,0,0,0.16)]">
          {allowEmpty ? (
            <button
              type="button"
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => {
                onChange("");
                onClose();
              }}
              className={`flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-[10px] hover:bg-muted ${
                value === "" ? "bg-muted/50 text-foreground" : "text-muted-foreground"
              }`}
            >
              {placeholder}
              {value === "" ? <span aria-hidden="true">✓</span> : null}
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

function formatAuditActionLabel(value: string): string {
  const parts = value.split(".");
  const group = parts[0] ?? value;
  const actionName = parts.slice(1).join(".");

  if (!actionName) {
    return prettifyToken(value);
  }

  return `${prettifyToken(group)} · ${prettifyToken(actionName)}`;
}

function formatEntityTypeLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function prettifyToken(value: string): string {
  return value
    .replace(/_/g, " ")
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
