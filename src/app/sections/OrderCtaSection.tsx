import { ArrowRight, Scissors } from "lucide-react";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";
import { AppLink } from "../components/AppLink";

export function OrderCtaSection() {
  const {settings,brand}=useSiteSettings();
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
            {settings.contactIntroText}
          </p>
          <AppLink href={brand.headerCtaUrl} className="inline-flex items-center gap-2.5 bg-foreground text-primary-foreground px-10 py-4 text-[13px] tracking-wide hover:bg-accent transition-colors duration-300">{brand.headerCtaLabel} <ArrowRight size={15} /></AppLink>
        </div>
      </section>

  );
}
