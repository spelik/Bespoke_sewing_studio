import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Download,
  FileText,
  Images,
  LayoutDashboard,
  ListOrdered,
  LogOut,
  Mail,
  Menu,
  Package,
  Palette,
  Scissors,
  ShieldCheck,
  Search,
  Settings,
  CheckCircle2,
  AlertTriangle,
  X,
} from "lucide-react";
import { ApiError } from "../../api/apiClient";
import { getAdminContactMessages } from "../../api/contactMessagesApi";
import {
  getAdminEmailDeliverySettings,
  getAdminSiteSettings,
} from "../../api/siteSettingsApi";
import {
  ORDER_STATUSES,
  type AdminOrderListItem,
  type AdminOrderStatus,
} from "../../api/ordersApi";
import { useAuth } from "../auth/AuthContext";
import { AdminBrandSettingsPanel } from "../components/AdminBrandSettingsPanel";
import { AdminContactMessagesPanel } from "../components/AdminContactMessagesPanel";
import { AdminContentPanel } from "../components/AdminContentPanel";
import { AdminOrderDetail } from "../components/AdminOrderDetail";
import { AdminOrdersTable } from "../components/AdminOrdersTable";
import { AdminPortfolioPanel } from "../components/AdminPortfolioPanel";
import { AdminRepeatableContentPanel } from "../components/AdminRepeatableContentPanel";
import { AdminServicesPanel } from "../components/AdminServicesPanel";
import { AdminSettingsPanel } from "../components/AdminSettingsPanel";
import {
  ADMIN_STATUS_LABELS,
  formatAdminDate,
} from "../components/adminOrderFormatting";
import { useAdminOrders } from "../hooks/useAdminOrders";
import { useAdminRealtimeUpdates } from "../hooks/useAdminRealtimeUpdates";
import { usePageNavigation } from "../routing/usePageNavigation";
import { createCsvFileName, downloadCsv } from "../utils/csvExport";
import type {
  AdminContactMessageListItem,
  AdminEmailDeliverySettings,
  AdminSiteSettings,
} from "../types";

type AdminSection =
  | "dashboard"
  | "orders"
  | "contactMessages"
  | "services"
  | "portfolio"
  | "content"
  | "repeatable"
  | "brand"
  | "settings";

interface AttentionCounts {
  newCount: number;
  totalCount: number;
}

interface ProductionReadinessItem {
  label: string;
  status: "ready" | "warning" | "review";
  detail: string;
}

const NAV_ITEMS: ReadonlyArray<{
  id: AdminSection;
  label: string;
  icon: typeof Package;
}> = [
  { id: "dashboard", label: "Dashboard", icon: LayoutDashboard },
  { id: "orders", label: "Orders", icon: Package },
  { id: "contactMessages", label: "Contact Messages", icon: Mail },
  { id: "services", label: "Services", icon: Scissors },
  { id: "portfolio", label: "Portfolio", icon: Images },
  { id: "content", label: "Content", icon: FileText },
  { id: "repeatable", label: "Repeatable Content", icon: ListOrdered },
  { id: "brand", label: "Brand / SEO", icon: Palette },
  { id: "settings", label: "Settings", icon: Settings },
];

