import {
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
  UserCircle,
  Users,
  type LucideIcon,
} from "lucide-react";

export type AdminSection =
  | "dashboard"
  | "orders"
  | "contactMessages"
  | "services"
  | "portfolio"
  | "content"
  | "repeatable"
  | "brand"
  | "users"
  | "account"
  | "auditLog"
  | "emailLog"
  | "storage"
  | "settings";

export interface AdminNavItem {
  id: AdminSection;
  label: string;
  icon: LucideIcon;
}

export const ADMIN_NAV_ITEMS: ReadonlyArray<AdminNavItem> = [
  { id: "dashboard", label: "Dashboard", icon: LayoutDashboard },
  { id: "orders", label: "Orders", icon: Package },
  { id: "contactMessages", label: "Contact Messages", icon: Mail },
  { id: "services", label: "Services", icon: Scissors },
  { id: "portfolio", label: "Portfolio", icon: Images },
  { id: "content", label: "Content", icon: FileText },
  { id: "repeatable", label: "Repeatable Content", icon: ListOrdered },
  { id: "brand", label: "Brand / SEO", icon: Palette },
  { id: "users", label: "Users", icon: Users },
  { id: "account", label: "My account", icon: UserCircle },
  { id: "auditLog", label: "Audit Log", icon: History },
  { id: "emailLog", label: "Email Log", icon: Mail },
  { id: "storage", label: "Storage", icon: HardDrive },
  { id: "settings", label: "Settings", icon: Settings },
];

export const ADMIN_SECTION_HASHES: Readonly<Record<AdminSection, string>> = {
  dashboard: "dashboard",
  orders: "orders",
  contactMessages: "contact-messages",
  services: "services",
  portfolio: "portfolio",
  content: "content",
  repeatable: "repeatable-content",
  brand: "brand-seo",
  users: "users",
  account: "my-account",
  auditLog: "audit-log",
  emailLog: "email-log",
  storage: "storage",
  settings: "settings",
};

const ADMIN_SECTIONS_BY_HASH = Object.entries(ADMIN_SECTION_HASHES).reduce(
  (sections, [section, hash]) => {
    sections[hash] = section as AdminSection;
    return sections;
  },
  {} as Record<string, AdminSection>,
);

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
