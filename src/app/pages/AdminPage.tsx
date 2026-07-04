import { useCallback, useEffect, useMemo, useState } from "react";
import { Download, LogOut, Menu } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import { getAdminContactMessages } from "../../api/contactMessagesApi";
import {
  getAdminEmailDeliverySettings,
  getAdminSiteSettings,
} from "../../api/siteSettingsApi";
import {
  getAdminApiErrorMessage,
  getAdminOrders,
  ORDER_STATUSES,
  type AdminOrderListQuery,
  type AdminOrderListItem,
  type AdminOrderStatus,
} from "../../api/ordersApi";
import { type AdminPageSize } from "../../api/pagination";
import {
  ADMIN_NAV_ITEMS,
  getAdminSectionFromHash,
  updateAdminSectionHash,
  type AdminSection,
} from "../admin/adminSections";
import { useAuth } from "../auth/AuthContext";
import { AdminAccountPanel } from "../components/AdminAccountPanel";
import {
  AttentionBadge,
  AttentionSummaryCards,
  getNavAttentionCounts,
  type AttentionCounts,
} from "../components/AdminAttention";
import { AdminAuditLogPanel } from "../components/AdminAuditLogPanel";
import { AdminDashboardOverview } from "../components/AdminDashboardOverview";
import { AdminEmailLogPanel } from "../components/AdminEmailLogPanel";
import { AdminLiveUpdatesStatus } from "../components/AdminLiveUpdatesStatus";
import {
  AdminActionButton,
  AdminConfirmDialog,
  AdminFilterDropdown,
  AdminServerPagination,
  AdminSearchInput,
  type AdminFilterOption,
} from "../components/AdminUi";
import { AdminBrandSettingsPanel } from "../components/AdminBrandSettingsPanel";
import { AdminContactMessagesPanel } from "../components/AdminContactMessagesPanel";
import { AdminContentPanel } from "../components/AdminContentPanel";
import { AdminOrderDetail } from "../components/AdminOrderDetail";
import { AdminOrdersTable } from "../components/AdminOrdersTable";
import { AdminPortfolioPanel } from "../components/AdminPortfolioPanel";
import { AdminRepeatableContentPanel } from "../components/AdminRepeatableContentPanel";
import { AdminServicesPanel } from "../components/AdminServicesPanel";
import { AdminSettingsPanel } from "../components/AdminSettingsPanel";
import { AdminStoragePanel } from "../components/AdminStoragePanel";
import { AdminUsersPanel } from "../components/AdminUsersPanel";
import { ADMIN_STATUS_LABELS } from "../components/adminOrderFormatting";
import { useAdminOrders } from "../hooks/useAdminOrders";
import { useAdminRealtimeUpdates } from "../hooks/useAdminRealtimeUpdates";
import { usePageNavigation } from "../routing/usePageNavigation";
import { exportOrdersCsv } from "../utils/adminOrderCsvExport";
import type {
  AdminContactMessageListItem,
  AdminEmailDeliverySettings,
  AdminSiteSettings,
} from "../types";

const ORDER_STATUS_FILTER_OPTIONS: AdminFilterOption[] = [
  { value: "All", label: "All statuses" },
  ...ORDER_STATUSES.map((status) => ({
    value: status,
    label: ADMIN_STATUS_LABELS[status],
  })),
];

