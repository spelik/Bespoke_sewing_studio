import { WHY_US } from "../appContent";
import { SectionLabel } from "../components/SectionLabel";

export function StudioValuesSection() {
  return (
      <section className="py-24 bg-foreground text-primary-foreground">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 lg:gap-24 items-center">
            <div>
              <div className="text-[10px] tracking-[0.4em] uppercase text-primary-foreground/35 mb-6 font-sans">
                Why Choose Us
              </div>
              <h2 className="font-serif text-[2.4rem] lg:text-[3.2rem] font-light leading-tight mb-8">
                Where Tradition
                <br />
                <em className="italic text-accent">Meets Precision.</em>
              </h2>
              <p className="text-[13px] text-primary-foreground/60 leading-relaxed font-sans max-w-md">
                We bring together Northern Irish craftsmanship heritage and modern tailoring precision. Every client receives our full attention, every garment our full skill.
              </p>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-8 lg:gap-10">
              {WHY_US.map((item) => (
                <div key={item.title}>
                  <div className="w-9 h-9 border border-primary-foreground/15 flex items-center justify-center mb-4">
                    <item.icon size={15} className="text-accent" />
                  </div>
                  <h3 className="font-serif text-[0.95rem] font-light text-primary-foreground mb-2">{item.title}</h3>
                  <p className="text-[13px] text-primary-foreground/55 leading-relaxed font-sans">{item.desc}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

  );
}
