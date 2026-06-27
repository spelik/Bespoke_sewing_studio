import { Eye } from "lucide-react";
import type { AdminOrderListItem } from "../../api/ordersApi";
import {
  ADMIN_STATUS_COLORS,
  ADMIN_STATUS_LABELS,
  formatAdminDate,
  formatServiceType,
} from "./adminOrderFormatting";

interface AdminOrdersTableProps {
  orders: AdminOrderListItem[];
  isLoading: boolean;
  emptyMessage?: string;
  onSelect(id: string): void;
}

export function AdminOrdersTable({
  orders,
  isLoading,
  emptyMessage = "No enquiries have been received yet.",
  onSelect,
}: AdminOrdersTableProps) {
  return (
    <div className="bg-card border border-border overflow-x-auto">
      <table className="w-full min-w-[820px]">
        <thead>
          <tr className="border-b border-border bg-secondary/40">
            {[
              "Client",
              "Contact",
              "Service",
              "Message",
              "Created",
              "Status",
              "",
            ].map((heading) => (
              <th
                key={heading || "actions"}
                className="px-5 py-3 text-left text-[10px] tracking-wider text-muted-foreground font-sans font-normal"
              >
                {heading}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {isLoading ? (
            <tr>
              <td colSpan={7} className="px-5 py-10 text-center text-[11px] text-muted-foreground">
                Loading enquiries...
              </td>
            </tr>
          ) : null}
          {!isLoading && orders.length === 0 ? (
            <tr>
              <td colSpan={7} className="px-5 py-10 text-center text-[11px] text-muted-foreground">
                {emptyMessage}
              </td>
            </tr>
          ) : null}
          {!isLoading
            ? orders.map((order) => (
                <tr
                  key={order.id}
                  className="border-b border-border/40 hover:bg-secondary/25 transition-colors"
                >
                  <td className="px-5 py-3.5 text-[12px] text-foreground font-sans">
                    <div>{order.clientName}</div>
                    <div className="text-[9px] text-muted-foreground font-mono mt-0.5">
                      {order.id.slice(0, 8)}
                    </div>
                  </td>
                  <td className="px-5 py-3.5 text-[10px] text-muted-foreground font-sans max-w-[170px]">
                    <div className="truncate">{order.clientEmail ?? "No email"}</div>
                    <div className="truncate mt-0.5">{order.clientPhone ?? "No phone"}</div>
                  </td>
                  <td className="px-5 py-3.5 text-[11px] text-muted-foreground font-sans">
                    {formatServiceType(order.serviceType)}
                  </td>
                  <td className="px-5 py-3.5 text-[10px] text-muted-foreground font-sans max-w-[220px]">
                    <p className="line-clamp-2">{order.description}</p>
                  </td>
                  <td className="px-5 py-3.5 text-[10px] text-muted-foreground font-sans whitespace-nowrap">
                    {formatAdminDate(order.createdAt)}
                  </td>
                  <td className="px-5 py-3.5">
                    <span
                      className={`text-[10px] px-2 py-0.5 whitespace-nowrap font-sans ${ADMIN_STATUS_COLORS[order.status]}`}
                    >
                      {ADMIN_STATUS_LABELS[order.status]}
                    </span>
                  </td>
                  <td className="px-5 py-3.5">
                    <button
                      type="button"
                      onClick={() => onSelect(order.id)}
                      className="inline-flex items-center gap-1.5 text-[10px] text-muted-foreground hover:text-foreground transition-colors"
                      aria-label={`View enquiry from ${order.clientName}`}
                    >
                      <Eye size={13} /> View
                    </button>
                  </td>
                </tr>
              ))
            : null}
        </tbody>
      </table>
    </div>
  );
}
