import type { PublicServiceOffering } from "../types";
import {
  getServiceIcon,
  getServicePriceSummary,
} from "../services/servicePresentation";

interface ServiceCardProps {
  service: PublicServiceOffering;
  variant?: "preview" | "detail";
  onRequest?: () => void;
}

export function ServiceCard({ service, variant = "preview", onRequest }: ServiceCardProps) {
  const Icon = getServiceIcon(service);
  const priceSummary = getServicePriceSummary(service);

  if (variant === "detail") {
    return (
      <div className="flex gap-6 p-8 border border-border hover:border-accent/35 transition-colors duration-300 group">
        <div className="shrink-0">
          <div className="w-11 h-11 border border-border group-hover:border-accent/40 flex items-center justify-center transition-colors duration-300">
            <Icon size={16} className="text-accent" />
          </div>
        </div>
        <div className="flex-1 min-w-0">
          <h3 className="font-serif text-[1.15rem] font-light text-foreground mb-2">{service.name}</h3>
          <p className="text-[13px] text-muted-foreground leading-relaxed mb-3 font-sans">
            {service.description ?? service.shortDescription}
          </p>
          {service.priceOptions.length > 0 ? (
            <ul className="space-y-1.5 mb-4 text-[11px] text-muted-foreground font-sans">
              {service.priceOptions.map((option) => (
                <li key={option.id ?? `${service.slug}-${option.displayOrder}`} className="flex justify-between gap-4">
                  <span>{option.label}</span>
                  <span className="text-accent whitespace-nowrap">{option.priceText}</span>
                </li>
              ))}
            </ul>
          ) : null}
          <div className="flex items-center justify-between gap-4 pt-3 border-t border-border/50">
            <span className="text-[11px] font-medium text-accent tracking-wide font-sans">{priceSummary}</span>
            <button
              onClick={onRequest}
              className="text-[11px] tracking-wide text-muted-foreground hover:text-foreground transition-colors border-b border-muted-foreground/30 hover:border-foreground pb-0.5 font-sans"
            >
              Request this service
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-background p-8 hover:bg-secondary transition-colors duration-300 group cursor-default">
      <Icon size={18} className="text-accent mb-5 group-hover:scale-110 transition-transform duration-300" />
      <h3 className="font-serif text-[1.1rem] font-light text-foreground mb-3">{service.name}</h3>
      <p className="text-[13px] text-muted-foreground leading-relaxed mb-5 font-sans">{service.shortDescription}</p>
      <span className="text-[11px] tracking-wider text-accent font-sans font-medium">{priceSummary}</span>
    </div>
  );
}
