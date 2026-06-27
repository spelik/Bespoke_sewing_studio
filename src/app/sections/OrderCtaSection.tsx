import { ArrowRight, Scissors } from "lucide-react";
import type { NavigableSectionProps } from "./sectionTypes";

export function OrderCtaSection({ navigate }: NavigableSectionProps) {
  return (
      <section className="py-24 bg-secondary">
        <div className="max-w-2xl mx-auto px-6 lg:px-10 text-center">
          <Scissors size={20} className="mx-auto text-accent/60 mb-8" />
          <div className="text-[10px] tracking-[0.4em] uppercase text-muted-foreground mb-6 font-sans">
            Ready to Begin?
          </div>
          <h2 className="font-serif text-[2.4rem] lg:text-[3rem] font-light text-foreground mb-6 leading-tight">
            Your Perfect Fit
            <br />
            <em className="italic text-accent">Awaits You.</em>
          </h2>
          <p className="text-[13px] text-muted-foreground leading-relaxed mb-10 font-sans">
            Begin your order request today and we will be in touch within one working day to arrange your personal consultation at the studio.
          </p>
          <button
            onClick={() => navigate("order")}
            className="inline-flex items-center gap-2.5 bg-foreground text-primary-foreground px-10 py-4 text-[13px] tracking-wide hover:bg-accent transition-colors duration-300"
          >
            Place an Order Request <ArrowRight size={15} />
          </button>
        </div>
      </section>

  );
}
