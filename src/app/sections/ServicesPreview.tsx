import { ChevronRight } from "lucide-react";
import { ServiceCard } from "../components/ServiceCard";
import { SectionLabel } from "../components/SectionLabel";
import type { NavigableSectionProps } from "./sectionTypes";
import { useServices } from "../services/ServicesContext";

export function ServicesPreview({ navigate }: NavigableSectionProps) {
  const { services } = useServices();
  const featuredServices = services.filter((service) => service.isFeatured);
  const displayedServices = featuredServices.length > 0 ? featuredServices : services;
  return (
      <section className="py-24 lg:py-32 bg-background">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <SectionLabel text="Our Services" />
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-10 lg:gap-20 items-end mb-16">
            <h2 className="font-serif text-[2.4rem] lg:text-[3.2rem] font-light text-foreground leading-tight">
              Every Stitch,
              <br />
              <em className="italic text-accent">a Promise of Care.</em>
            </h2>
            <div>
              <p className="text-sm text-muted-foreground leading-relaxed mb-6 font-sans">
                From bridal alterations to fully bespoke commissions, we handle your garments with the reverence they deserve. Browse our complete range of specialist services.
              </p>
              <button
                onClick={() => navigate("services")}
                className="flex items-center gap-2 text-sm text-foreground border-b border-foreground pb-0.5 hover:text-accent hover:border-accent transition-colors font-sans"
              >
                All Services <ChevronRight size={13} />
              </button>
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
