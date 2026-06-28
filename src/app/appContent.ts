import { getPortfolioCategories, getPortfolioItems } from "../api/portfolioApi";
import {
  getContactInfo,
  getContactPageItems,
  getHomeContactItems,
  getNavigationItems,
  getPrivacySections,
  getProcessSteps,
  getSiteAssets,
  getSiteSettings,
  getStudioValues,
  getTestimonials,
} from "../api/siteContentApi";
import {
  ADMIN_ORDERS,
  ADMIN_STATS as ADMIN_STATS_DATA,
  MONTHLY_DATA,
  SERVICE_BREAKDOWN,
  STATUS_COLORS,
} from "../data/adminDemoData";
import {
  ADMIN_STAT_ICONS,
  CONTACT_ICONS,
  VALUE_ICONS,
} from "./iconRegistry";

export const SITE_SETTINGS = getSiteSettings();
export const SITE_ASSETS = getSiteAssets();
export const CONTACT_DETAILS = getContactInfo();
export const NAV_LINKS = getNavigationItems();
export const TESTIMONIALS = getTestimonials();
export const PORTFOLIO_ITEMS = getPortfolioItems();
export const PORTFOLIO_CATEGORIES = getPortfolioCategories();
export const HOW_IT_WORKS = getProcessSteps();
export const WHY_US = getStudioValues().map((value) => ({
  ...value,
  icon: VALUE_ICONS[value.icon],
}));
export const HOME_CONTACT_ITEMS = getHomeContactItems().map((item) => ({
  ...item,
  kind: item.icon,
  icon: CONTACT_ICONS[item.icon],
}));
export const CONTACT_PAGE_ITEMS = getContactPageItems().map((item) => ({
  ...item,
  kind: item.icon,
  icon: CONTACT_ICONS[item.icon],
}));
export const PRIVACY_SECTIONS = getPrivacySections();
export const ADMIN_STATS = ADMIN_STATS_DATA.map((stat) => ({
  ...stat,
  icon: ADMIN_STAT_ICONS[stat.icon],
}));

export {
  ADMIN_ORDERS,
  MONTHLY_DATA,
  SERVICE_BREAKDOWN,
  STATUS_COLORS,
};
