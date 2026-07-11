import { AlertTriangle, CheckCircle2 } from "lucide-react";
import type { EmailOutboxMonitoringSummary } from "../../api/emailDeliveryLogApi";
import type {
  AdminSiteSettings,
  ProductionReadinessCheck,
  ProductionReadinessSummary,
} from "../types";
import { formatAdminDate } from "./adminOrderFormatting";

interface ProductionReadinessItem {
  label: string;
  status: "ready" | "warning" | "review";
  detail: string;
}

interface AdminProductionHealthPanelProps {
  siteSettings: AdminSiteSettings | null;
  siteSettingsError: string | null;
  productionReadiness: ProductionReadinessSummary | null;
  productionReadinessError: string | null;
  isProductionReadinessLoading: boolean;
  emailOutboxSummary: EmailOutboxMonitoringSummary | null;
  emailOutboxSummaryError: string | null;
  isEmailOutboxSummaryLoading: boolean;
  isAdminDataLoading: boolean;
  adminDataError: string | null;
  onOpenBusinessInfo(): void;
  onOpenSystemSettings(): void;
  onOpenEmailLog(): void;
  onOpenStorage(): void;
}

export function AdminProductionHealthPanel({
  siteSettings,
  siteSettingsError,
  productionReadiness,
  productionReadinessError,
  isProductionReadinessLoading,
  emailOutboxSummary,
  emailOutboxSummaryError,
  isEmailOutboxSummaryLoading,
  isAdminDataLoading,
  adminDataError,
  onOpenBusinessInfo,
  onOpenSystemSettings,
  onOpenEmailLog,
  onOpenStorage,
}: AdminProductionHealthPanelProps) {
  const items = buildProductionReadinessItems(
    siteSettings,
    siteSettingsError,
    isAdminDataLoading,
    adminDataError,
    emailOutboxSummary,
    emailOutboxSummaryError,
    isEmailOutboxSummaryLoading,
    productionReadiness,
    productionReadinessError,
    isProductionReadinessLoading,
  );
  const warningCount = items.filter((item) => item.status !== "ready").length;

  return (
    <div className="space-y-6">
      <section className="bg-card border border-border">
        <div className="p-5 border-b border-border flex flex-wrap items-start justify-between gap-4">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              Production health checks
            </h2>
            <p className="text-[10px] text-muted-foreground font-sans mt-1 max-w-2xl">
              Technical readiness for the live website. This page is for deployment,
              email delivery, upload scanning and backend connectivity checks.
            </p>
          </div>
          <div className="border border-border bg-background px-4 py-3 text-[10px] font-sans text-muted-foreground">
            <span className="block uppercase tracking-[0.18em] text-[9px]">
              Status
            </span>
            <span className="mt-1 block text-foreground">
              {warningCount > 0
                ? `${warningCount} check${warningCount === 1 ? "" : "s"} need review`
                : "System healthy"}
            </span>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3 p-5">
          {items.map((item) => (
            <ProductionReadinessCard key={item.label} item={item} />
          ))}
        </div>

        {productionReadiness ? (
          <div className="border-t border-border px-5 py-3 text-[9px] text-muted-foreground font-sans">
            Last backend readiness check generated{" "}
            {formatAdminDate(productionReadiness.generatedAt)}.
          </div>
        ) : null}
      </section>

      <section className="bg-card border border-border p-5">
        <h2 className="font-serif text-[1.05rem] font-light text-foreground">
          Where to fix issues
        </h2>
        <div className="mt-4 grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-3">
          <QuickLink
            title="Business Info"
            description="Public email, phone and studio details."
            onClick={onOpenBusinessInfo}
          />
          <QuickLink
            title="System Settings"
            description="Email provider, notifications and confirmations."
            onClick={onOpenSystemSettings}
          />
          <QuickLink
            title="Email Log"
            description="Delivery retries, failed email and outbox health."
            onClick={onOpenEmailLog}
          />
          <QuickLink
            title="Storage"
            description="Uploads, orphan files and cleanup jobs."
            onClick={onOpenStorage}
          />
        </div>
      </section>
    </div>
  );
}

