import { Link } from "react-router-dom";
import { SectionLabel } from "../components/SectionLabel";
import { usePageContent } from "../content/PageContentContext";
import { useRepeatableContent } from "../repeatableContent/RepeatableContentContext";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

const DATA_NOTICE_BLOCKS = [
  {
    title: "Contact form enquiries",
    body:
      "When you send a contact message, the website may collect your name, email address, phone number, subject and message so the studio can respond to your enquiry.",
  },
  {
    title: "Order request forms",
    body:
      "When you submit an order request, the website may collect your contact details, selected service, preferred date, project description and related enquiry details so the studio can review and discuss your request.",
  },
  {
    title: "Uploaded files",
    body:
      "If you upload photos or PDFs, those files are used to understand your sewing request. Files are checked before acceptance and should only contain information that is relevant to the enquiry.",
  },
  {
    title: "Admin records and audit log",
    body:
      "The website admin area stores request statuses, messages, settings and audit log entries so the studio can manage enquiries safely and understand important administrator actions.",
  },
];

export function PrivacyPage() {
  const main = usePageContent("privacy").section("main-content");
  const { privacySections } = useRepeatableContent();
  const { brand, settings } = useSiteSettings();
  const contactText = settings.email ?? settings.phone ?? settings.contactIntroText;

  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="Legal" />
          <h1 className="font-serif text-[2.8rem] lg:text-[4rem] font-light text-foreground mt-4">
            {main?.title ?? "Privacy Policy"}
          </h1>
          <p className="text-[13px] text-muted-foreground mt-4 font-sans">
            {main?.subtitle ?? "Last updated: June 2026"}
          </p>
        </div>
      </div>
      <div className="py-16 bg-background">
        <div className="max-w-3xl mx-auto px-6 lg:px-10">
          <div className="border border-border bg-secondary/20 p-6 mb-12">
            <h2 className="font-serif text-[1.35rem] font-light text-foreground mb-3">
              How this website uses your information
            </h2>
            <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">
              {main?.body ??
                `${brand.brandDisplayName} collects only the information needed to respond to enquiries, review sewing requests, arrange consultations and manage the website administration area.`}
            </p>
            <p className="text-[12px] text-muted-foreground/70 leading-relaxed font-sans mt-4">
              This page is intended as a clear website privacy notice. The final
              wording should be reviewed by the business owner before public launch.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-12">
            {DATA_NOTICE_BLOCKS.map((block) => (
              <section key={block.title} className="border border-border bg-background p-5">
                <h2 className="font-serif text-[1.05rem] font-light text-foreground mb-3">
                  {block.title}
                </h2>
                <p className="text-[12px] text-muted-foreground leading-relaxed font-sans">
                  {block.body}
                </p>
              </section>
            ))}
          </div>

          <div className="space-y-10">
            {privacySections.map((section) => (
              <section key={section.title} className="border-t border-border pt-8">
                <h2 className="font-serif text-[1.1rem] font-light text-foreground mb-4">
                  {section.title}
                </h2>
                <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">
                  {section.body}
                </p>
              </section>
            ))}

            <section className="border-t border-border pt-8">
              <h2 className="font-serif text-[1.1rem] font-light text-foreground mb-4">
                Service terms
              </h2>
              <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">
                Practical information about sewing enquiries, order requests,
                consultations and uploaded files is available on the{" "}
                <Link to="/terms" className="text-foreground underline underline-offset-2 hover:text-accent transition-colors">
                  Terms &amp; Service Information
                </Link>{" "}
                page.
              </p>
            </section>

            {contactText ? (
              <section className="border-t border-border pt-8">
                <h2 className="font-serif text-[1.1rem] font-light text-foreground mb-4">
                  Contact about your data
                </h2>
                <p className="text-[13px] text-muted-foreground leading-relaxed font-sans">
                  To ask a privacy question or request access, correction or
                  deletion of information you submitted through the website, contact
                  the studio using the published contact details: {contactText}.
                </p>
              </section>
            ) : null}
          </div>
        </div>
      </div>
    </div>
  );
}
