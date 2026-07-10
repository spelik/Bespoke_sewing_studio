import { useMemo } from "react";
import {
  AlertTriangle,
  CheckCircle2,
  Mail,
  Package,
  ShieldCheck,
  type LucideIcon,
} from "lucide-react";
import type { EmailOutboxMonitoringSummary } from "../../api/emailDeliveryLogApi";
import type { AdminOrderListItem } from "../../api/ordersApi";
import type { AdminSection } from "../admin/adminSections";
import type {
  AdminContactMessageListItem,
  AdminSiteSettings,
  ProductionReadinessCheck,
  ProductionReadinessSummary,
} from "../types";
import type { AttentionCounts } from "./AdminAttention";
import {
  ADMIN_STATUS_LABELS,
  formatAdminDate,
} from "./adminOrderFormatting";

interface ProductionReadinessItem {
  label: string;
  status: "ready" | "warning" | "review";
  detail: string;
}

interface AdminDashboardOverviewProps {
  orders: readonly AdminOrderListItem[];
  contactMessages: readonly AdminContactMessageListItem[];
  orderAttentionCounts: AttentionCounts;
  contactAttentionCounts: AttentionCounts | null;
  emailOutboxSummary: EmailOutboxMonitoringSummary | null;
  emailOutboxSummaryError: string | null;
  isEmailOutboxSummaryLoading: boolean;
  siteSettings: AdminSiteSettings | null;
  siteSettingsError: string | null;
  productionReadiness: ProductionReadinessSummary | null;
  productionReadinessError: string | null;
  isProductionReadinessLoading: boolean;
  isOrdersLoading: boolean;
  ordersError: string | null;
  onOpenSection(section: AdminSection): void;
  onSelectOrder(id: string): void;
}

