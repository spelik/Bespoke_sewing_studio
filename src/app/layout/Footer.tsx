import { Mail, MapPin, Phone } from "lucide-react";
import { Link } from "react-router-dom";
import { CONTACT_DETAILS, NAV_LINKS, SITE_ASSETS } from "../appContent";
import { StitchDivider } from "../components/StitchDivider";
import { PAGE_PATHS } from "../routing/routes";

export function Footer() {
  return (
    <footer className="bg-foreground text-primary-foreground">
      <div className="max-w-7xl mx-auto px-6 lg:px-10 pt-16 pb-10">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-12 mb-12">
          <div className="lg:col-span-1">
            <div className="mb-5">
              <img
                src={SITE_ASSETS.headerLogo}
                alt="Oksana Logosha Logo"
                className="h-8 w-auto object-contain brightness-0 invert opacity-90"
              />
            </div>
            <p className="text-sm text-primary-foreground/55 leading-relaxed">
              Premium sewing, tailoring, dressmaking, alterations and handmade toys in Northern Ireland.
            </p>
            <div className="mt-6 flex items-center gap-3">
              <div className="w-8 h-8 border border-primary-foreground/20 flex items-center justify-center hover:border-accent transition-colors cursor-pointer">
                <span className="text-[10px] text-primary-foreground/50">ig</span>
              </div>
              <div className="w-8 h-8 border border-primary-foreground/20 flex items-center justify-center hover:border-accent transition-colors cursor-pointer">
                <span className="text-[10px] text-primary-foreground/50">fb</span>
              </div>
            </div>
          </div>

          <div>
            <h3 className="text-[10px] tracking-[0.25em] uppercase text-primary-foreground/35 mb-5">Navigate</h3>
            <ul className="space-y-2.5">
              {NAV_LINKS.map((link) => (
                <li key={link.page}>
                  <Link
                    to={PAGE_PATHS[link.page]}
                    className="text-sm text-primary-foreground/60 hover:text-primary-foreground transition-colors font-sans"
                  >
                    {link.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h3 className="text-[10px] tracking-[0.25em] uppercase text-primary-foreground/35 mb-5">Services</h3>
            <ul className="space-y-2.5 text-sm text-primary-foreground/60">
              <li>Luxury Custom Tailoring</li>
              <li>Dressmaking & Alterations</li>
              <li>Occasionwear & Bridal</li>
              <li>Handmade Toys</li>
              <li>Memory Bears</li>
              <li>Repairs & Restyling</li>
            </ul>
          </div>

          <div>
            <h3 className="text-[10px] tracking-[0.25em] uppercase text-primary-foreground/35 mb-5">Contact</h3>
            <ul className="space-y-3 text-sm text-primary-foreground/60">
              <li className="flex items-start gap-2.5">
                <MapPin size={13} className="mt-0.5 shrink-0 text-accent/70" />
                <span>{CONTACT_DETAILS.location}</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Phone size={13} className="text-accent/70" />
                <span>{CONTACT_DETAILS.phone}</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Mail size={13} className="text-accent/70" />
                <span>Please send an enquiry to discuss your order.</span>
              </li>
            </ul>
          </div>
        </div>

        <StitchDivider />

        <div className="mt-6 flex flex-col md:flex-row items-center justify-between gap-4 text-[11px] text-primary-foreground/30">
          <span>&copy; 2024 Bespoke Sewing Studio. All rights reserved.</span>
          <div className="flex items-center gap-6">
            <Link to="/privacy" className="hover:text-primary-foreground/60 transition-colors">
              Privacy Policy
            </Link>
            <Link to="/admin" className="hover:text-primary-foreground/60 transition-colors">
              Studio Login
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
}
