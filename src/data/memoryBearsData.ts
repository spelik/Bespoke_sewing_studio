import type { MemoryBearPrice } from "../app/types";

export const MEMORY_BEAR_PRICES: ReadonlyArray<MemoryBearPrice> = [
  { label: "Small (25cm)", price: "from £45" },
  { label: "Medium (30-35cm)", price: "from £65" },
  { label: "Large (40cm+)", price: "from £95" },
];

export const MEMORY_BEAR_EMBROIDERY_PRICE = "+£15";
export const MEMORY_BEAR_LEAD_TIME = "Ready in approx. 5 working days.";

export const MEMORY_BEAR_SERVICE_DETAIL = `${MEMORY_BEAR_PRICES.map(
  ({ label, price }) => `${label}: ${price}`,
).join(" | ")}\nPersonalised embroidery: ${MEMORY_BEAR_EMBROIDERY_PRICE}\n${MEMORY_BEAR_LEAD_TIME}`;
