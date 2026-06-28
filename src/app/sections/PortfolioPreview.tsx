import { ArrowRight } from "lucide-react";
import { PortfolioCard } from "../components/PortfolioCard";
import { SectionLabel } from "../components/SectionLabel";
import type { NavigableSectionProps } from "./sectionTypes";
import { usePortfolio } from "../portfolio/PortfolioContext";

export function PortfolioPreview({ navigate }: NavigableSectionProps) {
  const { items } = usePortfolio();
  const featured = items.filter((item) => item.isFeatured);
  return (
      <section className="py-24 bg-background">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <div className="flex items-end justify-between mb-12">
            <div>
              <SectionLabel text="Portfolio" />
              <h2 className="font-serif text-[2.4rem] lg:text-[3rem] font-light text-foreground">Our Work</h2>
            </div>
            <button
              onClick={() => navigate("portfolio")}
              className="flex items-center gap-2 text-[13px] text-muted-foreground hover:text-foreground transition-colors font-sans"
            >
              View all <ArrowRight size={13} />
            </button>
          </div>
          <div className="grid grid-cols-2 lg:grid-cols-3 gap-3 lg:gap-4">
            {(featured.length > 0 ? featured : items).slice(0, 3).map((item) => (
              <PortfolioCard key={item.id} item={item} />
            ))}
          </div>
        </div>
      </section>

  );
}