export function AdminPage() {
  const navigate = usePageNavigation();
  const { user, logout } = useAuth();
  const adminOrders = useAdminOrders(logout);
  const [section, setSection] = useState<AdminSection>("dashboard");
  const [orderFilter, setOrderFilter] = useState<AdminOrderStatus | "All">(
    "All",
  );
  const [orderSearchQuery, setOrderSearchQuery] = useState("");
  const [contactMessages, setContactMessages] = useState<AdminContactMessageListItem[]>([]);
  const [contactRefreshKey, setContactRefreshKey] = useState(0);
  const [contactAttentionCounts, setContactAttentionCounts] =
    useState<AttentionCounts | null>(null);
  const [emailDeliverySettings, setEmailDeliverySettings] =
    useState<AdminEmailDeliverySettings | null>(null);
  const [emailDeliveryError, setEmailDeliveryError] = useState<string | null>(null);
  const [siteSettings, setSiteSettings] = useState<AdminSiteSettings | null>(null);
  const [siteSettingsError, setSiteSettingsError] = useState<string | null>(null);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const orderAttentionCounts = useMemo(
    () => calculateOrderAttentionCounts(adminOrders.orders),
    [adminOrders.orders],
  );

  const loadContactMessagesForDashboard = useCallback(async () => {
    try {
      const messages = await getAdminContactMessages();
      setContactMessages(messages);
      setContactAttentionCounts(calculateContactAttentionCounts(messages));
    } catch (reason: unknown) {
      if (
        reason instanceof ApiError &&
        (reason.status === 401 || reason.status === 403)
      ) {
        logout();
      }
    }
  }, [logout]);

  useEffect(() => {
    void loadContactMessagesForDashboard();
  }, [loadContactMessagesForDashboard]);

  const handleAdminRealtimeEvent = useCallback(
    (event: { entity: "Order" | "ContactMessage" }) => {
      if (event.entity === "Order") {
        void adminOrders.reload();
        return;
      }

      void loadContactMessagesForDashboard();
      setContactRefreshKey((current) => current + 1);
    },
    [adminOrders.reload, loadContactMessagesForDashboard],
  );

  const adminRealtime = useAdminRealtimeUpdates({
    enabled: Boolean(user),
    onEvent: handleAdminRealtimeEvent,
  });

  useEffect(() => {
    let cancelled = false;

    async function loadSiteSettings() {
      setSiteSettingsError(null);
      try {
        const settings = await getAdminSiteSettings();
        if (!cancelled) {
          setSiteSettings(settings);
        }
      } catch (reason: unknown) {
        if (
          reason instanceof ApiError &&
          (reason.status === 401 || reason.status === 403)
        ) {
          logout();
          return;
        }

        if (!cancelled) {
          setSiteSettingsError("Site settings could not be loaded.");
        }
      }
    }

    void loadSiteSettings();

    return () => {
      cancelled = true;
    };
  }, [logout]);

  useEffect(() => {
    let cancelled = false;

    async function loadEmailDeliverySettings() {
      setEmailDeliveryError(null);
      try {
        const settings = await getAdminEmailDeliverySettings();
        if (!cancelled) {
          setEmailDeliverySettings(settings);
        }
      } catch (reason: unknown) {
        if (
          reason instanceof ApiError &&
          (reason.status === 401 || reason.status === 403)
        ) {
          logout();
          return;
        }

        if (!cancelled) {
          setEmailDeliveryError("Email delivery status could not be loaded.");
        }
      }
    }

    void loadEmailDeliverySettings();

    return () => {
      cancelled = true;
    };
  }, [logout]);

  const filteredOrders = useMemo(() => {
    const normalizedQuery = normalizeSearchValue(orderSearchQuery);
    return adminOrders.orders.filter((order) => {
      if (orderFilter !== "All" && order.status !== orderFilter) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      return [
        order.referenceNumber,
        order.clientName,
        order.clientEmail,
        order.clientPhone,
        order.serviceName,
        order.description,
      ]
        .map((value) => normalizeSearchValue(value))
        .some((value) => value.includes(normalizedQuery));
    });
  }, [adminOrders.orders, orderFilter, orderSearchQuery]);

  return (
    <div className="pt-[72px] min-h-screen bg-[#F5F0E8] flex">
      <aside
        className={`fixed lg:sticky lg:top-[72px] inset-y-0 left-0 z-40 w-56 bg-foreground text-primary-foreground flex flex-col transform transition-transform duration-300 lg:transform-none lg:h-[calc(100vh-72px)] ${sidebarOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"}`}
      >
        <div className="p-5 border-b border-primary-foreground/10">
          <div className="text-[11px] font-serif tracking-wide">
            Studio Admin
          </div>
          <div className="text-[9px] tracking-[0.3em] uppercase text-primary-foreground/35 mt-0.5 font-sans">
            {user?.email ?? "Administrator"}
          </div>
        </div>
        <nav className="p-3 flex-1 overflow-y-auto">
          {NAV_ITEMS.map((item) => {
            const attentionCounts = getNavAttentionCounts(
              item.id,
              orderAttentionCounts,
              contactAttentionCounts,
            );
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => {
                  setSection(item.id);
                  setSidebarOpen(false);
                }}
                className={`w-full flex items-center justify-between gap-3 px-3 py-2.5 text-[12px] font-sans transition-colors mb-0.5 ${section === item.id ? "bg-primary-foreground/12 text-primary-foreground" : "text-primary-foreground/55 hover:text-primary-foreground hover:bg-primary-foreground/6"}`}
              >
                <span className="inline-flex items-center gap-3 min-w-0">
                  <item.icon size={13} />
                  <span className="truncate">{item.label}</span>
                </span>
                <AttentionBadge counts={attentionCounts} />
              </button>
            );
          })}
        </nav>
        <div className="p-4 border-t border-primary-foreground/10">
          <button
            type="button"
            onClick={logout}
            className="w-full flex items-center gap-2 text-[11px] text-primary-foreground/50 hover:text-primary-foreground mb-3"
          >
            <LogOut size={12} /> Sign out
          </button>
          <button
            type="button"
            onClick={() => navigate("home")}
            className="w-full text-[11px] text-primary-foreground/35 hover:text-primary-foreground/65 text-left"
          >
            &larr; Back to Website
          </button>
        </div>
      </aside>

      <button
        type="button"
        className="lg:hidden fixed top-20 left-3 z-50 bg-foreground text-primary-foreground p-2.5"
        onClick={() => setSidebarOpen((value) => !value)}
        aria-label="Toggle sidebar"
      >
        <Menu size={14} />
      </button>

      <main className="flex-1 overflow-auto">
        <div className="w-full max-w-7xl mx-auto p-6 lg:p-10">
          <div className="mb-8">
            <h1 className="font-serif text-[1.6rem] font-light text-foreground">
              {NAV_ITEMS.find((item) => item.id === section)?.label}
            </h1>
            <div className="flex flex-wrap items-center gap-3 mt-1">
              <p className="text-[11px] text-muted-foreground font-sans">
                Backend-backed studio management
              </p>
              <AdminLiveUpdatesStatus
                status={adminRealtime.status}
                lastEventAt={adminRealtime.lastEventAt}
              />
            </div>
          </div>

          {section === "dashboard" ? (
            <AdminDashboardOverview
              orders={adminOrders.orders}
              contactMessages={contactMessages}
              orderAttentionCounts={orderAttentionCounts}
              contactAttentionCounts={contactAttentionCounts}
              emailDeliverySettings={emailDeliverySettings}
              emailDeliveryError={emailDeliveryError}
              siteSettings={siteSettings}
              siteSettingsError={siteSettingsError}
              isOrdersLoading={adminOrders.isLoading}
              ordersError={adminOrders.error}
              onOpenSection={(targetSection) => setSection(targetSection)}
              onSelectOrder={(id) => void adminOrders.selectOrder(id)}
            />
          ) : null}
          {section === "orders" ? (
            <div className="space-y-5">
              <AttentionSummaryCards
                items={[
                  {
                    label: "New orders",
                    value: orderAttentionCounts.newCount,
                    tone: "accent",
                  },
                  {
                    label: "Total orders",
                    value: orderAttentionCounts.totalCount,
                  },
                ]}
              />
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex flex-wrap items-center gap-3">
                  <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
                    Status
                    <select
                      value={orderFilter}
                      onChange={(event) =>
                        setOrderFilter(
                          event.target.value as AdminOrderStatus | "All",
                        )
                      }
                      className="ml-3 px-3 py-2 text-[10px] border border-border bg-background focus:outline-none focus:border-accent"
                    >
                      <option value="All">All statuses</option>
                      {ORDER_STATUSES.map((status) => (
                        <option key={status} value={status}>
                          {ADMIN_STATUS_LABELS[status]}
                        </option>
                      ))}
                    </select>
                  </label>
                  <div className="relative w-full sm:w-[320px]">
                    <Search
                      size={13}
                      className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground"
                      aria-hidden="true"
                    />
                    <input
                      type="text"
                      value={orderSearchQuery}
                      onChange={(event) =>
                        setOrderSearchQuery(event.target.value)
                      }
                      placeholder="Search reference, client, email, phone, service..."
                      className="w-full border border-border bg-background pl-8 pr-8 py-2 text-[10px] font-sans focus:outline-none focus:border-accent"
                      aria-label="Search orders"
                    />
                    {orderSearchQuery ? (
                      <button
                        type="button"
                        onClick={() => setOrderSearchQuery("")}
                        className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-muted-foreground hover:text-foreground"
                        aria-label="Clear order search"
                      >
                        <X size={12} />
                      </button>
                    ) : null}
                  </div>
                  <span className="text-[10px] text-muted-foreground font-sans">
                    {filteredOrders.length} visible /{" "}
                    {adminOrders.orders.length} total
                  </span>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  <button
                    type="button"
                    onClick={() => exportOrdersCsv(filteredOrders)}
                    disabled={filteredOrders.length === 0}
                    className="inline-flex items-center gap-2 px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50"
                  >
                    <Download size={12} aria-hidden="true" /> Export CSV
                  </button>
                  <button
                    type="button"
                    onClick={() => void adminOrders.reload()}
                    disabled={adminOrders.isLoading}
                    className="px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50"
                  >
                    Refresh
                  </button>
                </div>
              </div>
              <AdminOrdersTable
                orders={filteredOrders}
                isLoading={adminOrders.isLoading}
                emptyMessage="No enquiries match this status or search."
                onSelect={(id) => void adminOrders.selectOrder(id)}
              />
            </div>
          ) : null}
          {section === "contactMessages" ? (
            <AdminContactMessagesPanel
              onUnauthorized={logout}
              onCountsChange={setContactAttentionCounts}
              onMessagesChange={setContactMessages}
              realtimeRefreshKey={contactRefreshKey}
            />
          ) : null}
          {section === "services" ? (
            <AdminServicesPanel onUnauthorized={logout} />
          ) : null}
          {section === "portfolio" ? (
            <AdminPortfolioPanel onUnauthorized={logout} />
          ) : null}
          {section === "content" ? (
            <AdminContentPanel onUnauthorized={logout} />
          ) : null}
          {section === "repeatable" ? (
            <AdminRepeatableContentPanel onUnauthorized={logout} />
          ) : null}
          {section === "brand" ? (
            <AdminBrandSettingsPanel onUnauthorized={logout} />
          ) : null}
          {section === "settings" ? (
            <AdminSettingsPanel onUnauthorized={logout} />
          ) : null}
        </div>
      </main>

      {adminOrders.error ? (
        <div
          role="alert"
          className="fixed bottom-5 right-5 z-[80] max-w-sm bg-card border border-destructive/30 px-4 py-3 text-[11px] text-destructive shadow-lg"
        >
          {adminOrders.error}
        </div>
      ) : null}
      <AdminOrderDetail
        order={adminOrders.selectedOrder}
        isLoading={adminOrders.isDetailLoading}
        isSaving={adminOrders.isSaving}
        onClose={adminOrders.clearSelection}
        onStatusChange={adminOrders.changeStatus}
        onAddNote={adminOrders.addNote}
      />
    </div>
  );
}

