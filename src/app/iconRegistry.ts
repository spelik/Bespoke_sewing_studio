import {
  Award,
  Check,
  Clock,
  Heart,
  Mail,
  MapPin,
  Package,
  Phone,
  RefreshCw,
  Scissors,
  Shield,
  ShoppingBag,
  Sparkles,
  TrendingUp,
  Users,
  type LucideIcon,
} from "lucide-react";
import type {
  AdminStatIconKey,
  ContactIconKey,
  ServiceIconKey,
  ValueIconKey,
} from "./types";

export const SERVICE_ICONS: Record<ServiceIconKey, LucideIcon> = {
  scissors: Scissors,
  sparkles: Sparkles,
  refresh: RefreshCw,
  package: Package,
};

export const VALUE_ICONS: Record<ValueIconKey, LucideIcon> = {
  award: Award,
  heart: Heart,
  shield: Shield,
  check: Check,
};

export const CONTACT_ICONS: Record<ContactIconKey, LucideIcon> = {
  location: MapPin,
  phone: Phone,
  email: Mail,
  hours: Clock,
};

export const ADMIN_STAT_ICONS: Record<AdminStatIconKey, LucideIcon> = {
  orders: ShoppingBag,
  active: Clock,
  clients: Users,
  revenue: TrendingUp,
};
