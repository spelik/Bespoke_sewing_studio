import { Link } from "react-router-dom";
import { SectionLabel } from "../components/SectionLabel";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

const SERVICE_ITEMS = [
  {
    title: "1. About these terms",
    body:
      "These service terms explain how enquiries, consultations and sewing service requests are handled by the studio. They are written for general information and should be reviewed before public launch to match the final business setup.",
  },
  {
    title: "2. Enquiries and consultations",
    body:
      "Submitting an order request or contact message does not create a confirmed booking. The studio reviews each enquiry and will contact you to discuss the work, timing, measurements, fittings and any practical requirements before accepting a project.",
  },
  {
    title: "3. Quotes, prices and payments",
    body:
      "Any prices shown on the website are guide prices unless clearly confirmed in writing for your specific request. Final pricing can depend on garment condition, fabric, complexity, fitting requirements and agreed deadlines.",
  },
  {
    title: "4. Garments, fittings and client materials",
    body:
      "Clients are responsible for providing accurate information, attending agreed fittings where required, and supplying garments, fabrics or keepsake materials in a clean and suitable condition. Delays in providing materials or fitting information may affect completion dates.",
  },
  {
    title: "5. Uploaded photos and files",
    body:
      "The order form may allow photos or PDF files to help explain your request. Please upload only files that are relevant to your enquiry and do not include unnecessary personal or sensitive information.",
  },
  {
    title: "6. Timelines",
    body:
      "Preferred dates are treated as requests, not guaranteed deadlines. The studio will confirm realistic timings after reviewing the enquiry and current availability.",
  },
  {
    title: "7. Changes and cancellations",
    body:
      "If you need to change or cancel a request, contact the studio as soon as possible. The practical outcome may depend on whether work has already started, materials have been prepared, or a fitting time has been reserved.",
  },
  {
    title: "8. Website information",
    body:
      "The website is intended to describe the studio and accept enquiries. We aim to keep information accurate, but service details, availability, guide prices and content may change over time.",
  },
];

export function TermsPage() {
  const { brand, settings } = useSiteSettings();
  const contactText = settings.email ?? settings.phone ?? settings.contactIntroText;

  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="Service information" />
          <h1 className="font-serif text-[2.8rem] lg:text-[4rem] font-light text-foreground mt-4">
            Terms &amp; Service Information
          </h1>
          <p className="text-[13px] text-muted-foreground mt-4 font-sans max-w-2xl leading-relaxed">
            Practical information about enquiries, order requests, uploaded files,
            consultations and service arrangements with {brand.brandDisplayName}.
          </p>
        </div>
      </div>

      <div className="py-16 bg-background">
        <div className="max-w-3xl mx-auto px-6 lg:px-10">
          <div className="border border-border bg-secondary/20 p-6 mb-12">
            <h2 className="font-serif text-[1.35rem] font-light text-foreground mb-3">
              Important note
            </h2>
            <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">
              These terms are a plain-English service notice for the website. They
              should be checked and finalised by the business owner before the site
              is launched publicly.
            </p>
          </div>

          <div className="space-y-10">
            {SERVICE_ITEMS.map((item) => (
              <section key={item.title} className="border-t border-border pt-8">
                <h2 className="font-serif text-[1.15rem] font-light text-foreground mb-4">
                  {item.title}
                </h2>
                <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">
                  {item.body}
                </p>
              </section>
            ))}

            <section className="border-t border-border pt-8">
              <h2 className="font-serif text-[1.15rem] font-light text-foreground mb-4">
                9. Privacy and data protection
              </h2>
              <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">
                Contact and order forms collect information needed to respond to
                enquiries and arrange sewing services. More detail is available in
                the{" "}
                <Link to="/privacy" className="text-foreground underline underline-offset-2 hover:text-accent transition-colors">
                  Privacy Policy
                </Link>
                .
              </p>
            </section>

            {contactText ? (
              <section className="border-t border-border pt-8">
                <h2 className="font-serif text-[1.15rem] font-light text-foreground mb-4">
                  10. Contact
                </h2>
                <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">
                  Questions about a request, booking or service arrangement can be
                  sent through the contact form or using the published studio
                  contact details: {contactText}.
                </p>
              </section>
            ) : null}
          </div>
        </div>
      </div>
    </div>
  );
}
