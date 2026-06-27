import { SERVICES } from "../appContent";

type ServiceViewModel = (typeof SERVICES)[number];

interface ServiceCardProps {
  service: ServiceViewModel;
  variant?: "preview" | "detail";
  onRequest?: () => void;
}

export function ServiceCard({ service, variant = "preview", onRequest }: ServiceCardProps) {
  const Icon = service.icon;

  if (variant === "detail") {
    return (
      <div className="flex gap-6 p-8 border border-border hover:border-accent/35 transition-colors duration-300 group">
        <div className="shrink-0">
          <div className="w-11 h-11 border border-border group-hover:border-accent/40 flex items-center justify-center transition-colors duration-300">
            <Icon size={16} className="text-accent" />
          </div>
        </div>
        <div className="flex-1">
          <h3 className="font-serif text-[1.15rem] font-light text-foreground mb-2">{service.title}</h3>
          <p className="text-[13px] text-muted-foreground leading-relaxed mb-3 font-sans">{service.detail}</p>
          <div className="flex items-center justify-between pt-3 border-t border-border/50">
            <span className="text-[11px] font-medium text-accent tracking-wide font-sans">{service.price}</span>
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
      <h3 className="font-serif text-[1.1rem] font-light text-foreground mb-3">{service.title}</h3>
      <p className="text-[13px] text-muted-foreground leading-relaxed mb-5 font-sans">{service.desc}</p>
      <span className="text-[11px] tracking-wider text-accent font-sans font-medium">{service.price}</span>
    </div>
  );
}
