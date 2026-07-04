import type { AdminOrderListItem } from "../../api/ordersApi";
import { ADMIN_STATUS_LABELS } from "../components/adminOrderFormatting";
import { createCsvFileName, downloadCsv } from "./csvExport";

export function exportOrdersCsv(
  orders: readonly AdminOrderListItem[],
): void {
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
