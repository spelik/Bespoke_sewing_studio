import { useState } from "react";
import { BarChart2, Bell, Eye, LogOut, Mail, Menu, Package, Search, Send, TrendingUp, Users } from "lucide-react";
import { Bar, BarChart, CartesianGrid, Cell, Line, LineChart, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { ORDER_STATUSES, type AdminOrderStatus } from "../../api/ordersApi";
import { ADMIN_STATS, MONTHLY_DATA, SERVICE_BREAKDOWN } from "../appContent";
import { useAuth } from "../auth/AuthContext";
import { AdminOrderDetail } from "../components/AdminOrderDetail";
import { AdminOrdersTable } from "../components/AdminOrdersTable";
import { ADMIN_STATUS_LABELS } from "../components/adminOrderFormatting";
import { useAdminOrders } from "../hooks/useAdminOrders";
import { usePageNavigation } from "../routing/usePageNavigation";

type AdminSection = "overview" | "orders" | "clients" | "campaigns" | "analytics";

export function AdminPage() {
  const navigate = usePageNavigation();
  const { user, logout } = useAuth();
  const adminOrders = useAdminOrders(logout);
  const [section, setSection] = useState<AdminSection>("overview");
  const [orderFilter, setOrderFilter] = useState<AdminOrderStatus | "All">("All");
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const monthlyData = [...MONTHLY_DATA];
  const serviceBreakdown = [...SERVICE_BREAKDOWN];
  const filteredOrders =
    orderFilter === "All"
      ? adminOrders.orders
      : adminOrders.orders.filter((order) => order.status === orderFilter);

  const navItems: { id: AdminSection; label: string; icon: typeof BarChart2 }[] = [
    { id: "overview", label: "Overview", icon: BarChart2 },
    { id: "orders", label: "Orders", icon: Package },
    { id: "clients", label: "Clients", icon: Users },
    { id: "campaigns", label: "Campaigns", icon: Mail },
    { id: "analytics", label: "Analytics", icon: TrendingUp },
  ];

  return (
    <div className="pt-[72px] min-h-screen bg-[#F5F0E8] flex">
      {/* Sidebar */}
      <aside
        className={`fixed lg:sticky lg:top-[72px] inset-y-0 left-0 z-40 w-56 bg-foreground text-primary-foreground flex flex-col transform transition-transform duration-300 lg:transform-none lg:h-[calc(100vh-72px)] ${
          sidebarOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"
        }`}
      >
        <div className="p-5 border-b border-primary-foreground/10">
          <div className="text-[11px] font-serif tracking-wide text-primary-foreground">Studio Admin</div>
          <div className="text-[9px] tracking-[0.3em] uppercase text-primary-foreground/35 mt-0.5 font-sans">
            {user?.email ?? "Administrator"}
          </div>
        </div>
        <nav className="p-3 flex-1 overflow-y-auto">
          {navItems.map((item) => (
            <button
              key={item.id}
              onClick={() => {
                setSection(item.id);
                setSidebarOpen(false);
              }}
              className={`w-full flex items-center gap-3 px-3 py-2.5 text-[12px] font-sans transition-colors mb-0.5 ${
                section === item.id
                  ? "bg-primary-foreground/12 text-primary-foreground"
                  : "text-primary-foreground/55 hover:text-primary-foreground hover:bg-primary-foreground/6"
              }`}
            >
              <item.icon size={13} />
              {item.label}
            </button>
          ))}
        </nav>
        <div className="p-4 border-t border-primary-foreground/10">
          <button
            type="button"
            onClick={logout}
            className="w-full flex items-center gap-2 text-[11px] text-primary-foreground/50 hover:text-primary-foreground transition-colors text-left font-sans mb-3"
          >
            <LogOut size={12} /> Sign out
          </button>
          <button
            onClick={() => navigate("home")}
            className="w-full text-[11px] text-primary-foreground/35 hover:text-primary-foreground/65 transition-colors text-left font-sans"
          >
            &larr; Back to Website
          </button>
        </div>
      </aside>

      {/* Mobile sidebar toggle */}
      <button
        className="lg:hidden fixed top-20 left-3 z-50 bg-foreground text-primary-foreground p-2.5"
        onClick={() => setSidebarOpen(!sidebarOpen)}
        aria-label="Toggle sidebar"
      >
        <Menu size={14} />
      </button>

      {/* Main content */}
      <main className="flex-1 overflow-auto">
        <div className="p-6 lg:p-10 max-w-5xl">
          {/* Header */}
          <div className="flex items-center justify-between mb-8">
            <div>
              <h1 className="font-serif text-[1.6rem] font-light text-foreground">
                {navItems.find((n) => n.id === section)?.label}
              </h1>
              <p className="text-[11px] text-muted-foreground mt-1 font-sans">
                Studio management &middot; Live enquiries
              </p>
            </div>
            <div className="flex items-center gap-2">
              <button className="p-2 border border-border bg-background text-muted-foreground hover:text-foreground transition-colors">
                <Search size={13} />
              </button>
              <button className="p-2 border border-border bg-background text-muted-foreground hover:text-foreground transition-colors relative">
                <Bell size={13} />
                <span className="absolute top-1 right-1 w-1.5 h-1.5 bg-accent rounded-full" />
              </button>
            </div>
          </div>

          {/* ── OVERVIEW ── */}
          {section === "overview" && (
            <div className="space-y-6">
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                {ADMIN_STATS.map((stat) => (
                  <div key={stat.label} className="bg-card border border-border p-5">
                    <div className="flex items-start justify-between mb-3">
                      <stat.icon size={16} className={stat.color} />
                      <span className="text-[10px] text-emerald-600 font-medium font-sans">{stat.change}</span>
                    </div>
                    <div className="font-serif text-[1.8rem] font-light text-foreground leading-none">{stat.value}</div>
                    <div className="text-[11px] text-muted-foreground mt-1.5 font-sans">{stat.label}</div>
                  </div>
                ))}
              </div>

              <div className="bg-card border border-border p-6">
                <h3 className="text-[12px] font-medium text-foreground mb-5 font-sans tracking-wide">
                  Monthly Orders & Revenue
                </h3>
                <ResponsiveContainer width="100%" height={200}>
                  <BarChart data={monthlyData} barGap={4}>
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(0,0,0,0.05)" />
                    <XAxis dataKey="month" tick={{ fontSize: 10, fontFamily: "DM Sans" }} />
                    <YAxis yAxisId="left" tick={{ fontSize: 10, fontFamily: "DM Sans" }} />
                    <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 10, fontFamily: "DM Sans" }} />
                    <Tooltip contentStyle={{ fontSize: 12, fontFamily: "DM Sans" }} />
                    <Bar yAxisId="left" dataKey="orders" fill="#B8946A" radius={[2, 2, 0, 0]} name="Orders" />
                    <Bar yAxisId="right" dataKey="revenue" fill="#1C1917" radius={[2, 2, 0, 0]} name="Revenue (£)" />
                  </BarChart>
                </ResponsiveContainer>
              </div>

              <div className="bg-card border border-border">
                <div className="px-6 py-4 border-b border-border flex items-center justify-between">
                  <h3 className="text-[12px] font-medium text-foreground font-sans tracking-wide">Recent Orders</h3>
                  <button
                    onClick={() => setSection("orders")}
                    className="text-[11px] text-accent hover:underline font-sans"
                  >
                    View all
                  </button>
                </div>
                <AdminOrdersTable
                  orders={adminOrders.orders.slice(0, 5)}
                  isLoading={adminOrders.isLoading}
                  onSelect={(id) => void adminOrders.selectOrder(id)}
                />
              </div>
            </div>
          )}

          {/* ── ORDERS ── */}
          {section === "orders" && (
            <div className="space-y-5">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
                  Status
                  <select
                    value={orderFilter}
                    onChange={(event) => setOrderFilter(event.target.value as AdminOrderStatus | "All")}
                    className="ml-3 px-3 py-2 text-[10px] border border-border bg-background focus:outline-none focus:border-accent"
                  >
                    <option value="All">All statuses</option>
                    {ORDER_STATUSES.map((status) => (
                      <option key={status} value={status}>{ADMIN_STATUS_LABELS[status]}</option>
                    ))}
                  </select>
                </label>
                <button
                  type="button"
                  onClick={() => void adminOrders.reload()}
                  disabled={adminOrders.isLoading}
                  className="px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50 font-sans transition-colors"
                >
                  Refresh
                </button>
              </div>
              <AdminOrdersTable
                orders={filteredOrders}
                isLoading={adminOrders.isLoading}
                emptyMessage="No enquiries match this status."
                onSelect={(id) => void adminOrders.selectOrder(id)}
              />
            </div>
          )}

          {/* ── CLIENTS ── */}
          {section === "clients" && (
            <div className="space-y-5">
              <div className="relative max-w-xs">
                <Search size={13} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Search clients..."
                  className="w-full pl-9 pr-4 py-2.5 text-[12px] border border-border bg-background focus:outline-none focus:border-accent transition-colors font-sans"
                />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {[
                  { name: "Catherine O'Neill", orders: 4, spent: "£620", last: "15 Jun", location: "Belfast" },
                  { name: "Margaret Doherty", orders: 2, spent: "£380", last: "14 Jun", location: "Derry" },
                  { name: "Siobhán McBride", orders: 3, spent: "£540", last: "14 Jun", location: "Antrim" },
                  { name: "Fionnuala Walsh", orders: 7, spent: "£1,240", last: "13 Jun", location: "Lisburn" },
                  { name: "Aoife Murphy", orders: 1, spent: "£25", last: "12 Jun", location: "Belfast" },
                  { name: "Roisin Gallagher", orders: 2, spent: "£620", last: "11 Jun", location: "Bangor" },
                ].map((client) => (
                  <div key={client.name} className="bg-card border border-border p-5 hover:border-accent/30 transition-colors">
                    <div className="flex items-center gap-3 mb-4">
                      <div className="w-8 h-8 bg-secondary border border-border flex items-center justify-center text-[11px] font-medium text-foreground font-sans">
                        {client.name.split(" ").map((n) => n[0]).join("")}
                      </div>
                      <div>
                        <div className="text-[12px] font-medium text-foreground font-sans">{client.name}</div>
                        <div className="text-[10px] text-muted-foreground font-sans">{client.location}</div>
                      </div>
                    </div>
                    <div className="grid grid-cols-3 gap-2 text-center border-t border-border pt-4">
                      <div>
                        <div className="font-serif text-[1rem] font-light text-foreground">{client.orders}</div>
                        <div className="text-[9px] text-muted-foreground font-sans uppercase tracking-wide">Orders</div>
                      </div>
                      <div>
                        <div className="font-serif text-[1rem] font-light text-foreground">{client.spent}</div>
                        <div className="text-[9px] text-muted-foreground font-sans uppercase tracking-wide">Total</div>
                      </div>
                      <div>
                        <div className="text-[11px] text-foreground font-sans">{client.last}</div>
                        <div className="text-[9px] text-muted-foreground font-sans uppercase tracking-wide">Last</div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* ── CAMPAIGNS ── */}
          {section === "campaigns" && (
            <div className="space-y-5">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {[
                  { label: "Emails Sent", value: "1,240", icon: Send, color: "text-blue-600" },
                  { label: "Average Open Rate", value: "38.4%", icon: Eye, color: "text-emerald-600" },
                  { label: "Average Click Rate", value: "12.1%", icon: TrendingUp, color: "text-amber-600" },
                ].map((s) => (
                  <div key={s.label} className="bg-card border border-border p-5">
                    <s.icon size={15} className={`${s.color} mb-3`} />
                    <div className="font-serif text-[1.8rem] font-light text-foreground">{s.value}</div>
                    <div className="text-[11px] text-muted-foreground mt-1 font-sans">{s.label}</div>
                  </div>
                ))}
              </div>

              <div className="bg-card border border-border">
                <div className="px-5 py-4 border-b border-border">
                  <h3 className="text-[12px] font-medium text-foreground font-sans tracking-wide">Recent Campaigns</h3>
                </div>
                {[
                  { name: "Summer Wedding Season Offer", sent: "420", open: "41%", date: "1 Jun 2024", status: "Sent" },
                  { name: "Vintage Restoration — New Service", sent: "380", open: "35%", date: "15 May 2024", status: "Sent" },
                  { name: "Spring Studio Newsletter", sent: "440", open: "39%", date: "1 Apr 2024", status: "Sent" },
                  { name: "Back to School Alterations Offer", sent: "0", open: "—", date: "Draft", status: "Draft" },
                ].map((c) => (
                  <div
                    key={c.name}
                    className="px-5 py-4 border-b border-border/40 flex items-center justify-between hover:bg-secondary/20 transition-colors"
                  >
                    <div>
                      <div className="text-[12px] text-foreground font-sans">{c.name}</div>
                      <div className="text-[10px] text-muted-foreground mt-0.5 font-sans">
                        {c.date} &middot; {c.sent} recipients &middot; {c.open} open rate
                      </div>
                    </div>
                    <span
                      className={`text-[10px] px-2 py-0.5 font-sans ${
                        c.status === "Sent" ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"
                      }`}
                    >
                      {c.status}
                    </span>
                  </div>
                ))}
              </div>

              <div className="bg-card border border-border p-6">
                <h3 className="text-[12px] font-medium text-foreground mb-5 font-sans tracking-wide">New Campaign</h3>
                <div className="space-y-3">
                  <input
                    type="text"
                    placeholder="Campaign subject line..."
                    className="w-full border border-border bg-background px-4 py-2.5 text-[12px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                  <textarea
                    rows={4}
                    placeholder="Email body..."
                    className="w-full border border-border bg-background px-4 py-2.5 text-[12px] focus:outline-none focus:border-accent transition-colors resize-none placeholder:text-muted-foreground/40 font-sans"
                  />
                  <div className="flex gap-3">
                    <button className="bg-foreground text-primary-foreground px-5 py-2.5 text-[11px] tracking-wide hover:bg-accent transition-colors flex items-center gap-2 font-sans">
                      <Send size={11} /> Send Campaign
                    </button>
                    <button className="border border-border px-5 py-2.5 text-[11px] tracking-wide text-muted-foreground hover:text-foreground hover:border-foreground transition-colors font-sans">
                      Save as Draft
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* ── ANALYTICS ── */}
          {section === "analytics" && (
            <div className="space-y-6">
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                {ADMIN_STATS.map((stat) => (
                  <div key={stat.label} className="bg-card border border-border p-5">
                    <div className="flex items-start justify-between mb-3">
                      <stat.icon size={16} className={stat.color} />
                      <span className="text-[10px] text-emerald-600 font-medium font-sans">{stat.change}</span>
                    </div>
                    <div className="font-serif text-[1.8rem] font-light text-foreground leading-none">{stat.value}</div>
                    <div className="text-[11px] text-muted-foreground mt-1.5 font-sans">{stat.label}</div>
                  </div>
                ))}
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
                <div className="lg:col-span-2 bg-card border border-border p-6">
                  <h3 className="text-[12px] font-medium text-foreground mb-5 font-sans tracking-wide">Revenue Trend</h3>
                  <ResponsiveContainer width="100%" height={200}>
                    <LineChart data={monthlyData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="rgba(0,0,0,0.05)" />
                      <XAxis dataKey="month" tick={{ fontSize: 10, fontFamily: "DM Sans" }} />
                      <YAxis tick={{ fontSize: 10, fontFamily: "DM Sans" }} />
                      <Tooltip
                        formatter={(v: number) => [`£${v.toLocaleString()}`, "Revenue"]}
                        contentStyle={{ fontSize: 12, fontFamily: "DM Sans" }}
                      />
                      <Line type="monotone" dataKey="revenue" stroke="#B8946A" strokeWidth={1.5} dot={false} />
                    </LineChart>
                  </ResponsiveContainer>
                </div>

                <div className="bg-card border border-border p-6">
                  <h3 className="text-[12px] font-medium text-foreground mb-5 font-sans tracking-wide">Services Mix</h3>
                  <ResponsiveContainer width="100%" height={150}>
                    <PieChart>
                      <Pie
                        data={serviceBreakdown}
                        dataKey="value"
                        nameKey="name"
                        cx="50%"
                        cy="50%"
                        outerRadius={60}
                        innerRadius={32}
                      >
                        {serviceBreakdown.map((entry, index) => (
                          <Cell key={index} fill={entry.color} />
                        ))}
                      </Pie>
                      <Tooltip contentStyle={{ fontSize: 11, fontFamily: "DM Sans" }} />
                    </PieChart>
                  </ResponsiveContainer>
                  <div className="space-y-1.5 mt-2">
                    {serviceBreakdown.map((item) => (
                      <div key={item.name} className="flex items-center justify-between text-[11px]">
                        <div className="flex items-center gap-2">
                          <div className="w-2 h-2 rounded-full shrink-0" style={{ backgroundColor: item.color }} />
                          <span className="text-muted-foreground font-sans">{item.name}</span>
                        </div>
                        <span className="text-foreground font-medium font-sans">{item.value}%</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </main>
      {adminOrders.error ? (
        <div role="alert" className="fixed bottom-5 right-5 z-[80] max-w-sm bg-card border border-destructive/30 px-4 py-3 text-[11px] text-destructive shadow-lg">
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

