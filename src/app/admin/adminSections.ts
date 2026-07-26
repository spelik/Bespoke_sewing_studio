import {
  Activity,
  Building2,
  FileText,
  HardDrive,
  History,
  Images,
  LayoutDashboard,
  ListOrdered,
  Mail,
  Package,
  Palette,
  Scissors,
  Settings,
  Shirt,
  UserCircle,
  Users,
  type LucideIcon,
} from "lucide-react";

export type AdminSection =
  | "dashboard"
  | "orders"
  | "contactMessages"
  | "services"
  | "inStock"
  | "portfolio"
  | "content"
  | "repeatable"
  | "businessInfo"
  | "brand"
  | "users"
  | "account"
  | "auditLog"
  | "emailLog"
  | "storage"
  | "productionHealth"
  | "systemSettings";

export interface AdminNavItem {
  id: AdminSection;
  label: string;
  title: string;
  description: string;
  icon: LucideIcon;
}

export interface AdminNavGroup {
  label: string;
  items: ReadonlyArray<AdminNavItem>;
}

export const ADMIN_NAV_GROUPS: ReadonlyArray<AdminNavGroup> = [
  {
    label: "Work",
    items: [
      {
        id: "dashboard",
        label: "Dashboard",
        title: "Dashboard",
        description:
          "Overview of recent customer activity and items that need attention.",
        icon: LayoutDashboard,
      },
      {
        id: "orders",
        label: "Orders",
        title: "Orders",
        description:
          "Manage customer order requests, attached garment photos and order status.",
        icon: Package,
      },
      {
        id: "contactMessages",
        label: "Messages",
        title: "Contact Messages",
        description: "Review enquiries submitted from the contact page.",
        icon: Mail,
      },
      {
        id: "services",
        label: "Services & Prices",
        title: "Services & Prices",
        description:
          "Control which services appear on the public site and order form.",
        icon: Scissors,
      },
      {
        id: "inStock",
        label: "IN STOCK",
        title: "IN STOCK",
        description:
          "Manage finished pieces available to buy, with photos, price and availability.",
        icon: Shirt,
      },
      {
        id: "portfolio",
        label: "Portfolio",
        title: "Portfolio",
        description:
          "Add, edit and organise public portfolio photos and categories.",
        icon: Images,
      },
    ],
  },
  {
    label: "Website",
    items: [
      {
        id: "content",
        label: "Pages",
        title: "Site Pages",
        description: "Edit text sections shown on public pages.",
        icon: FileText,
      },
      {
        id: "repeatable",
        label: "Website Blocks",
        title: "Website Blocks",
        description:
          "Manage reusable blocks such as process steps, studio values, testimonials and legal sections.",
        icon: ListOrdered,
      },
      {
        id: "businessInfo",
        label: "Business Info",
        title: "Business Info",
        description:
          "Edit public studio details such as contact email, phone and business information.",
        icon: Building2,
      },
      {
        id: "brand",
        label: "Brand / SEO",
        title: "Brand / SEO",
        description:
          "Manage logo, favicon, social preview image and default SEO metadata.",
        icon: Palette,
      },
    ],
  },
  {
    label: "Administration",
    items: [
      {
        id: "users",
        label: "Users",
        title: "Users",
        description: "Manage admin users and access.",
        icon: Users,
      },
      {
        id: "emailLog",
        label: "Email Log",
        title: "Email Log",
        description: "Review outbound email delivery, retries and failures.",
        icon: Mail,
      },
      {
        id: "storage",
        label: "Storage",
        title: "Storage",
        description: "Check uploaded file storage, orphan files and cleanup jobs.",
        icon: HardDrive,
      },
      {
        id: "auditLog",
        label: "Audit Log",
        title: "Audit Log",
        description: "Review important admin actions and security events.",
        icon: History,
      },
      {
        id: "productionHealth",
        label: "Production Health",
        title: "Production Health",
        description:
          "Check deployment readiness, email delivery, DNS records, upload scanning and backend connectivity.",
        icon: Activity,
      },
      {
        id: "systemSettings",
        label: "System Settings",
        title: "System Settings",
        description:
          "Configure email delivery, notifications and technical site settings.",
        icon: Settings,
      },
    ],
  },
  {
    label: "Account",
    items: [
      {
        id: "account",
        label: "My Account",
        title: "My Account",
        description: "Review your current admin session and account security.",
        icon: UserCircle,
      },
    ],
  },
];

export const ADMIN_NAV_ITEMS: ReadonlyArray<AdminNavItem> =
  ADMIN_NAV_GROUPS.flatMap((group) => group.items);

export const ADMIN_SECTION_HASHES: Readonly<Record<AdminSection, string>> = {
  dashboard: "dashboard",
  orders: "orders",
  contactMessages: "contact-messages",
  services: "services",
  inStock: "in-stock",
  portfolio: "portfolio",
  content: "content",
  repeatable: "repeatable-content",
  businessInfo: "business-info",
  brand: "brand-seo",
  users: "users",
  account: "my-account",
  auditLog: "audit-log",
  emailLog: "email-log",
  storage: "storage",
  productionHealth: "production-health",
  systemSettings: "system-settings",
};

const ADMIN_SECTIONS_BY_HASH = Object.entries(ADMIN_SECTION_HASHES).reduce(
  (sections, [section, hash]) => {
    sections[hash] = section as AdminSection;
    return sections;
  },
  {} as Record<string, AdminSection>,
);

ADMIN_SECTIONS_BY_HASH.settings = "systemSettings";

export function getAdminSectionFromHash(): AdminSection {
  if (typeof window === "undefined") {
    return "dashboard";
  }

  const hash = window.location.hash.replace(/^#\/?/, "").trim();
  return ADMIN_SECTIONS_BY_HASH[hash] ?? "dashboard";
}

export function updateAdminSectionHash(section: AdminSection): void {
  if (typeof window === "undefined") {
    return;
  }

  const nextHash = `#${ADMIN_SECTION_HASHES[section]}`;
  if (window.location.hash === nextHash) {
    return;
  }

  window.history.pushState(null, "", nextHash);
}