export function AdminDashboardOverview({
  orders,
  contactMessages,
  orderAttentionCounts,
  contactAttentionCounts,
  emailOutboxSummary,
  emailOutboxSummaryError,
  isEmailOutboxSummaryLoading,
  siteSettings,
  siteSettingsError,
  productionReadiness,
  productionReadinessError,
  isProductionReadinessLoading,
  isOrdersLoading,
  ordersError,
  onOpenSection,
  onSelectOrder,
}: AdminDashboardOverviewProps) {
  const recentOrders = useMemo(() => getRecentOrders(orders), [orders]);
  const recentContactMessages = useMemo(
    () => getRecentContactMessages(contactMessages),
    [contactMessages],
  );

  const contactCounts = contactAttentionCounts ?? {
    newCount: 0,
    totalCount: contactMessages.length,
  };
  const productionReadinessItems = useMemo(
    () =>
      buildProductionReadinessItems(
        siteSettings,
        siteSettingsError,
        isOrdersLoading,
        ordersError,
        emailOutboxSummary,
        emailOutboxSummaryError,
        isEmailOutboxSummaryLoading,
        productionReadiness,
        productionReadinessError,
        isProductionReadinessLoading,
      ),
    [
      siteSettings,
      siteSettingsError,
      isOrdersLoading,
      ordersError,
      emailOutboxSummary,
      emailOutboxSummaryError,
      isEmailOutboxSummaryLoading,
      productionReadiness,
      productionReadinessError,
      isProductionReadinessLoading,
    ],
  );

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-3">
        <DashboardStatCard
          icon={Package}
          label="New orders"
          value={orderAttentionCounts.newCount}
          caption={`${orderAttentionCounts.totalCount} total orders`}
          tone="accent"
          onClick={() => onOpenSection("orders")}
        />
        <DashboardStatCard
          icon={Mail}
          label="New contact messages"
          value={contactCounts.newCount}
          caption={`${contactCounts.totalCount} total messages`}
          tone="accent"
          onClick={() => onOpenSection("contactMessages")}
        />
        <DashboardStatusCard
          icon={Mail}
          label="Email outbox"
          title={getEmailOutboxTitle(
            emailOutboxSummary,
            emailOutboxSummaryError,
            isEmailOutboxSummaryLoading,
          )}
          caption={getEmailOutboxCaption(
            emailOutboxSummary,
            emailOutboxSummaryError,
            isEmailOutboxSummaryLoading,
          )}
          onClick={() => onOpenSection("emailLog")}
        />
        <DashboardStatusCard
          icon={ShieldCheck}
          label="Upload security"
          title="Quarantine enabled"
          caption="Order attachments are validated before acceptance and show scan status in Orders."
          onClick={() => onOpenSection("orders")}
        />
      </div>

      <section className="bg-card border border-border">
        <div className="px-5 py-4 border-b border-border flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              Production readiness
            </h2>
            <p className="text-[10px] text-muted-foreground font-sans mt-0.5">
              Quick checks before the site is deployed or handed over.
            </p>
          </div>
          <button
            type="button"
            onClick={() => onOpenSection("settings")}
            className="text-[10px] border border-border bg-background px-3 py-2 hover:border-foreground font-sans"
          >
            Open settings
          </button>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3 p-5">
          {productionReadinessItems.map((item) => (
            <ProductionReadinessCard key={item.label} item={item} />
          ))}
        </div>
      </section>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-5">
        <section className="bg-card border border-border">
          <div className="px-5 py-4 border-b border-border flex items-center justify-between gap-3">
            <div>
              <h2 className="font-serif text-[1.15rem] font-light text-foreground">
                Recent orders
              </h2>
              <p className="text-[10px] text-muted-foreground font-sans mt-0.5">
                Latest order requests needing review.
              </p>
            </div>
            <button
              type="button"
              onClick={() => onOpenSection("orders")}
              className="text-[10px] border border-border bg-background px-3 py-2 hover:border-foreground font-sans"
            >
              View all
            </button>
          </div>
          <div className="divide-y divide-border/60">
            {isOrdersLoading ? (
              <DashboardEmptyState message="Loading recent orders..." />
            ) : null}
            {!isOrdersLoading && recentOrders.length === 0 ? (
              <DashboardEmptyState message="No orders yet." />
            ) : null}
            {!isOrdersLoading
              ? recentOrders.map((order) => (
                  <button
                    key={order.id}
                    type="button"
                    onClick={() => onSelectOrder(order.id)}
                    className="w-full px-5 py-4 text-left hover:bg-secondary/30 transition-colors"
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div className="min-w-0">
                        <p className="text-[12px] text-foreground font-sans truncate">
                          {order.clientName}
                        </p>
                        <p className="text-[9px] text-muted-foreground font-mono mt-0.5">
                          {order.referenceNumber}
                        </p>
                        <p className="text-[10px] text-muted-foreground font-sans mt-1 truncate">
                          {order.serviceName}
                        </p>
                      </div>
                      <div className="shrink-0 text-right">
                        <span className="inline-flex text-[9px] px-2 py-0.5 bg-slate-100 text-slate-700 font-sans">
                          {ADMIN_STATUS_LABELS[order.status]}
                        </span>
                        <p className="text-[9px] text-muted-foreground font-sans mt-1.5">
                          {formatAdminDate(order.createdAt)}
                        </p>
                      </div>
                    </div>
                  </button>
                ))
              : null}
          </div>
        </section>

        <section className="bg-card border border-border">
          <div className="px-5 py-4 border-b border-border flex items-center justify-between gap-3">
            <div>
              <h2 className="font-serif text-[1.15rem] font-light text-foreground">
                Recent contact messages
              </h2>
              <p className="text-[10px] text-muted-foreground font-sans mt-0.5">
                Latest messages submitted through the Contact page.
              </p>
            </div>
            <button
              type="button"
              onClick={() => onOpenSection("contactMessages")}
              className="text-[10px] border border-border bg-background px-3 py-2 hover:border-foreground font-sans"
            >
              View all
            </button>
          </div>
          <div className="divide-y divide-border/60">
            {recentContactMessages.length === 0 ? (
              <DashboardEmptyState message="No contact messages yet." />
            ) : null}
            {recentContactMessages.map((message) => (
              <button
                key={message.id}
                type="button"
                onClick={() => onOpenSection("contactMessages")}
                className="w-full px-5 py-4 text-left hover:bg-secondary/30 transition-colors"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="min-w-0">
                    <p className="text-[12px] text-foreground font-sans truncate">
                      {message.fullName}
                    </p>
                    <p className="text-[9px] text-muted-foreground font-mono mt-0.5">
                      {message.referenceNumber}
                    </p>
                    <p className="text-[10px] text-muted-foreground font-sans mt-1 line-clamp-1">
                      {message.subject ?? message.messagePreview}
                    </p>
                  </div>
                  <div className="shrink-0 text-right">
                    <span className="inline-flex text-[9px] px-2 py-0.5 bg-slate-100 text-slate-700 font-sans">
                      {message.status}
                    </span>
                    <p className="text-[9px] text-muted-foreground font-sans mt-1.5">
                      {formatAdminDate(message.createdAt)}
                    </p>
                  </div>
                </div>
              </button>
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}

function DashboardStatCard({
  icon: Icon,
  label,
  value,
  caption,
  tone,
  onClick,
}: {
  icon: LucideIcon;
  label: string;
  value: number;
  caption: string;
  tone?: "accent";
  onClick(): void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="bg-card border border-border px-5 py-4 text-left hover:border-foreground transition-colors"
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-[10px] uppercase tracking-wider text-muted-foreground font-sans">
            {label}
          </p>
          <p
            className={`mt-1 text-[1.65rem] font-serif font-light ${tone === "accent" ? "text-rose-700" : "text-foreground"}`}
          >
            {value}
          </p>
          <p className="text-[10px] text-muted-foreground font-sans mt-1">
            {caption}
          </p>
        </div>
        <Icon size={17} className="text-muted-foreground" />
      </div>
    </button>
  );
}

function DashboardStatusCard({
  icon: Icon,
  label,
  title,
  caption,
  onClick,
}: {
  icon: LucideIcon;
  label: string;
  title: string;
  caption: string;
  onClick(): void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="bg-card border border-border px-5 py-4 text-left hover:border-foreground transition-colors"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-[10px] uppercase tracking-wider text-muted-foreground font-sans">
            {label}
          </p>
          <p className="mt-2 text-[13px] text-foreground font-sans break-words leading-4">
            {title}
          </p>
          <p className="text-[10px] text-muted-foreground font-sans mt-1 leading-4 break-words">
            {caption}
          </p>
        </div>
        <Icon size={17} className="text-muted-foreground" />
      </div>
    </button>
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

function DashboardEmptyState({ message }: { message: string }) {
  return (
    <p className="px-5 py-8 text-center text-[11px] text-muted-foreground font-sans">
      {message}
    </p>
  );
}

function getRecentOrders(
  orders: readonly AdminOrderListItem[],
): AdminOrderListItem[] {
  return [...orders]
    .sort(
      (first, second) =>
        new Date(second.createdAt).getTime() -
        new Date(first.createdAt).getTime(),
    )
    .slice(0, 5);
}

function getRecentContactMessages(
  messages: readonly AdminContactMessageListItem[],
): AdminContactMessageListItem[] {
  return [...messages]
    .sort(
      (first, second) =>
        new Date(second.createdAt).getTime() -
        new Date(first.createdAt).getTime(),
    )
    .slice(0, 5);
}

function buildProductionReadinessItems(
  siteSettings: AdminSiteSettings | null,
  siteSettingsError: string | null,
  isOrdersLoading: boolean,
  ordersError: string | null,
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
  const backendEmailDelivery = backendReadinessByKey.get("emailDelivery");
  const backendEmailOutbox = backendReadinessByKey.get("emailOutbox");
  const backendUploadSecurity = backendReadinessByKey.get("uploadSecurity");
  const backendDnsEmailRecords = backendReadinessByKey.get("dnsEmailRecords");

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
          : "Add the public email and phone in Settings → Contact.",
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
        backendEmailDelivery,
        productionReadinessError,
        isProductionReadinessLoading,
        "Email delivery readiness has not been checked yet.",
      ),
    },
    {
      label: "Email outbox",
      ...resolveBackendReadinessItem(
        backendEmailOutbox,
        productionReadinessError ?? emailOutboxSummaryError,
        isProductionReadinessLoading || isEmailOutboxSummaryLoading,
        emailOutboxSummary?.summaryMessage ?? "Checking email outbox health.",
      ),
    },
    {
      label: "Upload security",
      ...resolveBackendReadinessItem(
        backendUploadSecurity,
        productionReadinessError,
        isProductionReadinessLoading,
        "ClamAV readiness has not been checked yet.",
      ),
    },
    {
      label: "Admin data API",
      status: ordersError || isOrdersLoading ? "review" : "ready",
      detail: ordersError
        ? "Admin data could not be loaded. Check the backend and database connection."
        : isOrdersLoading
          ? "Checking admin data access."
          : "Orders and admin data are loading from the backend API.",
    },
    {
      label: "DNS email records",
      ...resolveBackendReadinessItem(
        backendDnsEmailRecords,
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
      detail: check.missing.length > 0 ? (check.missing[0] ?? check.detail) : check.detail,
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
    detail: isLoading ? "Checking production readiness." : fallbackDetail,
  };
}

function getEmailOutboxTitle(
  summary: EmailOutboxMonitoringSummary | null,
  error: string | null,
  isLoading: boolean,
): string {
  if (error) {
    return "Email monitoring unavailable";
  }

  if (isLoading && !summary) {
    return "Checking email...";
  }

  if (!summary) {
    return "Checking email...";
  }

  if (summary.healthStatus === "Critical") {
    return "Email issues";
  }

  if (summary.healthStatus === "Warning") {
    return "Email needs review";
  }

  return "Email healthy";
}

function getEmailOutboxCaption(
  summary: EmailOutboxMonitoringSummary | null,
  error: string | null,
  isLoading: boolean,
): string {
  if (error) {
    return "Email outbox monitoring is unavailable. Open Email Log to review.";
  }

  if (isLoading && !summary) {
    return "Checking outbox health.";
  }

  if (!summary) {
    return "Checking outbox health.";
  }

  if (summary.healthStatus === "Critical") {
    return summary.summaryMessage || "Review failed emails in Email Log.";
  }

  if (summary.healthStatus === "Warning") {
    return summary.summaryMessage || "Review outbox status in Email Log.";
  }

  return `${summary.sentLast24HoursCount} sent in last 24h`;
}
