import { Check } from "lucide-react";
import { CONTACT_PAGE_ITEMS } from "../appContent";
import { SectionLabel } from "../components/SectionLabel";
import { usePrototypeForm } from "../hooks/usePrototypeForm";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

export function ContactPage() {
  const { submitted: messageSent, handleSubmit } = usePrototypeForm("contact message");
  const { settings } = useSiteSettings();
  const contactItems = CONTACT_PAGE_ITEMS.map((item) => ({
    ...item,
    text:
      item.kind === "location"
        ? settings.serviceAreaText
        : item.kind === "phone"
          ? settings.publicPhone
          : settings.publicEmail ?? settings.contactIntroText,
  })).filter((item) => item.text);

  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="Contact" />
          <h1 className="font-serif text-[3rem] lg:text-[5rem] font-light text-foreground mt-4 leading-tight">
            Get in
            <br />
            <em className="italic text-accent">Touch.</em>
          </h1>
        </div>
      </div>

      <div className="py-24 bg-background">
        <div className="max-w-7xl mx-auto px-6 lg:px-10">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-20">
            {/* Info */}
            <div>
              <div className="space-y-8">
                {contactItems.map(({ icon: Icon, label, text: value }) => (
                  <div key={label} className="flex gap-4">
                    <div className="w-10 h-10 border border-border flex items-center justify-center shrink-0">
                      <Icon size={13} className="text-accent" />
                    </div>
                    <div>
                      <div className="text-[10px] tracking-[0.25em] uppercase text-muted-foreground mb-1.5 font-sans">{label}</div>
                      <div className="text-[13px] text-foreground whitespace-pre-line font-sans leading-relaxed">{value}</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Message form */}
            <div>
              <h2 className="font-serif text-[1.8rem] font-light text-foreground mb-8">
                Send a Message
              </h2>
              {messageSent ? (
                <div className="border border-accent/30 bg-secondary p-8 text-center">
                  <Check size={20} className="mx-auto text-accent mb-3" />
                  <p className="font-serif text-[1rem] font-light text-foreground">Message received — thank you.</p>
                  <p className="text-[12px] text-muted-foreground mt-2 font-sans">We will be in touch within one working day.</p>
                </div>
              ) : (
                <form
                  className="space-y-4"
                  onSubmit={handleSubmit}
                >
                  <input
                    name="name"
                    type="text"
                    required
                    placeholder="Your Name"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                  <input
                    name="email"
                    type="email"
                    required
                    placeholder="Email Address"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                  <textarea
                    name="message"
                    required
                    rows={5}
                    placeholder="Your message..."
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors resize-none placeholder:text-muted-foreground/40 font-sans"
                  />
                  <button
                    type="submit"
                    className="w-full bg-foreground text-primary-foreground py-3.5 text-[13px] tracking-wide hover:bg-accent transition-colors font-sans"
                  >
                    Send Message
                  </button>
                </form>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

