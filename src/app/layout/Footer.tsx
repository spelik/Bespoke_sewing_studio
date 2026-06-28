import { Mail, MapPin, Phone } from "lucide-react";
import { Link } from "react-router-dom";
import { SITE_ASSETS } from "../appContent";
import { StitchDivider } from "../components/StitchDivider";
import { PAGE_PATHS } from "../routing/routes";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";
import { getBrandNavigation } from "../siteSettings/brandNavigation";
import { useServices } from "../services/ServicesContext";

export function Footer() {
  const { settings, brand } = useSiteSettings();
  const { services } = useServices();
  const navigation=getBrandNavigation(brand.navigation);
  const socialLinks = [
    { label: "Instagram", shortLabel: "ig", url: settings.instagramUrl },
    { label: "Facebook", shortLabel: "fb", url: settings.facebookUrl },
    { label: "TikTok", shortLabel: "tt", url: settings.tikTokUrl },
    { label: "Pinterest", shortLabel: "pt", url: settings.pinterestUrl },
  ];
  const publicContactText = settings.email ?? settings.contactIntroText;

  return (
    <footer className="bg-foreground text-primary-foreground">
      <div className="max-w-7xl mx-auto px-6 lg:px-10 pt-16 pb-10">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-12 mb-12">
          <div className="lg:col-span-1">
            <div className="mb-5">
              <img
                src={brand.logoUrl ?? SITE_ASSETS.headerLogo}
                alt={brand.logoAltText}
                className="h-8 w-auto object-contain brightness-0 invert opacity-90"
              />
            </div>
            <p className="text-sm text-primary-foreground/55 leading-relaxed">
              {settings.siteTagline}
            </p>
            <div className="mt-6 flex items-center gap-3">
              {socialLinks
                .filter((social) => social.url)
                .map((social) =>
                social.url ? (
                  <a
                    key={social.label}
                    href={social.url}
                    target="_blank"
                    rel="noreferrer"
                    aria-label={social.label}
                    className="w-8 h-8 border border-primary-foreground/20 flex items-center justify-center hover:border-accent transition-colors"
                  >
                    <span className="text-[10px] text-primary-foreground/50">{social.shortLabel}</span>
                  </a>
                ) : null,
              )}
            </div>
          </div>

          <div>
            <h3 className="text-[10px] tracking-[0.25em] uppercase text-primary-foreground/35 mb-5">Navigate</h3>
            <ul className="space-y-2.5">
              {navigation.map((link) => (
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
              {services.map((service) => <li key={service.id ?? service.slug}>{service.name}</li>)}
            </ul>
          </div>

          <div>
            <h3 className="text-[10px] tracking-[0.25em] uppercase text-primary-foreground/35 mb-5">Contact</h3>
            <ul className="space-y-3 text-sm text-primary-foreground/60">
              {settings.serviceAreaText ? (
                <li className="flex items-start gap-2.5">
                <MapPin size={13} className="mt-0.5 shrink-0 text-accent/70" />
                  <span>{settings.serviceAreaText}</span>
                </li>
              ) : null}
              {settings.phone ? (
                <li className="flex items-center gap-2.5">
                <Phone size={13} className="text-accent/70" />
                  <span>{settings.phone}</span>
                </li>
              ) : null}
              {publicContactText ? (
                <li className="flex items-center gap-2.5">
                <Mail size={13} className="text-accent/70" />
                  <span>{publicContactText}</span>
                </li>
              ) : null}
            </ul>
          </div>
        </div>

        <StitchDivider />

        <div className="mt-6 flex flex-col md:flex-row items-center justify-between gap-4 text-[11px] text-primary-foreground/30">
          <span>
            &copy; 2024 {settings.footerText ?? brand.brandDisplayName}
          </span>
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
