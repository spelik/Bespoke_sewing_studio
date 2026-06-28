import type {
  NavigationItem,
  PrivacySection,
  ProcessStep,
  PublicSiteSettings,
  PublicBrandSettings,
  SiteAssets,
  SiteSettings,
  StudioValue,
  Testimonial,
} from "../app/types";
import { ABOUT_HERO_IMAGE, HEADER_LOGO, HOME_HERO_IMAGE } from "./imageAssets";

export const SITE_SETTINGS: SiteSettings = {
  brandName: "Bespoke Sewing Studio",
};

export const PUBLIC_SITE_SETTINGS_FALLBACK: PublicSiteSettings = {
  studioName: SITE_SETTINGS.brandName,
  siteTagline: "Premium sewing, tailoring, dressmaking, alterations and memory bears.",
  email: null,
  phone: "074 6734 7194",
  contactButtonLabel: "Send Enquiry",
  contactIntroText: "Consultations and orders are arranged individually.",
  facebookUrl: null,
  instagramUrl: null,
  tikTokUrl: null,
  pinterestUrl: null,
  footerText: "Bespoke Sewing Studio. All rights reserved.",
  serviceAreaText: "Appointments arranged individually.",
};

export const PUBLIC_BRAND_SETTINGS_FALLBACK: PublicBrandSettings = {
  brandDisplayName: SITE_SETTINGS.brandName,
  logoUrl: null, logoAltText: "Bespoke Sewing Studio logo", faviconUrl: null,
  headerCtaLabel: "Book Now", headerCtaUrl: "/order",
  defaultMetaTitle: "Bespoke Sewing Studio",
  defaultMetaDescription: "Bespoke sewing, tailoring, dressmaking, alterations and memory bears.",
  defaultOgTitle: null, defaultOgDescription: null, defaultOgImageUrl: null,
  navigation: {
    showServicesLink: true, servicesLabel: "Services", showPortfolioLink: true, portfolioLabel: "Portfolio",
    showOrderLink: true, orderLabel: "Order", showAboutLink: true, aboutLabel: "About",
    showContactLink: true, contactLabel: "Contact",
  },
};

export const SITE_ASSETS: SiteAssets = {
  headerLogo: HEADER_LOGO,
  homeHero: HOME_HERO_IMAGE,
  aboutHero: ABOUT_HERO_IMAGE,
};

export const NAV_LINKS: ReadonlyArray<NavigationItem> = [
  { label: "Home", page: "home" },
  { label: "Services", page: "services" },
  { label: "Portfolio", page: "portfolio" },
  { label: "Order", page: "order" },
  { label: "About", page: "about" },
  { label: "Contact", page: "contact" },
];

export const TESTIMONIALS: ReadonlyArray<Testimonial> = [
  {
    name: "Catherine O'Neill",
    location: "Northern Ireland",
    rating: 5,
    text: "The alterations on my dress were absolutely perfect. Every detail was handled with such care and skill. I could not have asked for more — truly a magical experience.",
    service: "Occasionwear & Bridal Adjustments",
  },
  {
    name: "Margaret Doherty",
    location: "Northern Ireland",
    rating: 5,
    text: "I ordered a memory bear for my granddaughter and was moved to tears by the result. It is beautifully made. Truly gifted hands at work here.",
    service: "Memory Bears",
  },
  {
    name: "Siobhán McBride",
    location: "Northern Ireland",
    rating: 5,
    text: "My bespoke dress for my daughter's graduation was made to perfection. The attention to detail, the quality of fabric, the care taken — outstanding from the very first consultation.",
    service: "Luxury Custom Tailoring",
  },
  {
    name: "Fionnuala Walsh",
    location: "Northern Ireland",
    rating: 5,
    text: "Professional, warm, and the results are always impeccable. A true gem for anyone seeking quality tailoring and alterations.",
    service: "Dressmaking & Alterations",
  },
];

export const HOW_IT_WORKS: ReadonlyArray<ProcessStep> = [
  { step: "01", title: "Initial Consultation", desc: "Send an enquiry to discuss your requirements, garments, and vision." },
  { step: "02", title: "Measurements & Design", desc: "We take precise measurements and discuss fabric, style, and your preferred timeline." },
  { step: "03", title: "Expert Craftsmanship", desc: "Your piece is crafted or altered with meticulous attention to every detail." },
  { step: "04", title: "Final Fitting", desc: "A final fitting ensures everything is perfect before collection or delivery." },
];

export const STUDIO_VALUES: ReadonlyArray<StudioValue> = [
  { icon: "award", title: "Crafted with Care", desc: "Every piece is carefully made to ensure the highest quality finish." },
  { icon: "heart", title: "Passion for Craft", desc: "Every stitch placed with care, pride, and genuine love for the art of sewing." },
  { icon: "shield", title: "Quality Guarantee", desc: "We stand behind every piece. Your complete satisfaction is our promise." },
  { icon: "check", title: "Attention to Detail", desc: "We focus on the small details that make a piece truly special and bespoke." },
];

export const PRIVACY_SECTIONS: ReadonlyArray<PrivacySection> = [
  {
    title: "1. Information We Collect",
    body: "We collect information you provide directly when you place an order request, contact us, or use our services. This may include your name, email address, telephone number, and details about your garment or order. We may also collect photographs of garments you upload through our order request form.",
  },
  {
    title: "2. How We Use Your Information",
    body: "We use the information we collect to process your order requests, communicate with you about your order, provide customer service, and improve our services. We will not sell, trade, or rent your personal information to third parties under any circumstances.",
  },
  {
    title: "3. Data Storage and Security",
    body: "Your personal data is stored securely and we take appropriate technical and organisational measures to protect it against unauthorised access, loss, or alteration. We retain your data only for as long as necessary to fulfil our services or as required by applicable law.",
  },
  {
    title: "4. Your Rights",
    body: "Under the UK General Data Protection Regulation (UK GDPR) and the Data Protection Act 2018, you have the right to access, correct, or delete your personal data. You may also object to the processing of your data or request that we restrict its use. To exercise any of these rights, please contact us by phone or enquiry.",
  },
  {
    title: "5. Cookies",
    body: "Our website uses only essential cookies necessary for its operation. We do not use tracking, advertising, or analytics cookies without your explicit consent. You can disable cookies in your browser settings at any time, though this may affect certain functionality.",
  },
  {
    title: "6. Contact",
    body: "Bespoke Sewing Studio is the data controller for your personal information. If you have questions about this Privacy Policy or wish to exercise your data rights, please contact us by phone or enquiry.",
  },
];
