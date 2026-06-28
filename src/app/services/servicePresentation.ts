import { SERVICE_ICONS } from "../iconRegistry";
import type { PublicServiceOffering, ServiceIconKey } from "../types";

const ICON_BY_SLUG: Readonly<Record<string, ServiceIconKey>> = {
  tailoring: "scissors",
  dressmaking: "sparkles",
  alterations: "refresh",
  "memory-bears": "package",
};

export function getServiceIcon(service: PublicServiceOffering) {
  return SERVICE_ICONS[ICON_BY_SLUG[service.slug] ?? "scissors"];
}

export function getServicePriceSummary(service: PublicServiceOffering): string {
  return service.priceOptions[0]?.priceText ?? "Enquire for pricing";
}
