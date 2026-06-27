import { ArrowLeft, MailWarning } from "lucide-react";
import { Link } from "react-router-dom";
import { SectionLabel } from "../components/SectionLabel";
import { PAGE_PATHS } from "../routing/routes";

export function NotFoundPage() {
  return (
    <div className="pt-[72px] min-h-screen bg-background">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="404" />
          <h1 className="font-serif text-[3rem] lg:text-[5rem] font-light text-foreground mt-4 leading-tight">
            Page
            <br />
            <em className="italic text-accent">not found.</em>
          </h1>
        </div>
      </div>

      <section className="py-20 px-6 lg:px-10">
        <div className="max-w-3xl mx-auto border border-border bg-card p-8 lg:p-12 text-center">
          <div className="w-14 h-14 border border-accent/30 flex items-center justify-center mx-auto mb-6">
            <MailWarning size={22} className="text-accent" />
          </div>
          <h2 className="font-serif text-[2rem] font-light text-foreground mb-4">
            We couldn&apos;t find that page
          </h2>
          <p className="text-[13px] text-muted-foreground leading-relaxed max-w-xl mx-auto font-sans">
            The page may have been moved, removed, or the address may be incorrect. You can return to the home page or contact the studio for help.
          </p>

          <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link
              to={PAGE_PATHS.home}
              className="inline-flex items-center justify-center gap-2.5 bg-foreground text-primary-foreground px-8 py-3.5 text-[13px] tracking-wide hover:bg-accent transition-colors font-sans"
            >
              <ArrowLeft size={14} />
              Back to Home
            </Link>
            <Link
              to={PAGE_PATHS.contact}
              className="inline-flex items-center justify-center border border-foreground text-foreground px-8 py-3.5 text-[13px] tracking-wide hover:bg-foreground hover:text-primary-foreground transition-colors font-sans"
            >
              Contact
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
