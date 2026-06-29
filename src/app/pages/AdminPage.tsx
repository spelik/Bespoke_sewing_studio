import { useEffect, useMemo, useState } from "react";
import {
  FileText,
  Images,
  ListOrdered,
  LogOut,
  Mail,
  Menu,
  Package,
  Palette,
  Scissors,
  Search,
  Settings,
  X,
} from "lucide-react";
import { ApiError } from "../../api/apiClient";
import { getAdminContactMessages } from "../../api/contactMessagesApi";
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
import { ADMIN_STATUS_LABELS } from "../components/adminOrderFormatting";
import { useAdminOrders } from "../hooks/useAdminOrders";
import { usePageNavigation } from "../routing/usePageNavigation";
import type { AdminContactMessageListItem } from "../types";

type AdminSection =
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

const NAV_ITEMS: ReadonlyArray<{
  id: AdminSection;
  label: string;
  icon: typeof Package;
}> = [
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
  const [section, setSection] = useState<AdminSection>("orders");
  const [orderFilter, setOrderFilter] = useState<AdminOrderStatus | "All">(
    "All",
  );
  const [orderSearchQuery, setOrderSearchQuery] = useState("");
  const [contactAttentionCounts, setContactAttentionCounts] =
    useState<AttentionCounts | null>(null);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const orderAttentionCounts = useMemo(
    () => calculateOrderAttentionCounts(adminOrders.orders),
    [adminOrders.orders],
  );

  useEffect(() => {
    let cancelled = false;

    async function loadContactAttentionCounts() {
      try {
        const messages = await getAdminContactMessages();
        if (!cancelled) {
          setContactAttentionCounts(calculateContactAttentionCounts(messages));
        }
      } catch (reason: unknown) {
        if (
          reason instanceof ApiError &&
          (reason.status === 401 || reason.status === 403)
        ) {
          logout();
        }
      }
    }

    void loadContactAttentionCounts();

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
            <p className="text-[11px] text-muted-foreground mt-1 font-sans">
              Backend-backed studio management
            </p>
          </div>

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
                <button
                  type="button"
                  onClick={() => void adminOrders.reload()}
                  disabled={adminOrders.isLoading}
                  className="px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50"
                >
                  Refresh
                </button>
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
