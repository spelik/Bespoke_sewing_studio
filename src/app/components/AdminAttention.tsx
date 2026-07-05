import type { AdminSection } from "../admin/adminSections";

export interface AttentionCounts {
  newCount: number;
  totalCount: number;
}

export function getNavAttentionCounts(
  section: AdminSection,
  orderAttentionCounts: AttentionCounts,
  contactAttentionCounts: AttentionCounts | null,
  emailOutboxAttentionCounts: AttentionCounts | null = null,
): AttentionCounts | null {
  if (section === "orders") {
    return orderAttentionCounts;
  }

  if (section === "contactMessages") {
    return contactAttentionCounts;
  }

  if (section === "emailLog") {
    return emailOutboxAttentionCounts;
  }

  return null;
}

export function AttentionBadge({
  counts,
}: {
  counts: AttentionCounts | null;
}) {
  if (!counts || counts.newCount <= 0) {
    return null;
  }

  return (
    <span className="shrink-0 rounded-full bg-rose-100 px-2 py-0.5 text-[9px] font-sans text-rose-700">
      {counts.newCount} new
    </span>
  );
}

export function AttentionSummaryCards({
  items,
}: {
  items: ReadonlyArray<{ label: string; value: number; tone?: "accent" }>;
}) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
      {items.map((item) => (
        <div
          key={item.label}
          className="bg-card border border-border px-5 py-4"
        >
          <div className="text-[10px] uppercase tracking-wider text-muted-foreground font-sans">
            {item.label}
          </div>
          <div
            className={`mt-1 text-[1.45rem] font-serif font-light ${item.tone === "accent" ? "text-rose-700" : "text-foreground"}`}
          >
            {item.value}
          </div>
        </div>
      ))}
    </div>
  );
}
