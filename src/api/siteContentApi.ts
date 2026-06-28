import { apiClient } from "./apiClient";
import {
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
export const getNavigationItems = () => apiClient.resolve(NAV_LINKS);
export const getTestimonials = () => apiClient.resolve(TESTIMONIALS);
export const getProcessSteps = () => apiClient.resolve(HOW_IT_WORKS);
export const getStudioValues = () => apiClient.resolve(STUDIO_VALUES);
export const getPrivacySections = () => apiClient.resolve(PRIVACY_SECTIONS);
