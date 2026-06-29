import { type ChangeEvent, type FormEvent, useState } from "react";
import { Check, Mail, MapPin, Phone } from "lucide-react";
import {
  createContactMessage,
  getContactMessageSubmissionErrorMessage,
} from "../../api/contactMessagesApi";
import type { ContactMessageRequest } from "../types";
import { CmsHeading } from "../components/CmsHeading";
import { SectionLabel } from "../components/SectionLabel";
import { usePageContent } from "../content/PageContentContext";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

const emptyForm: ContactMessageRequest = {
  fullName: "",
  email: "",
  phone: "",
  subject: "",
  message: "",
  consent: false,
};

export function ContactPage() {
  const [form, setForm] = useState<ContactMessageRequest>(emptyForm);
  const [submittedReference, setSubmittedReference] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { settings } = useSiteSettings();
  const intro = usePageContent("contact").section("intro");
  const contactItems = [
    { label: "Service area", icon: MapPin, text: settings.serviceAreaText },
    { label: "Telephone", icon: Phone, text: settings.phone },
    { label: "Enquiries", icon: Mail, text: settings.email ?? settings.contactIntroText },
  ].filter((item): item is typeof item & { text: string } => Boolean(item.text));

  function handleInputChange(
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) {
    const { name, value } = event.currentTarget;
    const nextValue = event.currentTarget instanceof HTMLInputElement && event.currentTarget.type === "checkbox"
      ? event.currentTarget.checked
      : value;

    setForm((current) => ({
      ...current,
      [name]: nextValue,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const result = await createContactMessage(form);
      setForm(emptyForm);
      setSubmittedReference(result.referenceNumber);
    } catch (submissionError) {
      setError(getContactMessageSubmissionErrorMessage(submissionError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="Contact" />
          {intro?.title ? (
            <CmsHeading
              title={intro.title}
              className="font-serif text-[3rem] lg:text-[5rem] font-light text-foreground mt-4 leading-tight"
            />
          ) : null}
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
                      <div className="text-[10px] tracking-[0.25em] uppercase text-muted-foreground mb-1.5 font-sans">
                        {label}
                      </div>
                      <div className="text-[13px] text-foreground whitespace-pre-line font-sans leading-relaxed">
                        {value}
                      </div>
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
              {submittedReference ? (
                <div className="border border-accent/30 bg-secondary p-8 text-center" aria-live="polite">
                  <Check size={20} className="mx-auto text-accent mb-3" />
                  <p className="font-serif text-[1rem] font-light text-foreground">
                    Message received — thank you.
                  </p>
                  <p className="text-[12px] text-muted-foreground mt-2 font-sans">
                    We will be in touch within one working day.
                  </p>
                  <p className="text-[11px] text-muted-foreground/60 mt-3 font-sans">
                    Message reference: {submittedReference}
                  </p>
                  <button
                    type="button"
                    onClick={() => {
                      setSubmittedReference(null);
                      setError(null);
                    }}
                    className="mt-6 px-5 py-2.5 border border-border text-[11px] tracking-wide hover:border-foreground transition-colors font-sans"
                  >
                    Send another message
                  </button>
                </div>
              ) : (
                <form className="space-y-4" onSubmit={handleSubmit}>
                  <input
                    name="fullName"
                    type="text"
                    required
                    maxLength={200}
                    value={form.fullName}
                    onChange={handleInputChange}
                    placeholder="Your Name"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                  <input
                    name="email"
                    type="email"
                    required
                    maxLength={320}
                    value={form.email}
                    onChange={handleInputChange}
                    placeholder="Email Address"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                  <input
                    name="phone"
                    type="tel"
                    maxLength={50}
                    value={form.phone ?? ""}
                    onChange={handleInputChange}
                    placeholder="Telephone Number (optional)"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                  <input
                    name="subject"
                    type="text"
                    maxLength={250}
                    value={form.subject ?? ""}
                    onChange={handleInputChange}
                    placeholder="Subject (optional)"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                  <textarea
                    name="message"
                    required
                    maxLength={4000}
                    rows={5}
                    value={form.message}
                    onChange={handleInputChange}
                    placeholder="Your message..."
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors resize-none placeholder:text-muted-foreground/40 font-sans"
                  />
                  <label className="flex items-start gap-3 text-[11px] leading-relaxed text-muted-foreground font-sans">
                    <input
                      name="consent"
                      type="checkbox"
                      required
                      checked={form.consent}
                      onChange={handleInputChange}
                      className="mt-1 h-3.5 w-3.5 accent-foreground"
                    />
                    <span>
                      I agree that Bespoke Sewing Studio may use my details to respond to this enquiry.
                    </span>
                  </label>

                  {error ? (
                    <div role="alert" className="border border-destructive/30 bg-destructive/5 px-4 py-3 text-[12px] text-destructive font-sans">
                      {error}
                    </div>
                  ) : null}

                  <button
                    type="submit"
                    disabled={isSubmitting}
                    className="w-full bg-foreground text-primary-foreground py-3.5 text-[13px] tracking-wide hover:bg-accent transition-colors font-sans disabled:opacity-60 disabled:cursor-not-allowed"
                  >
                    {isSubmitting ? "Sending..." : "Send Message"}
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
