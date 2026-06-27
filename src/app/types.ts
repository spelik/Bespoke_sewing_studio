export type Page =
  | "home"
  | "services"
  | "portfolio"
  | "order"
  | "about"
  | "contact"
  | "privacy"
  | "admin";

export type Language = "en" | "uk";

export type OrderServiceType =
  | "Tailoring"
  | "Dressmaking"
  | "Alterations"
  | "Memory Bears";

export type OrderApiServiceType =
  | "Tailoring"
  | "Dressmaking"
  | "Alterations"
  | "MemoryBear"
  | "Other";

export type ServiceIconKey = "scissors" | "sparkles" | "refresh" | "package";
export type ValueIconKey = "award" | "heart" | "shield" | "check";
export type ContactIconKey = "location" | "phone" | "email" | "hours";
export type AdminStatIconKey = "orders" | "active" | "clients" | "revenue";

export type PortfolioCategory = OrderServiceType;
export type PortfolioFilter = "all" | PortfolioCategory;

export interface NavigationItem {
  label: string;
  page: Page;
}

export interface ContactInfo {
  phone: string;
  location: string;
  enquiryNote: string;
}

export interface SiteSettings {
  brandName: string;
  defaultLanguage: Language;
  contact: ContactInfo;
}

export interface ResponsiveImageAsset {
  src: string;
  srcSet?: string;
  webpSrc?: string;
  webpSrcSet?: string;
  sizes?: string;
}

export interface SiteAssets {
  headerLogo: string;
  homeHero: ResponsiveImageAsset;
  aboutHero: ResponsiveImageAsset;
}

export interface ServiceItem {
  icon: ServiceIconKey;
  title: OrderServiceType;
  desc: string;
  price: string;
  detail: string;
}

export interface PortfolioItem {
  id: number;
  title: string;
  category: PortfolioCategory;
  image: ResponsiveImageAsset;
  size: "normal" | "tall";
}

export interface Testimonial {
  name: string;
  location: string;
  rating: number;
  text: string;
  service: string;
}

export interface ProcessStep {
  step: string;
  title: string;
  desc: string;
}

export interface StudioValue {
  icon: ValueIconKey;
  title: string;
  desc: string;
}

export interface ContactDisplayItem {
  icon: ContactIconKey;
  text: string;
}

export interface PrivacySection {
  title: string;
  body: string;
}

export interface MemoryBearPrice {
  label: string;
  price: string;
}

export interface OrderRequest {
  fullName: string;
  email: string;
  phone?: string;
  service: OrderServiceType;
  description: string;
  preferredDate?: string;
  consent: boolean;
  attachments: File[];
}

export interface CreateOrderApiRequest {
  fullName: string;
  email: string | null;
  phone: string | null;
  serviceType: OrderApiServiceType;
  description: string;
  preferredDate: string | null;
  consent: boolean;
  attachmentIds: string[] | null;
}

export interface OrderSubmissionResponse {
  id: string;
  status: "New";
  createdAt: string;
}
