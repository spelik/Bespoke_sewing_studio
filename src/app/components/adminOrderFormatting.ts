import type { AdminOrderStatus } from "../../api/ordersApi";

export const ADMIN_STATUS_LABELS: Readonly<Record<AdminOrderStatus, string>> = {
  New: "New",
  Contacted: "Contacted",
  WaitingForDetails: "Waiting for details",
  Quoted: "Quoted",
  Accepted: "Accepted",
  InProgress: "In progress",
  ReadyForCollection: "Ready for collection",
  Completed: "Completed",
  Cancelled: "Cancelled",
};

export const ADMIN_STATUS_COLORS: Readonly<Record<AdminOrderStatus, string>> = {
  New: "bg-slate-100 text-slate-700",
  Contacted: "bg-purple-100 text-purple-700",
  WaitingForDetails: "bg-amber-100 text-amber-700",
  Quoted: "bg-cyan-100 text-cyan-700",
  Accepted: "bg-teal-100 text-teal-700",
  InProgress: "bg-blue-100 text-blue-700",
  ReadyForCollection: "bg-orange-100 text-orange-700",
  Completed: "bg-emerald-100 text-emerald-700",
  Cancelled: "bg-rose-100 text-rose-700",
};

export function formatAdminDate(value: string): string {
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(new Date(value));
}

export function formatServiceType(value: string): string {
  return value === "MemoryBear" ? "Memory Bear" : value;
}