function AdminLiveUpdatesStatus({
  status,
  lastEventAt,
}: {
  status: "connecting" | "connected" | "disconnected";
  lastEventAt: string | null;
}) {
  const statusLabel: Record<typeof status, string> = {
    connecting: "Live updates connecting",
    connected: "Live updates connected",
    disconnected: "Live updates disconnected",
  };
  const toneClass: Record<typeof status, string> = {
    connecting: "border-amber-200 bg-amber-50 text-amber-700",
    connected: "border-emerald-200 bg-emerald-50 text-emerald-700",
    disconnected: "border-slate-200 bg-slate-50 text-slate-600",
  };
  const eventHint = lastEventAt
    ? ` · last update ${formatAdminDate(lastEventAt)}`
    : "";

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-[9px] tracking-wide font-sans ${toneClass[status]}`}
      title={`${statusLabel[status]}${eventHint}`}
    >
      {statusLabel[status]}
    </span>
  );
}

function AdminDashboardOverview({
  orders,
  contactMessages,
  orderAttentionCounts,
  contactAttentionCounts,
  emailDeliverySettings,
  emailDeliveryError,
  siteSettings,
  siteSettingsError,
  isOrdersLoading,
  ordersError,
  onOpenSection,
  onSelectOrder,
}: {
  orders: readonly AdminOrderListItem[];
  contactMessages: readonly AdminContactMessageListItem[];
  orderAttentionCounts: AttentionCounts;
  contactAttentionCounts: AttentionCounts | null;
  emailDeliverySettings: AdminEmailDeliverySettings | null;
  emailDeliveryError: string | null;
  siteSettings: AdminSiteSettings | null;
  siteSettingsError: string | null;
  isOrdersLoading: boolean;
  ordersError: string | null;
  onOpenSection(section: AdminSection): void;
  onSelectOrder(id: string): void;
}) {
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
        emailDeliverySettings,
        emailDeliveryError,
        isOrdersLoading,
        ordersError,
      ),
    [
      siteSettings,
      siteSettingsError,
      emailDeliverySettings,
      emailDeliveryError,
      isOrdersLoading,
      ordersError,
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
          label="Email delivery"
          title={getEmailDeliveryTitle(emailDeliverySettings, emailDeliveryError)}
          caption={getEmailDeliveryCaption(emailDeliverySettings, emailDeliveryError)}
          onClick={() => onOpenSection("settings")}
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
  icon: typeof Package;
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
  icon: typeof Package;
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
          <p className="mt-2 text-[13px] text-foreground font-sans truncate">
            {title}
          </p>
          <p className="text-[10px] text-muted-foreground font-sans mt-1 leading-4">
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
        new Date(second.createdAt).getTime() - new Date(first.createdAt).getTime(),
    )
    .slice(0, 5);
}

function getRecentContactMessages(
  messages: readonly AdminContactMessageListItem[],
): AdminContactMessageListItem[] {
  return [...messages]
    .sort(
      (first, second) =>
        new Date(second.createdAt).getTime() - new Date(first.createdAt).getTime(),
    )
    .slice(0, 5);
}

function buildProductionReadinessItems(
  siteSettings: AdminSiteSettings | null,
  siteSettingsError: string | null,
  emailDeliverySettings: AdminEmailDeliverySettings | null,
  emailDeliveryError: string | null,
  isOrdersLoading: boolean,
  ordersError: string | null,
): ProductionReadinessItem[] {
  const settingsUnavailable = Boolean(siteSettingsError);
  const emailUnavailable = Boolean(emailDeliveryError);
  const emailConfigured = Boolean(siteSettings?.email?.trim());
  const phoneConfigured = Boolean(siteSettings?.phone?.trim());
  const ownerNotificationsReady = Boolean(
    siteSettings?.emailNotificationsEnabled && emailConfigured,
  );
  const customerConfirmationsReady = Boolean(
    siteSettings?.customerConfirmationEmailsEnabled,
  );
  const gmailReady = Boolean(
    emailDeliverySettings?.provider === "GmailSmtp" &&
      emailDeliverySettings.gmailAddress?.trim() &&
      emailDeliverySettings.appPasswordConfigured,
  );
  const configurationReady = emailDeliverySettings?.provider === "Configuration";

  return [
    {
      label: "Public contact details",
      status: !settingsUnavailable && emailConfigured && phoneConfigured ? "ready" : "warning",
      detail: settingsUnavailable
        ? siteSettingsError ?? "Site settings could not be loaded."
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
      status: !emailUnavailable && (gmailReady || configurationReady) ? "ready" : "warning",
      detail: emailUnavailable
        ? emailDeliveryError ?? "Email delivery status could not be loaded."
        : gmailReady
          ? "Gmail SMTP is configured with an App Password."
          : configurationReady
            ? "Delivery is controlled by server configuration or secrets."
            : "Complete Gmail SMTP settings and send a test email.",
    },
    {
      label: "Upload security",
      status: "review",
      detail:
        "Quarantine validation is implemented. Confirm ClamAV is configured on the production server.",
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
      status: "review",
      detail:
        "Before production, verify SPF, DKIM and DMARC for the sender domain or Gmail account.",
    },
  ];
}

function getEmailDeliveryTitle(
  settings: AdminEmailDeliverySettings | null,
  error: string | null,
): string {
  if (error) {
    return "Status unavailable";
  }

  if (!settings) {
    return "Loading status...";
  }

  return settings.provider === "GmailSmtp"
    ? "Gmail SMTP"
    : "Configuration";
}

function getEmailDeliveryCaption(
  settings: AdminEmailDeliverySettings | null,
  error: string | null,
): string {
  if (error) {
    return error;
  }

  if (!settings) {
    return "Checking configured delivery mode.";
  }

  if (settings.provider === "GmailSmtp") {
    return settings.appPasswordConfigured
      ? `${settings.gmailAddress ?? "Gmail address"} is configured.`
      : "Gmail SMTP is selected, but the App Password is missing.";
  }

  return "Email delivery is managed by server configuration or user-secrets.";
}


function exportOrdersCsv(orders: readonly AdminOrderListItem[]): void {
  downloadCsv(createCsvFileName("bespoke-orders"), orders, [
    { header: "Reference", value: (order) => order.referenceNumber },
    { header: "Client", value: (order) => order.clientName },
    { header: "Email", value: (order) => order.clientEmail },
    { header: "Phone", value: (order) => order.clientPhone },
    { header: "Service", value: (order) => order.serviceName },
    { header: "Status", value: (order) => ADMIN_STATUS_LABELS[order.status] },
    { header: "Preferred date", value: (order) => order.preferredDate },
    { header: "Created at", value: (order) => order.createdAt },
    { header: "Message", value: (order) => order.description },
  ]);
}

function normalizeSearchValue(value: string | null | undefined): string {
  return (value ?? "").trim().toLocaleLowerCase();
}

function calculateOrderAttentionCounts(
  orders: readonly AdminOrderListItem[],
): AttentionCounts {
  return {
    newCount: orders.filter((order) => order.status === "New").length,
    totalCount: orders.length,
  };
}

function calculateContactAttentionCounts(
  messages: readonly AdminContactMessageListItem[],
): AttentionCounts {
  return {
    newCount: messages.filter((message) => message.status === "New").length,
    totalCount: messages.length,
  };
}

function getNavAttentionCounts(
  section: AdminSection,
  orderAttentionCounts: AttentionCounts,
  contactAttentionCounts: AttentionCounts | null,
): AttentionCounts | null {
  if (section === "orders") {
    return orderAttentionCounts;
  }

  if (section === "contactMessages") {
    return contactAttentionCounts;
  }

  return null;
}

function AttentionBadge({ counts }: { counts: AttentionCounts | null }) {
  if (!counts || counts.newCount <= 0) {
    return null;
  }

  return (
    <span className="shrink-0 rounded-full bg-rose-100 px-2 py-0.5 text-[9px] font-sans text-rose-700">
      {counts.newCount} new
    </span>
  );
}

function AttentionSummaryCards({
  items,
}: {
  items: ReadonlyArray<{ label: string; value: number; tone?: "accent" }>;
}) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
      {items.map((item) => (
        <div
          key={item.label}
          className="bg-card border border-border px-5 py-4"
        >
          <div className="text-[10px] uppercase tracking-wider text-muted-foreground font-sans">
            {item.label}
          </div>
          <div
            className={`mt-1 text-[1.45rem] font-serif font-light ${item.tone === "accent" ? "text-rose-700" : "text-foreground"}`}
          >
            {item.value}
          </div>
        </div>
      ))}
    </div>
  );
}
