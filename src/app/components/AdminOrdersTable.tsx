import { Eye, LoaderCircle, Trash2 } from "lucide-react";
import type { AdminOrderListItem } from "../../api/ordersApi";
import {
  ADMIN_STATUS_COLORS,
  ADMIN_STATUS_LABELS,
  formatAdminDate,
} from "./adminOrderFormatting";
import { AdminTableState } from "./AdminUi";

interface AdminOrdersTableProps {
  orders: AdminOrderListItem[];
  isLoading: boolean;
  emptyMessage?: string;
  deletingOrderId?: string | null;
  onSelect(id: string): void;
  onRequestDelete(order: AdminOrderListItem): void;
}

export function AdminOrdersTable({
  orders,
  isLoading,
  emptyMessage = "No enquiries have been received yet.",
  deletingOrderId = null,
  onSelect,
  onRequestDelete,
}: AdminOrdersTableProps) {
  return (
    <div className="bg-card border border-border overflow-hidden">
      <table className="w-full table-fixed">
        <colgroup>
          <col className="w-[18%]" />
          <col className="w-[18%]" />
          <col className="w-[13%]" />
          <col className="w-[18%]" />
          <col className="w-[10%]" />
          <col className="w-[10%]" />
          <col className="w-[13%]" />
        </colgroup>
        <thead>
          <tr className="border-b border-border bg-secondary/40">
            {[
              "Client",
              "Contact",
              "Service",
              "Message",
              "Created",
              "Status",
              "Actions",
            ].map((heading) => (
              <th
                key={heading || "actions"}
                className="px-3 py-3 text-left text-[10px] tracking-wider text-muted-foreground font-sans font-normal"
              >
                {heading}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {isLoading ? (
            <tr>
              <td colSpan={7}>
                <AdminTableState message="Loading enquiries..." isLoading />
              </td>
            </tr>
          ) : null}
          {!isLoading && orders.length === 0 ? (
            <tr>
              <td colSpan={7}>
                <AdminTableState message={emptyMessage} />
              </td>
            </tr>
          ) : null}
          {!isLoading
            ? orders.map((order) => (
                <tr
                  key={order.id}
                  className="border-b border-border/40 hover:bg-secondary/25 transition-colors"
                >
                  <td className="px-3 py-3.5 text-[12px] text-foreground font-sans min-w-0">
                    <div className="truncate">{order.clientName}</div>
                    <div className="truncate text-[9px] text-muted-foreground font-mono mt-0.5">
                      {order.referenceNumber}
                    </div>
                  </td>
                  <td className="px-3 py-3.5 text-[10px] text-muted-foreground font-sans min-w-0">
                    <div className="truncate">{order.clientEmail ?? "No email"}</div>
                    <div className="truncate mt-0.5">{order.clientPhone ?? "No phone"}</div>
                  </td>
                  <td className="px-3 py-3.5 text-[11px] text-muted-foreground font-sans min-w-0">
                    <span className="line-clamp-2 overflow-hidden break-words" title={order.serviceName}>
                      {order.serviceName}
                    </span>
                  </td>
                  <td className="px-3 py-3.5 text-[10px] text-muted-foreground font-sans min-w-0">
                    <p className="line-clamp-2 overflow-hidden break-words" title={order.description}>
                      {order.description}
                    </p>
                  </td>
                  <td className="px-3 py-3.5 text-[10px] text-muted-foreground font-sans whitespace-nowrap">
                    {formatAdminDate(order.createdAt)}
                  </td>
                  <td className="px-3 py-3.5">
                    <span
                      className={`text-[10px] px-2 py-0.5 whitespace-nowrap font-sans ${ADMIN_STATUS_COLORS[order.status]}`}
                    >
                      {ADMIN_STATUS_LABELS[order.status]}
                    </span>
                  </td>
                  <td className="px-3 py-3.5">
                    <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                      <button
                        type="button"
                        onClick={() => onSelect(order.id)}
                        className="inline-flex items-center gap-1 text-[10px] text-muted-foreground hover:text-foreground transition-colors"
                        aria-label={`View enquiry ${order.referenceNumber} from ${order.clientName}`}
                      >
                        <Eye size={13} /> View
                      </button>
                      <button
                        type="button"
                        onClick={() => onRequestDelete(order)}
                        disabled={deletingOrderId === order.id}
                        className="inline-flex items-center gap-1 text-[10px] text-destructive hover:text-foreground disabled:opacity-50 transition-colors"
                        aria-label={`Delete enquiry ${order.referenceNumber} from ${order.clientName}`}
                      >
                        {deletingOrderId === order.id ? (
                          <LoaderCircle size={13} className="animate-spin" />
                        ) : (
                          <Trash2 size={13} />
                        )}
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            : null}
        </tbody>
      </table>
    </div>
  );
}
