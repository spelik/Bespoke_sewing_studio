import { NavLink } from "react-router-dom";
import { PAGE_PATHS } from "../routing/routes";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";
import { getBrandNavigation } from "../siteSettings/brandNavigation";

export function MobileMenu({ onNavigate }: { onNavigate: () => void }) {
  const {brand}=useSiteSettings(); const navigation=getBrandNavigation(brand.navigation);
  return (
    <div className="lg:hidden bg-background border-t border-border">
      <nav className="max-w-7xl mx-auto px-6 py-6 flex flex-col gap-1">
        {navigation.map((link) => (
          <NavLink
            key={link.page}
            to={PAGE_PATHS[link.page]}
            end={link.page === "home"}
            onClick={onNavigate}
            className={({ isActive }) =>
              `text-left text-base py-3 border-b border-border/30 transition-colors font-sans ${
                isActive ? "text-foreground font-medium" : "text-muted-foreground"
              }`
            }
          >
            {link.label}
          </NavLink>
        ))}
        {brand.headerCtaUrl.startsWith("/") ? <NavLink
          to={brand.headerCtaUrl}
          onClick={onNavigate}
          className="mt-4 bg-foreground text-primary-foreground px-5 py-3.5 text-sm tracking-wide text-center"
        >
          {brand.headerCtaLabel}
        </NavLink> : <a href={brand.headerCtaUrl} onClick={onNavigate} className="mt-4 bg-foreground text-primary-foreground px-5 py-3.5 text-sm tracking-wide text-center">{brand.headerCtaLabel}</a>}
      </nav>
    </div>
  );
}
