import type { Page } from "../types";

export interface NavigableSectionProps {
  navigate: (page: Page) => void;
}
