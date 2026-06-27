import { useEffect, useState } from "react";
import { Menu, X } from "lucide-react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { NAV_LINKS, SITE_ASSETS } from "../appContent";
import { PAGE_PATHS } from "../routing/routes";
import type { Language } from "../types";
import { MobileMenu } from "./MobileMenu";

interface HeaderProps {
  lang: Language;
  setLang: (language: Language) => void;
}

export function Header({ lang, setLang }: HeaderProps) {
  const [open, setOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const { pathname } = useLocation();

  useEffect(() => {
    const handler = () => setScrolled(window.scrollY > 50);
    window.addEventListener("scroll", handler, { passive: true });
    return () => window.removeEventListener("scroll", handler);
  }, []);

  useEffect(() => {
    setOpen(false);
  }, [pathname]);

  const isHero = pathname === "/" && !scrolled;
  const closeMenu = () => setOpen(false);

  return (
    <header
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-500 ${
        scrolled || open
          ? "bg-background/96 backdrop-blur-sm shadow-sm border-b border-border"
          : isHero
            ? "bg-transparent"
            : "bg-background/96 backdrop-blur-sm border-b border-border"
      }`}
    >
      <div className="max-w-7xl mx-auto px-6 lg:px-10">
        <div className="flex items-center justify-between h-[72px]">
          <Link to="/" onClick={closeMenu} className="flex flex-col items-start group">
            <img
              src={SITE_ASSETS.headerLogo}
              alt="Logosha Studio Logo"
              className="w-[130px] md:w-[160px] h-auto object-contain transition-opacity group-hover:opacity-80"
            />
          </Link>

          <nav className="hidden lg:flex items-center gap-7">
            {NAV_LINKS.map((link) => (
              <NavLink
                key={link.page}
                to={PAGE_PATHS[link.page]}
                end={link.page === "home"}
                className={({ isActive }) =>
                  `text-sm tracking-wide transition-colors font-sans ${
                    isActive
                      ? "text-foreground font-medium"
                      : "text-muted-foreground hover:text-foreground"
                  }`
                }
              >
                {({ isActive }) => (
                  <>
                    {link.label}
                    {isActive && <div className="h-px bg-accent mt-0.5 w-full" />}
                  </>
                )}
              </NavLink>
            ))}
          </nav>

          <div className="flex items-center gap-3">
            <div className="flex items-center gap-0.5 text-[11px] tracking-widest border border-border">
              <button
                onClick={() => setLang("en")}
                className={`px-2.5 py-1.5 transition-colors ${
                  lang === "en"
                    ? "bg-foreground text-primary-foreground"
                    : "text-muted-foreground hover:text-foreground"
                }`}
              >
                EN
              </button>
              <button
                onClick={() => setLang("uk")}
                className={`px-2.5 py-1.5 transition-colors ${
                  lang === "uk"
                    ? "bg-foreground text-primary-foreground"
                    : "text-muted-foreground hover:text-foreground"
                }`}
              >
                UA
              </button>
            </div>

            <Link
              to="/order"
              className="hidden lg:flex items-center gap-2 bg-foreground text-primary-foreground px-5 py-2.5 text-[13px] tracking-wide hover:bg-accent transition-colors"
            >
              Book Now
            </Link>

            <button
              className="lg:hidden p-1.5 text-foreground"
              onClick={() => setOpen((current) => !current)}
              aria-label="Toggle menu"
            >
              {open ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </div>
      </div>

      {open && <MobileMenu onNavigate={closeMenu} />}
    </header>
  );
}
