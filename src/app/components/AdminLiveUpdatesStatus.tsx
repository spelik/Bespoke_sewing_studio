import type { AdminRealtimeStatus } from "../../api/adminRealtimeApi";
import { formatAdminDate } from "./adminOrderFormatting";

interface AdminLiveUpdatesStatusProps {
  status: AdminRealtimeStatus;
  lastEventAt: string | null;
}

export function AdminLiveUpdatesStatus({
  status,
  lastEventAt,
}: AdminLiveUpdatesStatusProps) {
  const statusLabel: Record<AdminRealtimeStatus, string> = {
    connecting: "Live updates connecting",
    connected: "Live updates connected",
    disconnected: "Live updates disconnected",
  };
  const toneClass: Record<AdminRealtimeStatus, string> = {
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
