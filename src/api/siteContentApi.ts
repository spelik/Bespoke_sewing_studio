import { apiClient } from "./apiClient";
import { SERVICES } from "../data/servicesData";
import {
  CONTACT_DETAILS,
  CONTACT_PAGE_ITEMS,
  HOME_CONTACT_ITEMS,
  HOW_IT_WORKS,
  NAV_LINKS,
  PRIVACY_SECTIONS,
  SITE_ASSETS,
  SITE_SETTINGS,
  STUDIO_VALUES,
  TESTIMONIALS,
} from "../data/siteData";

export const getSiteSettings = () => apiClient.resolve(SITE_SETTINGS);
export const getSiteAssets = () => apiClient.resolve(SITE_ASSETS);
export const getContactInfo = () => apiClient.resolve(CONTACT_DETAILS);
export const getContactPageItems = () => apiClient.resolve(CONTACT_PAGE_ITEMS);
export const getNavigationItems = () => apiClient.resolve(NAV_LINKS);
export const getServices = () => apiClient.resolve(SERVICES);
export const getTestimonials = () => apiClient.resolve(TESTIMONIALS);
export const getProcessSteps = () => apiClient.resolve(HOW_IT_WORKS);
export const getStudioValues = () => apiClient.resolve(STUDIO_VALUES);
export const getHomeContactItems = () => apiClient.resolve(HOME_CONTACT_ITEMS);
export const getPrivacySections = () => apiClient.resolve(PRIVACY_SECTIONS);
