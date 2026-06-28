export type Page =
  | "home"
  | "services"
  | "portfolio"
  | "order"
  | "about"
  | "contact"
  | "privacy"
  | "admin";

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

export type PortfolioFilter = "all" | string;

export interface NavigationItem {
  label: string;
  page: Page;
}

export interface SiteSettings {
  brandName: string;
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

export interface BrandNavigationSettings {
  showServicesLink: boolean; servicesLabel: string;
  showPortfolioLink: boolean; portfolioLabel: string;
  showOrderLink: boolean; orderLabel: string;
  showAboutLink: boolean; aboutLabel: string;
  showContactLink: boolean; contactLabel: string;
}
export interface PublicBrandSettings {
  brandDisplayName: string; logoUrl: string | null; logoAltText: string; faviconUrl: string | null;
  headerCtaLabel: string; headerCtaUrl: string; defaultMetaTitle: string; defaultMetaDescription: string;
  defaultOgTitle: string | null; defaultOgDescription: string | null; defaultOgImageUrl: string | null;
  navigation: BrandNavigationSettings;
}
export interface AdminBrandSettings extends PublicBrandSettings {
  logoFileId: string | null; faviconFileId: string | null; defaultOgImageFileId: string | null; updatedAt: string;
}
export interface UpdateBrandSettingsRequest {
  brandDisplayName: string; logoFileId: string | null; logoAltText: string; faviconFileId: string | null;
  headerCtaLabel: string; headerCtaUrl: string; defaultMetaTitle: string; defaultMetaDescription: string;
  defaultOgTitle: string | null; defaultOgDescription: string | null; defaultOgImageFileId: string | null;
  showServicesLink: boolean; servicesLabel: string; showPortfolioLink: boolean; portfolioLabel: string;
  showOrderLink: boolean; orderLabel: string; showAboutLink: boolean; aboutLabel: string;
  showContactLink: boolean; contactLabel: string;
}
export interface UploadedBrandImage { id: string; originalFileName: string; contentType: string; sizeBytes: number; purpose: "BrandAsset"; createdAt: string; }

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

export interface PublicPortfolioCategory {
  id: string;
  slug: string;
  name: string;
  description: string | null;
  displayOrder: number;
}

export interface PortfolioItem {
  id: string;
  slug: string | null;
  title: string;
  shortDescription: string | null;
  description: string | null;
  category: PublicPortfolioCategory;
  imageUrl: string;
  imageAsset?: ResponsiveImageAsset;
  altText: string;
  isFeatured: boolean;
  displayOrder: number;
}

export interface AdminPortfolioCategory extends PublicPortfolioCategory {
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  archivedAt: string | null;
  itemCount: number;
}

export interface AdminPortfolioItem extends Omit<PortfolioItem, "category" | "imageAsset" | "imageUrl"> {
  categoryId: string;
  categoryName: string;
  imageFileId: string | null;
  imageUrl: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  archivedAt: string | null;
}

export interface SavePortfolioCategoryRequest {
  slug: string | null;
  name: string;
  description: string | null;
  isActive: boolean;
  displayOrder: number;
}

export interface SavePortfolioItemRequest {
  categoryId: string;
  slug: string | null;
  title: string;
  shortDescription: string | null;
  description: string | null;
  imageFileId: string | null;
  altText: string | null;
  isActive: boolean;
  isFeatured: boolean;
  displayOrder: number;
}

export interface DeletePortfolioResult {
  id: string;
  deleted: boolean;
  archived: boolean;
  message: string;
}

export interface UploadedPortfolioImage {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  purpose: "PortfolioImage";
  createdAt: string;
}

export interface PageContentSection {
  id: string;
  sectionKey: string;
  title: string | null;
  subtitle: string | null;
  body: string | null;
  ctaLabel: string | null;
  ctaUrl: string | null;
  imageUrl: string | null;
  imageAltText: string | null;
  displayOrder: number;
}
export interface PublicPageContent { pageKey: string; sections: PageContentSection[]; }
export interface AdminPageContent extends PageContentSection {
  pageKey: string; imageFileId: string | null; isActive: boolean; updatedAt: string; archivedAt: string | null;
}
export interface SavePageContentRequest {
  pageKey: string; sectionKey: string; title: string | null; subtitle: string | null; body: string | null;
  ctaLabel: string | null; ctaUrl: string | null; imageFileId: string | null; imageAltText: string | null;
  displayOrder: number; isActive: boolean;
}
export interface UploadedContentImage { id: string; originalFileName: string; contentType: string; sizeBytes: number; purpose: "SiteAsset"; createdAt: string; }
export interface DeletePageContentResult { id: string; archived: boolean; message: string; }

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
