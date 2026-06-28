import { ArrowRight, Scissors, Star } from "lucide-react";
import { SITE_ASSETS } from "../appContent";
import { ResponsiveImage } from "../components/ResponsiveImage";
import { usePageContent } from "../content/PageContentContext";
import { CmsHeading } from "../components/CmsHeading";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";
import { AppLink } from "../components/AppLink";

export function HomeHero() {
  const hero=usePageContent("home").section("hero");
  const {brand}=useSiteSettings();
  if(!hero) return null;
  return (
      <section className="relative min-h-screen flex items-center overflow-hidden bg-secondary">
        <div className="absolute inset-0 opacity-[0.07] pointer-events-none">
          {hero?.imageUrl ? <img src={hero.imageUrl} alt="" className="w-full h-full object-cover" aria-hidden="true" /> : <ResponsiveImage
            asset={SITE_ASSETS.homeHero}
            alt=""
            pictureClassName="block w-full h-full"
            imgClassName="w-full h-full object-cover"
            decoding="async"
            fetchPriority="low"
            aria-hidden="true"
          />}
        </div>
        <div className="relative z-10 max-w-7xl mx-auto px-6 lg:px-10 w-full pt-[72px]">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 lg:gap-20 items-center min-h-[88vh] py-16">
            {/* Text */}
            <div>
              <div className="text-[10px] tracking-[0.45em] uppercase text-muted-foreground mb-10 font-sans">
                {brand.brandDisplayName}
              </div>
              {hero.title ? <CmsHeading title={hero.title} accentLine={1} className="font-serif text-[3.2rem] lg:text-[4rem] font-light leading-[1.06] text-foreground mb-8" /> : null}
              {hero.subtitle ? <p className="text-base lg:text-[1.05rem] text-muted-foreground leading-relaxed max-w-[420px] mb-10 font-sans">{hero.subtitle}</p> : null}
              <div className="flex flex-col sm:flex-row gap-4">
                {hero.ctaLabel && hero.ctaUrl ? <AppLink href={hero.ctaUrl} className="flex items-center justify-center gap-2.5 bg-foreground text-primary-foreground px-8 py-4 text-[13px] tracking-wide hover:bg-accent transition-colors duration-300">{hero.ctaLabel} <ArrowRight size={15} /></AppLink> : null}
                {brand.navigation.showPortfolioLink ? <AppLink href="/portfolio" className="flex items-center justify-center gap-2.5 border border-foreground text-foreground px-8 py-4 text-[13px] tracking-wide hover:bg-foreground hover:text-primary-foreground transition-colors duration-300">{brand.navigation.portfolioLabel}</AppLink> : null}
              </div>
            </div>

            {/* Image + badge */}
            <div className="relative hidden lg:block">
              <div className="relative aspect-[4/5] bg-muted overflow-hidden">
                {hero.imageUrl ? <img src={hero.imageUrl} alt={hero.imageAltText ?? brand.brandDisplayName} className="w-full h-full object-cover" /> : <ResponsiveImage
                  asset={SITE_ASSETS.homeHero}
                  alt={hero.imageAltText ?? brand.brandDisplayName}
                  pictureClassName="block w-full h-full"
                  imgClassName="w-full h-full object-cover"
                  decoding="async"
                  fetchPriority="high"
                />}
                <div className="absolute inset-0 bg-foreground/5" />
              </div>
              <div className="absolute -bottom-5 -left-5 bg-background border border-border p-6 shadow-sm">
                <div className="text-[2.4rem] font-serif font-light text-foreground leading-none flex justify-center"><Star size={32} className="text-accent" /></div>
                <div className="text-[10px] tracking-[0.25em] uppercase text-muted-foreground mt-2 font-sans text-center">
                  100%<br />Bespoke
                </div>
              </div>
              <div className="absolute -top-4 -right-4 bg-accent p-4">
                <Scissors size={18} className="text-accent-foreground" />
              </div>
            </div>
          </div>
        </div>
        {/* Scroll cue */}
        <div className="absolute bottom-8 left-1/2 -translate-x-1/2 flex flex-col items-center gap-2">
          <div className="w-px h-10 bg-border/60 animate-pulse" />
          <span className="text-[9px] tracking-[0.4em] uppercase text-muted-foreground/60 font-sans">Scroll</span>
        </div>
      </section>

  );
}
