import {
  getNavigationItems,
  getPrivacySections,
  getProcessSteps,
  getSiteAssets,
  getSiteSettings,
  getStudioValues,
  getTestimonials,
} from "../api/siteContentApi";
import { VALUE_ICONS } from "./iconRegistry";

export const SITE_SETTINGS = getSiteSettings();
export const SITE_ASSETS = getSiteAssets();
export const NAV_LINKS = getNavigationItems();
export const TESTIMONIALS = getTestimonials();
export const HOW_IT_WORKS = getProcessSteps();
export const WHY_US = getStudioValues().map((value) => ({
  ...value,
  icon: VALUE_ICONS[value.icon],
}));
export const PRIVACY_SECTIONS = getPrivacySections();
