import { MapPin } from "lucide-react";
import { HOME_CONTACT_ITEMS } from "../appContent";
import { SectionLabel } from "../components/SectionLabel";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

export function ContactSection() {
  const { settings } = useSiteSettings();
  const contactItems = HOME_CONTACT_ITEMS.map((item) => ({
    ...item,
    text:
      item.kind === "location"
        ? settings.serviceAreaText
        : item.kind === "phone"
          ? settings.publicPhone
          : item.kind === "email"
            ? settings.publicEmail ?? settings.contactIntroText
            : item.text,
  })).filter((item) => item.text);

  return (
      <section className="py-24 bg-background">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
            <div>
              <SectionLabel text="Find Us" />
              <h2 className="font-serif text-[2.4rem] font-light text-foreground mb-8">
                Visit the Studio
              </h2>
              <div className="space-y-5">
                {contactItems.map(({ icon: Icon, kind, text }) => (
                  <div key={kind} className="flex items-start gap-3 text-[13px] text-muted-foreground font-sans">
                    <Icon size={14} className="text-accent mt-0.5 shrink-0" />
                    <span>{text}</span>
                  </div>
                ))}
              </div>
            </div>
            <div className="aspect-[4/3] bg-secondary border border-border flex items-center justify-center">
              <div className="text-center text-muted-foreground">
                <MapPin size={28} className="mx-auto mb-3 text-accent/40" />
                <p className="text-sm font-serif font-light">Logosha Studio</p>
                <p className="text-[11px] mt-1 text-muted-foreground/60 font-sans">Northern Ireland</p>
              </div>
            </div>
          </div>
        </div>
      </section>
  );
}
