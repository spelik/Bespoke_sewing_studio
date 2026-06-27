import type { OrderServiceType, ServiceItem } from "../app/types";
import { MEMORY_BEAR_SERVICE_DETAIL } from "./memoryBearsData";

export const ORDER_SERVICE_TYPES: ReadonlyArray<OrderServiceType> = [
  "Tailoring",
  "Dressmaking",
  "Alterations",
  "Memory Bears",
];

export const SERVICES: ReadonlyArray<ServiceItem> = [
  {
    icon: "scissors",
    title: "Tailoring",
    desc: "Bespoke tailoring crafted to your exact measurements and personal aesthetic.",
    price: "Enquire for pricing",
    detail: "Every piece begins with a detailed consultation and pattern drafting to achieve perfection.",
  },
  {
    icon: "sparkles",
    title: "Dressmaking",
    desc: "Custom dressmaking for everyday elegance or special occasions.",
    price: "Enquire for pricing",
    detail: "We work with all silhouettes and can accommodate specific requests for your unique style.",
  },
  {
    icon: "refresh",
    title: "Alterations",
    desc: "Expert alterations to ensure your garments fit perfectly, including bridal and formal wear.",
    price: "Enquire for pricing",
    detail: "Taking in, letting out, hemming, and delicate lace work — handled with precision.",
  },
  {
    icon: "package",
    title: "Memory Bears",
    desc: "Handmade keepsakes created from meaningful clothing.",
    price: "From £45",
    detail: MEMORY_BEAR_SERVICE_DETAIL,
  },
];
