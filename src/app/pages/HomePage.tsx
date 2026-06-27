import { usePageNavigation } from "../routing/usePageNavigation";
import { ContactSection } from "../sections/ContactSection";
import { HomeHero } from "../sections/HomeHero";
import { OrderCtaSection } from "../sections/OrderCtaSection";
import { PortfolioPreview } from "../sections/PortfolioPreview";
import { ProcessSection } from "../sections/ProcessSection";
import { ServicesPreview } from "../sections/ServicesPreview";
import { StudioValuesSection } from "../sections/StudioValuesSection";
import { TestimonialsSection } from "../sections/TestimonialsSection";

export function HomePage() {
  const navigate = usePageNavigation();

  return (
    <div>
      <HomeHero navigate={navigate} />
      <ServicesPreview navigate={navigate} />
      <ProcessSection />
      <PortfolioPreview navigate={navigate} />
      <StudioValuesSection />
      <TestimonialsSection />
      <OrderCtaSection navigate={navigate} />
      <ContactSection />
    </div>
  );
}
