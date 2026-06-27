import { useState } from "react";
import { PORTFOLIO_CATEGORIES, PORTFOLIO_ITEMS } from "../appContent";
import { PortfolioCard } from "../components/PortfolioCard";
import { SectionLabel } from "../components/SectionLabel";
import { usePageNavigation } from "../routing/usePageNavigation";
import type { PortfolioFilter } from "../types";

export function PortfolioPage() {
  const navigate = usePageNavigation();
  const [filter, setFilter] = useState<PortfolioFilter>("all");
  const categories: ReadonlyArray<PortfolioFilter> = ["all", ...PORTFOLIO_CATEGORIES];
  const filtered = filter === "all" ? PORTFOLIO_ITEMS : PORTFOLIO_ITEMS.filter((i) => i.category === filter);

  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="Portfolio" />
          <h1 className="font-serif text-[3rem] lg:text-[5rem] font-light text-foreground mt-4 leading-tight">
            Gallery of
            <br />
            <em className="italic text-accent">Our Work.</em>
          </h1>
        </div>
      </div>

      <div className="bg-background py-16">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <div className="flex items-center gap-2 mb-12 flex-wrap">
            {categories.map((cat) => (
              <button
                key={cat}
                onClick={() => setFilter(cat)}
                className={`px-5 py-2 text-[11px] tracking-[0.2em] uppercase font-sans transition-colors duration-200 ${
                  filter === cat
                    ? "bg-foreground text-primary-foreground"
                    : "border border-border text-muted-foreground hover:border-foreground hover:text-foreground"
                }`}
              >
                {cat}
              </button>
            ))}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {filtered.map((item) => (
              <PortfolioCard key={item.id} item={item} variant="gallery" />
            ))}
          </div>

          {filtered.length === 0 && (
            <div className="text-center py-20 text-muted-foreground">
              <p className="font-serif text-lg font-light">No pieces in this category yet.</p>
              <p className="text-[13px] mt-2 font-sans">More work coming soon.</p>
            </div>
          )}

          <div className="mt-16 text-center">
            <p className="text-[13px] text-muted-foreground mb-4 font-sans">
              Interested in commissioning a piece or requesting alterations?
            </p>
            <button
              onClick={() => navigate("order")}
              className="text-[13px] text-foreground border-b border-foreground pb-0.5 hover:text-accent hover:border-accent transition-colors font-sans"
            >
              Place an Order Request &rarr;
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

