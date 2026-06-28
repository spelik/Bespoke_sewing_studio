import {
  Award,
  Check,
  Heart,
  Package,
  RefreshCw,
  Scissors,
  Shield,
  Sparkles,
  type LucideIcon,
} from "lucide-react";
import type {
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
