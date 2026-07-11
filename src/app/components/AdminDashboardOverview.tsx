import { useMemo } from "react";
import {
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
  ProductionReadinessSummary,
} from "../types";
import type { AttentionCounts } from "./AdminAttention";
import {
  ADMIN_STATUS_LABELS,
  formatAdminDate,
} from "./adminOrderFormatting";

interface AdminDashboardOverviewProps {
  orders: readonly AdminOrderListItem[];
  contactMessages: readonly AdminContactMessageListItem[];
  orderAttentionCounts: AttentionCounts;
  contactAttentionCounts: AttentionCounts | null;
  emailOutboxSummary: EmailOutboxMonitoringSummary | null;
  emailOutboxSummaryError: string | null;
  isEmailOutboxSummaryLoading: boolean;
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
          label="System status"
          title={getProductionHealthTitle(
            productionReadiness,
            productionReadinessError,
            isProductionReadinessLoading,
          )}
          caption={getProductionHealthCaption(
            productionReadiness,
            productionReadinessError,
            isProductionReadinessLoading,
          )}
          onClick={() => onOpenSection("productionHealth")}
        />
      </div>

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

function getProductionHealthTitle(
  summary: ProductionReadinessSummary | null,
  error: string | null,
  isLoading: boolean,
): string {
  if (error) {
    return "Needs attention";
  }

  if (isLoading && !summary) {
    return "Checking system...";
  }

  if (!summary) {
    return "Check system";
  }

  const hasIssues = summary.checks.some((check) => check.status !== "ready");
  return hasIssues ? "Needs attention" : "System healthy";
}

function getProductionHealthCaption(
  summary: ProductionReadinessSummary | null,
  error: string | null,
  isLoading: boolean,
): string {
  if (error) {
    return "Open Production Health to review deployment checks.";
  }

  if (isLoading && !summary) {
    return "Checking deployment and backend readiness.";
  }

  if (!summary) {
    return "Open Production Health for deployment checks.";
  }

  const warningCount = summary.checks.filter(
    (check) => check.status !== "ready",
  ).length;
  if (warningCount > 0) {
    return `${warningCount} check${warningCount === 1 ? "" : "s"} need review.`;
  }

  return "Open Production Health for technical checks.";
}
