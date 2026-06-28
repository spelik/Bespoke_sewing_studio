import { useState } from "react";
import { FileText, Images, LogOut, Menu, Package, Palette, Scissors, Settings } from "lucide-react";
import { ORDER_STATUSES, type AdminOrderStatus } from "../../api/ordersApi";
import { useAuth } from "../auth/AuthContext";
import { AdminBrandSettingsPanel } from "../components/AdminBrandSettingsPanel";
import { AdminContentPanel } from "../components/AdminContentPanel";
import { AdminOrderDetail } from "../components/AdminOrderDetail";
import { AdminOrdersTable } from "../components/AdminOrdersTable";
import { AdminPortfolioPanel } from "../components/AdminPortfolioPanel";
import { AdminServicesPanel } from "../components/AdminServicesPanel";
import { AdminSettingsPanel } from "../components/AdminSettingsPanel";
import { ADMIN_STATUS_LABELS } from "../components/adminOrderFormatting";
import { useAdminOrders } from "../hooks/useAdminOrders";
import { usePageNavigation } from "../routing/usePageNavigation";

type AdminSection = "orders" | "services" | "portfolio" | "content" | "brand" | "settings";

const NAV_ITEMS: ReadonlyArray<{ id: AdminSection; label: string; icon: typeof Package }> = [
  { id: "orders", label: "Orders", icon: Package },
  { id: "services", label: "Services", icon: Scissors },
  { id: "portfolio", label: "Portfolio", icon: Images },
  { id: "content", label: "Content", icon: FileText },
  { id: "brand", label: "Brand / SEO", icon: Palette },
  { id: "settings", label: "Settings", icon: Settings },
];

export function AdminPage() {
  const navigate = usePageNavigation();
  const { user, logout } = useAuth();
  const adminOrders = useAdminOrders(logout);
  const [section, setSection] = useState<AdminSection>("orders");
  const [orderFilter, setOrderFilter] = useState<AdminOrderStatus | "All">("All");
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const filteredOrders = orderFilter === "All" ? adminOrders.orders : adminOrders.orders.filter((order) => order.status === orderFilter);

  return (
    <div className="pt-[72px] min-h-screen bg-[#F5F0E8] flex">
      <aside className={`fixed lg:sticky lg:top-[72px] inset-y-0 left-0 z-40 w-56 bg-foreground text-primary-foreground flex flex-col transform transition-transform duration-300 lg:transform-none lg:h-[calc(100vh-72px)] ${sidebarOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"}`}>
        <div className="p-5 border-b border-primary-foreground/10">
          <div className="text-[11px] font-serif tracking-wide">Studio Admin</div>
          <div className="text-[9px] tracking-[0.3em] uppercase text-primary-foreground/35 mt-0.5 font-sans">{user?.email ?? "Administrator"}</div>
        </div>
        <nav className="p-3 flex-1 overflow-y-auto">
          {NAV_ITEMS.map((item) => (
            <button key={item.id} type="button" onClick={() => { setSection(item.id); setSidebarOpen(false); }} className={`w-full flex items-center gap-3 px-3 py-2.5 text-[12px] font-sans transition-colors mb-0.5 ${section === item.id ? "bg-primary-foreground/12 text-primary-foreground" : "text-primary-foreground/55 hover:text-primary-foreground hover:bg-primary-foreground/6"}`}>
              <item.icon size={13} />{item.label}
            </button>
          ))}
        </nav>
        <div className="p-4 border-t border-primary-foreground/10">
          <button type="button" onClick={logout} className="w-full flex items-center gap-2 text-[11px] text-primary-foreground/50 hover:text-primary-foreground mb-3"><LogOut size={12} /> Sign out</button>
          <button type="button" onClick={() => navigate("home")} className="w-full text-[11px] text-primary-foreground/35 hover:text-primary-foreground/65 text-left">&larr; Back to Website</button>
        </div>
      </aside>

      <button type="button" className="lg:hidden fixed top-20 left-3 z-50 bg-foreground text-primary-foreground p-2.5" onClick={() => setSidebarOpen((value) => !value)} aria-label="Toggle sidebar"><Menu size={14} /></button>

      <main className="flex-1 overflow-auto">
        <div className="p-6 lg:p-10 max-w-5xl">
          <div className="mb-8">
            <h1 className="font-serif text-[1.6rem] font-light text-foreground">{NAV_ITEMS.find((item) => item.id === section)?.label}</h1>
            <p className="text-[11px] text-muted-foreground mt-1 font-sans">Backend-backed studio management</p>
          </div>

          {section === "orders" ? <div className="space-y-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <label className="text-[10px] tracking-wide text-muted-foreground font-sans">Status
                <select value={orderFilter} onChange={(event) => setOrderFilter(event.target.value as AdminOrderStatus | "All")} className="ml-3 px-3 py-2 text-[10px] border border-border bg-background focus:outline-none focus:border-accent">
                  <option value="All">All statuses</option>
                  {ORDER_STATUSES.map((status) => <option key={status} value={status}>{ADMIN_STATUS_LABELS[status]}</option>)}
                </select>
              </label>
              <button type="button" onClick={() => void adminOrders.reload()} disabled={adminOrders.isLoading} className="px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50">Refresh</button>
            </div>
            <AdminOrdersTable orders={filteredOrders} isLoading={adminOrders.isLoading} emptyMessage="No enquiries match this status." onSelect={(id) => void adminOrders.selectOrder(id)} />
          </div> : null}
          {section === "services" ? <AdminServicesPanel onUnauthorized={logout} /> : null}
          {section === "portfolio" ? <AdminPortfolioPanel onUnauthorized={logout} /> : null}
          {section === "content" ? <AdminContentPanel onUnauthorized={logout} /> : null}
          {section === "brand" ? <AdminBrandSettingsPanel onUnauthorized={logout} /> : null}
          {section === "settings" ? <AdminSettingsPanel onUnauthorized={logout} /> : null}
        </div>
      </main>

      {adminOrders.error ? <div role="alert" className="fixed bottom-5 right-5 z-[80] max-w-sm bg-card border border-destructive/30 px-4 py-3 text-[11px] text-destructive shadow-lg">{adminOrders.error}</div> : null}
      <AdminOrderDetail order={adminOrders.selectedOrder} isLoading={adminOrders.isDetailLoading} isSaving={adminOrders.isSaving} onClose={adminOrders.clearSelection} onStatusChange={adminOrders.changeStatus} onAddNote={adminOrders.addNote} />
    </div>
  );
}