export function AdminPage() {
  const navigate = usePageNavigation();
  const { user, logout } = useAuth();
  const [section, setSection] = useState<AdminSection>(() =>
    getAdminSectionFromHash(),
  );
  const [orderFilter, setOrderFilter] = useState<AdminOrderStatus | "All">(
    "All",
  );
  const [orderSearchQuery, setOrderSearchQuery] = useState("");
  const [debouncedOrderSearchQuery, setDebouncedOrderSearchQuery] = useState("");
  const [orderStatusFilterOpen, setOrderStatusFilterOpen] = useState(false);
  const [orderPage, setOrderPage] = useState(1);
  const [orderPageSize, setOrderPageSize] = useState<AdminPageSize>(25);
  const [orderDeleteCandidate, setOrderDeleteCandidate] = useState<AdminOrderListItem | null>(null);
  const [dashboardOrders, setDashboardOrders] = useState<AdminOrderListItem[]>([]);
  const [orderAttentionCounts, setOrderAttentionCounts] = useState<AttentionCounts>({
    newCount: 0,
    totalCount: 0,
  });
  const [isDashboardOrdersLoading, setIsDashboardOrdersLoading] = useState(true);
  const [dashboardOrdersError, setDashboardOrdersError] = useState<string | null>(null);
  const [contactMessages, setContactMessages] = useState<
    AdminContactMessageListItem[]
  >([]);
  const [contactRefreshKey, setContactRefreshKey] = useState(0);
  const [emailLogRefreshKey, setEmailLogRefreshKey] = useState(0);
  const [contactAttentionCounts, setContactAttentionCounts] =
    useState<AttentionCounts | null>(null);
  const [emailDeliverySettings, setEmailDeliverySettings] =
    useState<AdminEmailDeliverySettings | null>(null);
  const [emailDeliveryError, setEmailDeliveryError] = useState<string | null>(
    null,
  );
  const [siteSettings, setSiteSettings] = useState<AdminSiteSettings | null>(
    null,
  );
  const [siteSettingsError, setSiteSettingsError] = useState<string | null>(
    null,
  );
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const orderQuery = useMemo<AdminOrderListQuery>(
    () => ({
      page: orderPage,
      pageSize: orderPageSize,
      search: debouncedOrderSearchQuery.trim() || undefined,
      status: orderFilter === "All" ? undefined : orderFilter,
    }),
    [debouncedOrderSearchQuery, orderFilter, orderPage, orderPageSize],
  );
  const adminOrders = useAdminOrders(logout, orderQuery);

  const loadDashboardOrders = useCallback(async () => {
    setIsDashboardOrdersLoading(true);
    setDashboardOrdersError(null);

    try {
      const [allOrders, newOrders] = await Promise.all([
        getAdminOrders({ page: 1, pageSize: 10 }),
        getAdminOrders({ page: 1, pageSize: 10, status: "New" }),
      ]);
      setDashboardOrders(allOrders.items);
      setOrderAttentionCounts({
        newCount: newOrders.totalItems,
        totalCount: allOrders.totalItems,
      });
    } catch (reason: unknown) {
      if (
        reason instanceof ApiError &&
        (reason.status === 401 || reason.status === 403)
      ) {
        logout();
        return;
      }

      setDashboardOrdersError(getAdminApiErrorMessage(reason));
    } finally {
      setIsDashboardOrdersLoading(false);
    }
  }, [logout]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedOrderSearchQuery(orderSearchQuery);
    }, orderSearchQuery.trim() ? 300 : 0);

    return () => window.clearTimeout(timeoutId);
  }, [orderSearchQuery]);

  useEffect(() => {
    void loadDashboardOrders();
  }, [loadDashboardOrders]);

  const loadContactMessagesForDashboard = useCallback(async () => {
    try {
      const [allMessages, newMessages] = await Promise.all([
        getAdminContactMessages({ page: 1, pageSize: 10 }),
        getAdminContactMessages({ page: 1, pageSize: 10, status: "New" }),
      ]);
      setContactMessages(allMessages.items);
      setContactAttentionCounts({
        newCount: newMessages.totalItems,
        totalCount: allMessages.totalItems,
      });
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

  useEffect(() => {
    const syncSectionFromUrl = () => {
      setSection(getAdminSectionFromHash());
    };

    window.addEventListener("hashchange", syncSectionFromUrl);
    window.addEventListener("popstate", syncSectionFromUrl);

    return () => {
      window.removeEventListener("hashchange", syncSectionFromUrl);
      window.removeEventListener("popstate", syncSectionFromUrl);
    };
  }, []);

  const handleAdminSectionChange = useCallback(
    (targetSection: AdminSection) => {
      setSection(targetSection);
      updateAdminSectionHash(targetSection);
      setSidebarOpen(false);
    },
    [],
  );

  const handleAdminRealtimeEvent = useCallback(
    (event: { entity: "Order" | "ContactMessage" | "EmailDeliveryLog" }) => {
      if (event.entity === "Order") {
        void adminOrders.reload();
        void loadDashboardOrders();
        return;
      }

      if (event.entity === "ContactMessage") {
        void loadContactMessagesForDashboard();
        setContactRefreshKey((current) => current + 1);
        return;
      }

      setEmailLogRefreshKey((current) => current + 1);
    },
    [adminOrders.reload, loadContactMessagesForDashboard, loadDashboardOrders],
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

  useEffect(() => {
    if (orderPage > adminOrders.totalPages) {
      setOrderPage(adminOrders.totalPages);
    }
  }, [adminOrders.totalPages, orderPage]);

  async function confirmOrderDelete() {
    if (!orderDeleteCandidate) {
      return;
    }

    const wasDeleted = await adminOrders.deleteOrder(orderDeleteCandidate.id);
    if (wasDeleted) {
      setOrderDeleteCandidate(null);
      void loadDashboardOrders();
    }
  }

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
          {ADMIN_NAV_ITEMS.map((item) => {
            const attentionCounts = getNavAttentionCounts(
              item.id,
              orderAttentionCounts,
              contactAttentionCounts,
            );
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => handleAdminSectionChange(item.id)}
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
              {ADMIN_NAV_ITEMS.find((item) => item.id === section)?.label}
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
              orders={dashboardOrders}
              contactMessages={contactMessages}
              orderAttentionCounts={orderAttentionCounts}
              contactAttentionCounts={contactAttentionCounts}
              emailDeliverySettings={emailDeliverySettings}
              emailDeliveryError={emailDeliveryError}
              siteSettings={siteSettings}
              siteSettingsError={siteSettingsError}
              isOrdersLoading={isDashboardOrdersLoading}
              ordersError={dashboardOrdersError}
              onOpenSection={handleAdminSectionChange}
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
              <div className="bg-card border border-border p-5 space-y-3">
                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-12 gap-3 items-end">
                  <AdminFilterDropdown
                    id="orders-status-filter"
                    label="Status"
                    value={orderFilter}
                    placeholder="All statuses"
                    options={ORDER_STATUS_FILTER_OPTIONS}
                    allowEmpty={false}
                    isOpen={orderStatusFilterOpen}
                    onToggle={() =>
                      setOrderStatusFilterOpen((current) => !current)
                    }
                    onClose={() => setOrderStatusFilterOpen(false)}
                    onChange={(value) => {
                      setOrderPage(1);
                      setOrderFilter(value as AdminOrderStatus | "All");
                    }}
                    className="xl:col-span-2"
                  />
                  <AdminSearchInput
                    label="Search"
                    value={orderSearchQuery}
                    onChange={(value) => {
                      setOrderPage(1);
                      setOrderSearchQuery(value);
                    }}
                    placeholder="Reference, client, email, phone, service..."
                    ariaLabel="Search orders"
                    className="xl:col-span-5"
                  />
                  <div className="xl:col-span-2 text-[10px] text-muted-foreground font-sans">
                    {adminOrders.orders.length} visible / {adminOrders.totalItems} total
                  </div>
                  <div className="xl:col-span-3 flex flex-wrap items-center justify-start xl:justify-end gap-2">
                    <AdminActionButton
                      icon={<Download size={12} aria-hidden="true" />}
                      onClick={() => exportOrdersCsv(adminOrders.orders)}
                      disabled={adminOrders.orders.length === 0}
                    >
                      Export CSV
                    </AdminActionButton>
                  </div>
                </div>
              </div>
              <AdminOrdersTable
                orders={adminOrders.orders}
                isLoading={adminOrders.isLoading}
                emptyMessage="No enquiries match this status or search."
                deletingOrderId={adminOrders.deletingOrderId}
                onSelect={(id) => void adminOrders.selectOrder(id)}
                onRequestDelete={setOrderDeleteCandidate}
              />
              <AdminServerPagination
                page={adminOrders.page}
                pageSize={orderPageSize}
                totalItems={adminOrders.totalItems}
                totalPages={adminOrders.totalPages}
                isLoading={adminOrders.isLoading}
                onPageChange={setOrderPage}
                onPageSizeChange={(value) => {
                  setOrderPage(1);
                  setOrderPageSize(value);
                }}
              />
            </div>
          ) : null}
          {section === "contactMessages" ? (
            <AdminContactMessagesPanel
              onUnauthorized={logout}
              attentionCounts={contactAttentionCounts}
              onDataChanged={() => void loadContactMessagesForDashboard()}
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
          {section === "users" ? (
            <AdminUsersPanel onUnauthorized={logout} />
          ) : null}
          {section === "account" ? (
            <AdminAccountPanel
              email={user?.email ?? "Administrator"}
              roles={user?.roles ?? []}
              onLogout={logout}
              onUnauthorized={logout}
            />
          ) : null}
          {section === "auditLog" ? (
            <AdminAuditLogPanel onUnauthorized={logout} />
          ) : null}
          {section === "emailLog" ? (
            <AdminEmailLogPanel
              onUnauthorized={logout}
              realtimeRefreshKey={emailLogRefreshKey}
            />
          ) : null}
          {section === "storage" ? (
            <AdminStoragePanel onUnauthorized={logout} />
          ) : null}
          {section === "settings" ? (
            <AdminSettingsPanel onUnauthorized={logout} />
          ) : null}
        </div>
      </main>

      {orderDeleteCandidate ? (
        <AdminConfirmDialog
          title="Delete order?"
          description={
            <>
              This will permanently remove order
              <span className="font-medium text-foreground"> {orderDeleteCandidate.referenceNumber}</span>
              {' '}from
              <span className="font-medium text-foreground"> {orderDeleteCandidate.clientName}</span>.
              Any linked attachment files and internal notes will also be removed. This action cannot be undone.
            </>
          }
          confirmLabel="Delete order"
          isBusy={adminOrders.deletingOrderId === orderDeleteCandidate.id}
          onCancel={() => setOrderDeleteCandidate(null)}
          onConfirm={() => void confirmOrderDelete()}
        />
      ) : null}
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
        deletingAttachmentId={adminOrders.deletingAttachmentId}
        onClose={adminOrders.clearSelection}
        onStatusChange={adminOrders.changeStatus}
        onAddNote={adminOrders.addNote}
        onDeleteAttachment={adminOrders.deleteAttachment}
      />
    </div>
  );
}
