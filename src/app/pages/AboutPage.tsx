import { SITE_ASSETS, WHY_US } from "../appContent";
import { ResponsiveImage } from "../components/ResponsiveImage";
import { SectionLabel } from "../components/SectionLabel";
import { usePageNavigation } from "../routing/usePageNavigation";

export function AboutPage() {
  const navigate = usePageNavigation();
  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="About Us" />
          <h1 className="font-serif text-[3rem] lg:text-[5rem] font-light text-foreground mt-4 leading-tight">
            A Studio
            <br />
            <em className="italic text-accent">Built on Craft.</em>
          </h1>
        </div>
      </div>

      {/* Story */}
      <section className="py-24 bg-background">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
            <div className="aspect-[4/5] bg-muted overflow-hidden">
              <ResponsiveImage
                asset={SITE_ASSETS.aboutHero}
                alt="Bespoke Sewing Studio workspace"
                pictureClassName="block w-full h-full"
                imgClassName="w-full h-full object-cover grayscale hover:grayscale-0 transition-all duration-700"
                decoding="async"
              />
            </div>
            <div>
              <div className="text-[10px] tracking-[0.4em] uppercase text-muted-foreground mb-6 font-sans">
                Our Story
              </div>
              <h2 className="font-serif text-[2rem] lg:text-[2.5rem] font-light text-foreground mb-8 leading-tight">
                Crafted with passion,
                <br />
                <em className="italic text-accent">and a refined personal touch.</em>
              </h2>
              <div className="space-y-4 text-[13px] text-muted-foreground leading-relaxed font-sans">
                <p>
                  Bespoke Sewing Studio offers premium sewing, tailoring, dressmaking, alterations, and memory bears.
                </p>
                <p>
                  Born from a lifelong passion for fabric, form, and the art of creating garments that truly fit, our work ranges from intricate wedding dress alterations to full bespoke commissions and deeply personal memory bears, always with a quiet dedication to quality.
                </p>
                <p>
                  Every piece is created with care, attention to detail, and a refined personal touch.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Values */}
      <section className="py-24 bg-secondary">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <SectionLabel text="Our Values" />
          <h2 className="font-serif text-[2.2rem] lg:text-[2.8rem] font-light text-foreground mb-16 text-center">
            What We Stand For
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-10">
            {WHY_US.map((v) => (
              <div key={v.title} className="text-center">
                <div className="w-11 h-11 border border-border flex items-center justify-center mx-auto mb-5">
                  <v.icon size={16} className="text-accent" />
                </div>
                <h3 className="font-serif text-[0.95rem] font-light text-foreground mb-3">{v.title}</h3>
                <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">{v.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20 bg-foreground text-primary-foreground">
        <div className="max-w-2xl mx-auto px-6 text-center">
          <h2 className="font-serif text-[2rem] font-light mb-4">
            Discuss Your Order
          </h2>
          <p className="text-[13px] text-primary-foreground/60 mb-8 font-sans leading-relaxed">
            Please send an enquiry to discuss your order. Consultations and orders are arranged individually.
          </p>
          <button
            onClick={() => navigate("order")}
            className="bg-primary-foreground text-foreground px-8 py-3.5 text-[13px] tracking-wide hover:bg-accent hover:text-accent-foreground transition-colors font-sans"
          >
            Send an Enquiry
          </button>
        </div>
      </section>
    </div>
  );
}

