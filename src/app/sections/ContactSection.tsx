import { Mail, MapPin, Phone } from "lucide-react";
import { SectionLabel } from "../components/SectionLabel";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

export function ContactSection() {
  const { settings, brand } = useSiteSettings();
  const contactItems = [
    { kind: "location", icon: MapPin, text: settings.serviceAreaText },
    { kind: "phone", icon: Phone, text: settings.phone },
    { kind: "email", icon: Mail, text: settings.email ?? settings.contactIntroText },
  ].filter((item): item is typeof item & { text: string } => Boolean(item.text));

  return (
      <section className="py-24 bg-background">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
            <div>
              <SectionLabel text="Contact" />
              <h2 className="font-serif text-[2.4rem] font-light text-foreground mb-8">
                Get in Touch
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
                <p className="text-sm font-serif font-light">{brand.brandDisplayName}</p>
                {settings.serviceAreaText ? <p className="text-[11px] mt-1 text-muted-foreground/60 font-sans">{settings.serviceAreaText}</p> : null}
              </div>
            </div>
          </div>
        </div>
      </section>
  );
}
