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

export interface PublicSiteSettings {
  studioName: string;
  siteTagline: string | null;
  email: string | null;
  phone: string | null;
  contactButtonLabel: string | null;
  contactIntroText: string | null;
  facebookUrl: string | null;
  instagramUrl: string | null;
  tikTokUrl: string | null;
  pinterestUrl: string | null;
  footerText: string | null;
  serviceAreaText: string | null;
}

export interface AdminSiteSettings extends PublicSiteSettings {
  id: string;
  emailNotificationsEnabled: boolean;
  businessLegalName: string | null;
  updatedAt: string;
}

export interface EmailNotificationResult {
  success: boolean;
  provider: "Logging" | "Smtp" | "LoggingFallback" | string;
  sentExternally: boolean;
  message: string;
}

export type UpdateSiteSettingsRequest = Omit<
  AdminSiteSettings,
  "id" | "updatedAt"
>;

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
  id: string | null;
  slug: string;
  name: string;
  shortDescription: string;
  description: string | null;
  category: string | null;
  isFeatured: boolean;
  displayOrder: number;
  priceOptions: ServicePriceOption[];
  imageUrl: string | null;
  legacyServiceType?: OrderServiceType;
}

export interface ServicePriceOption {
  id: string | null;
  label: string;
  description: string | null;
  priceText: string;
  displayOrder: number;
  isActive: boolean;
}

export type PublicServiceOffering = ServiceItem;

export interface AdminServiceOffering extends Omit<ServiceItem, "legacyServiceType"> {
  id: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  archivedAt: string | null;
  usageCount: number;
  canDelete: boolean;
}

export interface ServicePriceOptionRequest {
  label: string;
  description: string | null;
  priceText: string;
  displayOrder: number;
  isActive: boolean;
}

export interface SaveServiceOfferingRequest {
  slug: string | null;
  name: string;
  shortDescription: string;
  description: string | null;
  category: string | null;
  isActive: boolean;
  isFeatured: boolean;
  displayOrder: number;
  priceOptions: ServicePriceOptionRequest[];
  imageUrl: string | null;
}

export interface DeleteServiceOfferingResult {
  id: string;
  deleted: boolean;
  archived: boolean;
  message: string;
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

export interface OrderRequest {
  fullName: string;
  email: string;
  phone?: string;
  serviceOfferingId?: string;
  serviceSlug?: string;
  legacyServiceType?: OrderServiceType;
  description: string;
  preferredDate?: string;
  consent: boolean;
  attachments: File[];
}

export interface CreateOrderApiRequest {
  fullName: string;
  email: string | null;
  phone: string | null;
  serviceType: OrderApiServiceType | null;
  serviceOfferingId: string | null;
  serviceSlug: string | null;
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
