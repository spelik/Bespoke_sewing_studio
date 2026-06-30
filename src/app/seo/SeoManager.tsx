import { useEffect, useMemo } from "react";
import { useLocation } from "react-router-dom";
import { appConfig } from "../../config/appConfig";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

interface SeoDefinition {
  title: string;
  description: string;
  robots?: string;
}

interface SeoState {
  title: string;
  description: string;
  robots: string;
  canonicalPath: string;
  canonicalUrl: string;
  ogTitle: string;
  ogDescription: string;
  ogImageUrl: string | null;
  structuredData: Record<string, unknown> | null;
}

const PUBLIC_ROUTE_SEO: Record<string, SeoDefinition> = {
  "/": {
    title: "Bespoke Sewing Studio | Tailoring, Dressmaking & Alterations",
    description:
      "Bespoke Sewing Studio offers tailoring, dressmaking, alterations and memory bear services with a personal, made-to-measure approach.",
  },
  "/services": {
    title:
      "Tailoring, Dressmaking, Alterations & Memory Bears | Bespoke Sewing Studio",
    description:
      "Explore tailoring, dressmaking, alterations and memory bear services from Bespoke Sewing Studio.",
  },
  "/portfolio": {
    title: "Portfolio | Bespoke Sewing Studio",
    description:
      "View selected sewing, tailoring, dressmaking, alteration and memory bear work from Bespoke Sewing Studio.",
  },
  "/order": {
    title: "Request a Sewing Service | Bespoke Sewing Studio",
    description:
      "Send an order request for tailoring, dressmaking, alterations or memory bear work with Bespoke Sewing Studio.",
  },
  "/about": {
    title: "About Bespoke Sewing Studio",
    description:
      "Learn about Bespoke Sewing Studio and its careful, personal approach to sewing, tailoring, dressmaking and alterations.",
  },
  "/contact": {
    title: "Contact Bespoke Sewing Studio",
    description:
      "Contact Bespoke Sewing Studio to discuss sewing, tailoring, dressmaking, alterations or memory bear enquiries.",
  },
  "/privacy": {
    title: "Privacy Policy | Bespoke Sewing Studio",
    description:
      "Read how Bespoke Sewing Studio handles personal information submitted through contact and order request forms.",
  },
  "/terms": {
    title: "Terms & Service Information | Bespoke Sewing Studio",
    description:
      "Read practical service information for sewing enquiries, order requests, consultations and uploaded files.",
  },
};

const ADMIN_ROBOTS = "noindex, nofollow";
const PUBLIC_ROBOTS = "index, follow";

export function SeoManager() {
  const location = useLocation();
  const { brand, settings } = useSiteSettings();

  const seo = useMemo<SeoState>(() => {
    const pathname = normalizePath(location.pathname);

    if (pathname.startsWith("/admin")) {
      const title = "Admin | Bespoke Sewing Studio";
      const description = "Bespoke Sewing Studio administration area.";

      return {
        title,
        description,
        robots: ADMIN_ROBOTS,
        canonicalPath: pathname,
        canonicalUrl: buildAbsoluteUrl(pathname),
        ogTitle: title,
        ogDescription: description,
        ogImageUrl: null,
        structuredData: null,
      };
    }

    const route = PUBLIC_ROUTE_SEO[pathname] ?? {
      title: "Page not found | Bespoke Sewing Studio",
      description: "The requested page could not be found.",
      robots: ADMIN_ROBOTS,
    };

    const isHome = pathname === "/";
    const title = isHome
      ? brand.defaultMetaTitle || route.title
      : route.title;
    const description = isHome
      ? brand.defaultMetaDescription || route.description
      : route.description;
    const canonicalUrl = buildAbsoluteUrl(pathname);
    const imageUrl = toAbsoluteUrl(
      brand.defaultOgImageUrl ?? brand.logoUrl,
    );
    const structuredData = isHome
      ? createBusinessStructuredData({
          description,
          imageUrl,
          logoUrl: toAbsoluteUrl(brand.logoUrl),
          name: brand.brandDisplayName || settings.studioName,
          phone: settings.phone,
          sameAs: [
            settings.facebookUrl,
            settings.instagramUrl,
            settings.tikTokUrl,
            settings.pinterestUrl,
          ],
        })
      : null;

    return {
      title,
      description,
      robots: route.robots ?? PUBLIC_ROBOTS,
      canonicalPath: pathname,
      canonicalUrl,
      ogTitle: isHome ? brand.defaultOgTitle ?? title : title,
      ogDescription: isHome
        ? brand.defaultOgDescription ?? description
        : description,
      ogImageUrl: imageUrl,
      structuredData,
    };
  }, [brand, location.pathname, settings]);

  useEffect(() => {
    document.title = seo.title;
    setNamedMeta("description", seo.description);
    setNamedMeta("robots", seo.robots);
    setPropertyMeta("og:title", seo.ogTitle);
    setPropertyMeta("og:description", seo.ogDescription);
    setPropertyMeta("og:type", "website");
    setPropertyMeta("og:site_name", brand.brandDisplayName || settings.studioName);
    setPropertyMeta("og:url", seo.canonicalUrl);
    setPropertyMeta("og:image", seo.ogImageUrl ?? null);
    setNamedMeta("twitter:card", seo.ogImageUrl ? "summary_large_image" : "summary");
    setNamedMeta("twitter:title", seo.ogTitle);
    setNamedMeta("twitter:description", seo.ogDescription);
    setNamedMeta("twitter:image", seo.ogImageUrl ?? null);
    setCanonicalLink(seo.canonicalUrl);
    setFavicon(toAbsoluteUrl(brand.faviconUrl));
    setJsonLd("business-jsonld", seo.structuredData);
  }, [brand, seo, settings]);

  return null;
}

