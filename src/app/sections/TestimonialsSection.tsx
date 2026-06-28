import { Star } from "lucide-react";
import { SectionLabel } from "../components/SectionLabel";
import { useRepeatableContent } from "../repeatableContent/RepeatableContentContext";

export function TestimonialsSection() {
  const { testimonials } = useRepeatableContent();

  return (
      <section className="py-24 bg-background">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <SectionLabel text="Testimonials" />
          <h2 className="font-serif text-[2.4rem] lg:text-[3rem] font-light text-foreground mb-16 text-center">
            What Our Clients Say
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {testimonials.map((t) => (
              <div
                key={t.name}
                className="p-8 border border-border hover:border-accent/30 transition-colors duration-300 bg-card group"
              >
                <div className="flex items-center gap-0.5 mb-5">
                  {Array.from({ length: t.rating }).map((_, i) => (
                    <Star key={i} size={11} className="fill-accent text-accent" />
                  ))}
                </div>
                <blockquote className="font-serif text-[1.05rem] font-light text-foreground leading-relaxed mb-6 italic">
                  &ldquo;{t.text}&rdquo;
                </blockquote>
                <div className="border-t border-border/50 pt-4">
                  <div className="text-[13px] font-medium text-foreground font-sans">{t.name}</div>
                  <div className="text-[11px] text-muted-foreground mt-0.5 font-sans">{t.location} &middot; {t.service}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

  );
}
