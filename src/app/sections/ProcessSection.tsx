import { SectionLabel } from "../components/SectionLabel";
import { useRepeatableContent } from "../repeatableContent/RepeatableContentContext";

export function ProcessSection() {
  const { processSteps } = useRepeatableContent();

  return (
      <section className="py-24 bg-secondary">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <SectionLabel text="Process" />
          <h2 className="font-serif text-[2.4rem] lg:text-[3rem] font-light text-foreground mb-16 text-center">
            How It Works
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-10">
            {processSteps.map((step, i) => (
              <div key={`${step.step}-${step.title}`} className="relative">
                {i < processSteps.length - 1 && (
                  <div className="hidden lg:block absolute top-7 left-[65%] right-0 border-t border-dashed border-border" />
                )}
                <div className="font-serif text-[3.5rem] font-light text-accent/25 leading-none mb-4">{step.step}</div>
                <h3 className="font-serif text-[1rem] font-light text-foreground mb-3">{step.title}</h3>
                <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">{step.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

  );
}