function normalizePath(pathname: string): string {
  if (!pathname || pathname === "/") {
    return "/";
  }

  return pathname.replace(/\/+$/, "") || "/";
}

function getPublicOrigin(): string {
  if (appConfig.publicSiteUrl) {
    return appConfig.publicSiteUrl.replace(/\/+$/, "");
  }

  if (typeof window !== "undefined" && window.location.origin) {
    return window.location.origin;
  }

  return "";
}

function buildAbsoluteUrl(pathname: string): string {
  const origin = getPublicOrigin();
  const path = pathname.startsWith("/") ? pathname : `/${pathname}`;
  return origin ? `${origin}${path}` : path;
}

function toAbsoluteUrl(url: string | null): string | null {
  if (!url) {
    return null;
  }

  try {
    return new URL(url, getPublicOrigin() || window.location.origin).toString();
  } catch {
    return null;
  }
}

function setNamedMeta(name: string, content: string | null): void {
  const existing = document.querySelector<HTMLMetaElement>(
    `meta[name="${name}"]`,
  );

  if (!content) {
    existing?.remove();
    return;
  }

  const meta = existing ?? document.createElement("meta");
  meta.name = name;
  meta.content = content;

  if (!meta.parentNode) {
    document.head.append(meta);
  }
}

function setPropertyMeta(property: string, content: string | null): void {
  const existing = document.querySelector<HTMLMetaElement>(
    `meta[property="${property}"]`,
  );

  if (!content) {
    existing?.remove();
    return;
  }

  const meta = existing ?? document.createElement("meta");
  meta.setAttribute("property", property);
  meta.content = content;

  if (!meta.parentNode) {
    document.head.append(meta);
  }
}

function setCanonicalLink(href: string): void {
  const existing = document.querySelector<HTMLLinkElement>(
    'link[rel="canonical"]',
  );
  const link = existing ?? document.createElement("link");
  link.rel = "canonical";
  link.href = href;

  if (!link.parentNode) {
    document.head.append(link);
  }
}

function setFavicon(href: string | null): void {
  const existing = document.querySelector<HTMLLinkElement>('link[rel~="icon"]');

  if (!href) {
    existing?.remove();
    return;
  }

  const icon = existing ?? document.createElement("link");
  icon.rel = "icon";
  icon.href = href;

  if (!icon.parentNode) {
    document.head.append(icon);
  }
}

function setJsonLd(id: string, value: Record<string, unknown> | null): void {
  const existing = document.getElementById(id) as HTMLScriptElement | null;

  if (!value) {
    existing?.remove();
    return;
  }

  const script = existing ?? document.createElement("script");
  script.id = id;
  script.type = "application/ld+json";
  script.textContent = JSON.stringify(value);

  if (!script.parentNode) {
    document.head.append(script);
  }
}

function createBusinessStructuredData({
  description,
  imageUrl,
  logoUrl,
  name,
  phone,
  sameAs,
}: {
  description: string;
  imageUrl: string | null;
  logoUrl: string | null;
  name: string;
  phone: string | null;
  sameAs: Array<string | null>;
}): Record<string, unknown> {
  return removeEmpty({
    "@context": "https://schema.org",
    "@type": ["LocalBusiness", "ProfessionalService"],
    "@id": `${buildAbsoluteUrl("/")}#business`,
    name,
    url: buildAbsoluteUrl("/"),
    telephone: phone,
    description,
    image: imageUrl,
    logo: logoUrl,
    serviceType: [
      "Tailoring",
      "Dressmaking",
      "Alterations",
      "Memory Bears",
    ],
    sameAs: sameAs.filter((url): url is string => Boolean(url)),
  });
}

function removeEmpty(value: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(value).filter(([, entry]) => {
      if (entry === null || entry === undefined || entry === "") {
        return false;
      }

      if (Array.isArray(entry) && entry.length === 0) {
        return false;
      }

      return true;
    }),
  );
}