function ProductionReadinessCard({ item }: { item: ProductionReadinessItem }) {
  const isReady = item.status === "ready";
  const Icon = isReady ? CheckCircle2 : AlertTriangle;

  return (
    <div className="border border-border bg-background px-4 py-3">
      <div className="flex items-start gap-3">
        <span
          className={`mt-0.5 inline-flex h-6 w-6 shrink-0 items-center justify-center rounded-full ${isReady ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}
        >
          <Icon size={13} aria-hidden="true" />
        </span>
        <div className="min-w-0">
          <p className="text-[11px] text-foreground font-sans">{item.label}</p>
          <p className="text-[10px] text-muted-foreground font-sans leading-4 mt-1">
            {item.detail}
          </p>
        </div>
      </div>
    </div>
  );
}

function QuickLink({
  title,
  description,
  onClick,
}: {
  title: string;
  description: string;
  onClick(): void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="border border-border bg-background px-4 py-3 text-left hover:border-foreground transition-colors"
    >
      <span className="block text-[11px] text-foreground font-sans">
        {title}
      </span>
      <span className="block text-[10px] text-muted-foreground font-sans mt-1 leading-4">
        {description}
      </span>
    </button>
  );
}

function buildProductionReadinessItems(
  siteSettings: AdminSiteSettings | null,
  siteSettingsError: string | null,
  isAdminDataLoading: boolean,
  adminDataError: string | null,
  emailOutboxSummary: EmailOutboxMonitoringSummary | null,
  emailOutboxSummaryError: string | null,
  isEmailOutboxSummaryLoading: boolean,
  productionReadiness: ProductionReadinessSummary | null,
  productionReadinessError: string | null,
  isProductionReadinessLoading: boolean,
): ProductionReadinessItem[] {
  const settingsUnavailable = Boolean(siteSettingsError);
  const emailConfigured = Boolean(siteSettings?.email?.trim());
  const phoneConfigured = Boolean(siteSettings?.phone?.trim());
  const ownerNotificationsReady = Boolean(
    siteSettings?.emailNotificationsEnabled && emailConfigured,
  );
  const customerConfirmationsReady = Boolean(
    siteSettings?.customerConfirmationEmailsEnabled,
  );
  const backendReadinessByKey = new Map(
    productionReadiness?.checks.map((check) => [check.key, check]) ?? [],
  );

  return [
    {
      label: "Public contact details",
      status:
        !settingsUnavailable && emailConfigured && phoneConfigured
          ? "ready"
          : "warning",
      detail: settingsUnavailable
        ? (siteSettingsError ?? "Site settings could not be loaded.")
        : emailConfigured && phoneConfigured
          ? "Public email and phone are configured."
          : "Add the public email and phone in Business Info.",
    },
    {
      label: "Owner notifications",
      status: ownerNotificationsReady ? "ready" : "warning",
      detail: ownerNotificationsReady
        ? "New orders and contact messages can notify the owner."
        : "Enable new-request notifications and confirm the owner email.",
    },
    {
      label: "Customer confirmations",
      status: customerConfirmationsReady ? "ready" : "review",
      detail: customerConfirmationsReady
        ? "Automatic customer confirmation emails are enabled."
        : "Review whether customers should receive automatic confirmations.",
    },
    {
      label: "Email delivery",
      ...resolveBackendReadinessItem(
        backendReadinessByKey.get("emailDelivery"),
        productionReadinessError,
        isProductionReadinessLoading,
        "Email delivery readiness has not been checked yet.",
      ),
    },
    {
      label: "Email outbox",
      ...resolveBackendReadinessItem(
        backendReadinessByKey.get("emailOutbox"),
        productionReadinessError ?? emailOutboxSummaryError,
        isProductionReadinessLoading || isEmailOutboxSummaryLoading,
        emailOutboxSummary?.summaryMessage ?? "Checking email outbox health.",
      ),
    },
    {
      label: "Upload scanning",
      ...resolveBackendReadinessItem(
        backendReadinessByKey.get("uploadSecurity"),
        productionReadinessError,
        isProductionReadinessLoading,
        "ClamAV readiness has not been checked yet.",
      ),
    },
    {
      label: "Admin data API",
      status: adminDataError || isAdminDataLoading ? "review" : "ready",
      detail: adminDataError
        ? "Admin data could not be loaded. Check the backend and database connection."
        : isAdminDataLoading
          ? "Checking admin data access."
          : "Orders and admin data are loading from the backend API.",
    },
    {
      label: "DNS email records",
      ...resolveBackendReadinessItem(
        backendReadinessByKey.get("dnsEmailRecords"),
        productionReadinessError,
        isProductionReadinessLoading,
        "DNS email records have not been checked yet.",
      ),
    },
  ];
}

function resolveBackendReadinessItem(
  check: ProductionReadinessCheck | undefined,
  error: string | null,
  isLoading: boolean,
  fallbackDetail: string,
): Pick<ProductionReadinessItem, "status" | "detail"> {
  if (check) {
    return {
      status: check.status === "ready" ? "ready" : "warning",
      detail:
        check.missing.length > 0
          ? (check.missing[0] ?? check.detail)
          : check.detail,
    };
  }

  if (error) {
    return {
      status: "warning",
      detail: error,
    };
  }

  return {
    status: "review",
    detail: isLoading ? "Checking production health." : fallbackDetail,
  };
}
