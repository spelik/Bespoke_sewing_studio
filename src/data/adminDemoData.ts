import type { AdminStatIconKey } from "../app/types";

export interface AdminStat {
  label: string;
  value: string;
  change: string;
  icon: AdminStatIconKey;
  color: string;
}

export type DemoOrderStatus = "In Progress" | "Awaiting Collection" | "Consultation" | "Completed";

export interface AdminOrder {
  id: string;
  client: string;
  service: string;
  status: DemoOrderStatus;
  date: string;
  amount: string;
}

export const ADMIN_STATS: ReadonlyArray<AdminStat> = [
  { label: "Orders This Month", value: "47", change: "+12%", icon: "orders", color: "text-amber-700" },
  { label: "Active Orders", value: "18", change: "+3 new", icon: "active", color: "text-blue-600" },
  { label: "Total Clients", value: "234", change: "+8 this month", icon: "clients", color: "text-emerald-600" },
  { label: "Revenue (Month)", value: "£6,840", change: "+18%", icon: "revenue", color: "text-rose-600" },
];

export const ADMIN_ORDERS: ReadonlyArray<AdminOrder> = [
  { id: "#ATL-2401", client: "Catherine O'Neill", service: "Wedding Dress Alteration", status: "In Progress", date: "15 Jun 2024", amount: "£180" },
  { id: "#ATL-2402", client: "Margaret Doherty", service: "Vintage Coat Restoration", status: "Awaiting Collection", date: "14 Jun 2024", amount: "£220" },
  { id: "#ATL-2403", client: "Siobhán McBride", service: "Bespoke Suit", status: "Consultation", date: "14 Jun 2024", amount: "£380" },
  { id: "#ATL-2404", client: "Fionnuala Walsh", service: "Evening Gown Alteration", status: "Completed", date: "13 Jun 2024", amount: "£95" },
  { id: "#ATL-2405", client: "Aoife Murphy", service: "Zipper Repair", status: "Completed", date: "12 Jun 2024", amount: "£25" },
  { id: "#ATL-2406", client: "Roisin Gallagher", service: "Made-to-Measure Dress", status: "In Progress", date: "11 Jun 2024", amount: "£450" },
];

export const MONTHLY_DATA = [
  { month: "Jan", orders: 28, revenue: 3840 },
  { month: "Feb", orders: 32, revenue: 4200 },
  { month: "Mar", orders: 41, revenue: 5100 },
  { month: "Apr", orders: 38, revenue: 4800 },
  { month: "May", orders: 43, revenue: 5600 },
  { month: "Jun", orders: 47, revenue: 6840 },
] as const;

export const SERVICE_BREAKDOWN = [
  { name: "Alterations", value: 38, color: "#B8946A" },
  { name: "Bespoke", value: 22, color: "#1C1917" },
  { name: "Wedding", value: 25, color: "#C4A882" },
  { name: "Vintage", value: 15, color: "#78716C" },
] as const;

export const STATUS_COLORS: Record<DemoOrderStatus, string> = {
  "In Progress": "bg-blue-100 text-blue-700",
  "Awaiting Collection": "bg-amber-100 text-amber-700",
  "Consultation": "bg-purple-100 text-purple-700",
  "Completed": "bg-emerald-100 text-emerald-700",
};
