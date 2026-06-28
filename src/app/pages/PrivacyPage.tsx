import { PRIVACY_SECTIONS } from "../appContent";
import { SectionLabel } from "../components/SectionLabel";
import { usePageContent } from "../content/PageContentContext";

export function PrivacyPage() {
  const main=usePageContent("privacy").section("main-content");
  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="Legal" />
          <h1 className="font-serif text-[2.8rem] lg:text-[4rem] font-light text-foreground mt-4">{main?.title??"Privacy Policy"}</h1>
          <p className="text-[13px] text-muted-foreground mt-4 font-sans">{main?.subtitle??"Last updated: June 2024"}</p>
        </div>
      </div>
      <div className="py-16 bg-background">
        <div className="max-w-2xl mx-auto px-6 lg:px-10">
          <p className="text-[13px] text-muted-foreground leading-relaxed mb-12 font-sans">
            {main?.body??"Bespoke Sewing Studio (“we”, “our”, “us”) is committed to protecting your personal data and complying with applicable data protection legislation. This Privacy Policy explains how we collect, use, and protect your information when you interact with our studio or website."}
          </p>
          <div className="space-y-10">
            {PRIVACY_SECTIONS.map((s) => (
              <div key={s.title} className="border-t border-border pt-8">
                <h2 className="font-serif text-[1.1rem] font-light text-foreground mb-4">{s.title}</h2>
                <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">{s.body}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

