import { NAV_LINKS } from "../appContent";
import type { BrandNavigationSettings, NavigationItem } from "../types";

export function getBrandNavigation(n:BrandNavigationSettings):NavigationItem[] {
  return NAV_LINKS.flatMap(link=>{
    if(link.page==="home") return [link];
    const values:Partial<Record<NavigationItem["page"],{show:boolean;label:string}>>={services:{show:n.showServicesLink,label:n.servicesLabel},portfolio:{show:n.showPortfolioLink,label:n.portfolioLabel},order:{show:n.showOrderLink,label:n.orderLabel},about:{show:n.showAboutLink,label:n.aboutLabel},contact:{show:n.showContactLink,label:n.contactLabel}};
    const value=values[link.page]; return !value||!value.show?[]:[{...link,label:value.label}];
  });
}
