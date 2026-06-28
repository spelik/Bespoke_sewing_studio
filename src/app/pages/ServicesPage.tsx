import { Scissors } from "lucide-react";
import { ServiceCard } from "../components/ServiceCard";
import { SectionLabel } from "../components/SectionLabel";
import { usePageNavigation } from "../routing/usePageNavigation";
import { useServices } from "../services/ServicesContext";

export function ServicesPage() {
  const navigate = usePageNavigation();
  const { services } = useServices();

  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="Services" />
          <h1 className="font-serif text-[3rem] lg:text-[5rem] font-light text-foreground mt-4 leading-tight">
            Our Craft,
            <br />
            <em className="italic text-accent">Your Vision.</em>
          </h1>
          <p className="text-[13px] text-muted-foreground mt-6 max-w-lg leading-relaxed font-sans">
            Every garment that passes through our studio receives the same devoted attention — whether a quick repair or a fully bespoke commission.
          </p>
        </div>
      </div>

      <div className="bg-background py-20">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {services.map((service) => (
              <ServiceCard
                key={service.id ?? service.slug}
                service={service}
                variant="detail"
                onRequest={() => navigate("order")}
              />
            ))}
          </div>

          {/* Consultation CTA */}
          <div className="mt-16 p-12 bg-secondary text-center">
            <Scissors size={22} className="mx-auto text-accent mb-5" />
            <h3 className="font-serif text-[1.6rem] font-light text-foreground mb-4">
              Not sure which service you need?
            </h3>
            <p className="text-[13px] text-muted-foreground max-w-md mx-auto mb-8 font-sans leading-relaxed">
              Book a complimentary initial consultation and we will advise you on the best approach for your garment and your budget.
            </p>
            <button
              onClick={() => navigate("order")}
              className="bg-foreground text-primary-foreground px-8 py-3.5 text-[13px] tracking-wide hover:bg-accent transition-colors"
            >
              Book Free Consultation
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

