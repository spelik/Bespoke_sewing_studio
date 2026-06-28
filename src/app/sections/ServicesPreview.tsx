import { ChevronRight } from "lucide-react";
import { ServiceCard } from "../components/ServiceCard";
import { SectionLabel } from "../components/SectionLabel";
import { useServices } from "../services/ServicesContext";
import { usePageContent } from "../content/PageContentContext";
import { CmsHeading } from "../components/CmsHeading";
import { AppLink } from "../components/AppLink";

export function ServicesPreview() {
  const { services } = useServices();
  const featuredServices = services.filter((service) => service.isFeatured);
  const displayedServices = featuredServices.length > 0 ? featuredServices : services;
  const intro=usePageContent("home").section("intro");
  return (
      <section className="py-24 lg:py-32 bg-background">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <SectionLabel text="Our Services" />
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-10 lg:gap-20 items-end mb-16">
            {intro?.title ? <CmsHeading as="h2" title={intro.title} className="font-serif text-[2.4rem] lg:text-[3.2rem] font-light text-foreground leading-tight"/> : <div />}
            <div>
              {intro?.body ? <p className="text-sm text-muted-foreground leading-relaxed mb-6 font-sans">{intro.body}</p> : null}
              {intro?.ctaLabel && intro.ctaUrl ? <AppLink href={intro.ctaUrl} className="flex items-center gap-2 text-sm text-foreground border-b border-foreground pb-0.5 hover:text-accent hover:border-accent transition-colors font-sans">{intro.ctaLabel} <ChevronRight size={13} /></AppLink> : null}
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-px bg-border">
            {displayedServices.map((service) => (
              <ServiceCard key={service.id ?? service.slug} service={service} />
            ))}
          </div>
        </div>
      </section>

  );
}
