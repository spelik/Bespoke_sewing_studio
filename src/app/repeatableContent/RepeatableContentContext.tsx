import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { getPublicRepeatableContentGroup } from "../../api/repeatableContentApi";
import {
  HOW_IT_WORKS,
  PRIVACY_SECTIONS,
  STUDIO_VALUES,
  TESTIMONIALS,
} from "../../data/siteData";
import type {
  PrivacySection,
  ProcessStep,
  PublicRepeatableContentGroup,
  StudioValue,
  Testimonial,
  ValueIconKey,
} from "../types";

const GROUP_KEYS = {
  processSteps: "process-steps",
  studioValues: "studio-values",
  testimonials: "testimonials",
  privacySections: "privacy-sections",
} as const;

const VALUE_ICON_KEYS: ValueIconKey[] = ["award", "heart", "shield", "check"];

interface RepeatableContentValue {
  processSteps: ProcessStep[];
  studioValues: StudioValue[];
  testimonials: Testimonial[];
  privacySections: PrivacySection[];
  isFallback: boolean;
  refresh(): Promise<void>;
}

const fallbackContent = {
  processSteps: [...HOW_IT_WORKS],
  studioValues: [...STUDIO_VALUES],
  testimonials: [...TESTIMONIALS],
  privacySections: [...PRIVACY_SECTIONS],
};

const RepeatableContentContext = createContext<RepeatableContentValue | null>(null);

export function RepeatableContentProvider({ children }: { children: ReactNode }) {
  const [content, setContent] = useState(fallbackContent);
  const [isFallback, setIsFallback] = useState(true);

  const refresh = useCallback(async () => {
    const nextContent = await loadRepeatableContent();
    setContent(nextContent);
    setIsFallback(false);
  }, []);

  useEffect(() => {
    let active = true;

    loadRepeatableContent()
      .then((nextContent) => {
        if (!active) return;
        setContent(nextContent);
        setIsFallback(false);
      })
      .catch(() => {
        // Typed fallback keeps public pages available while the API is offline.
      });

    return () => {
      active = false;
    };
  }, []);

  const value = useMemo(
    () => ({ ...content, isFallback, refresh }),
    [content, isFallback, refresh],
  );

  return (
    <RepeatableContentContext.Provider value={value}>
      {children}
    </RepeatableContentContext.Provider>
  );
}

export function useRepeatableContent(): RepeatableContentValue {
  const value = useContext(RepeatableContentContext);
  if (!value) {
    throw new Error("useRepeatableContent must be used within RepeatableContentProvider.");
  }

  return value;
}

async function loadRepeatableContent() {
  const [processGroup, valuesGroup, testimonialsGroup, privacyGroup] = await Promise.all([
    getPublicRepeatableContentGroup(GROUP_KEYS.processSteps),
    getPublicRepeatableContentGroup(GROUP_KEYS.studioValues),
    getPublicRepeatableContentGroup(GROUP_KEYS.testimonials),
    getPublicRepeatableContentGroup(GROUP_KEYS.privacySections),
  ]);

  return {
    processSteps: mapProcessSteps(processGroup),
    studioValues: mapStudioValues(valuesGroup),
    testimonials: mapTestimonials(testimonialsGroup),
    privacySections: mapPrivacySections(privacyGroup),
  };
}

function mapProcessSteps(group: PublicRepeatableContentGroup): ProcessStep[] {
  return group.items.map((item, index) => ({
    step: cleanText(item.label) || String(index + 1).padStart(2, "0"),
    title: cleanText(item.title),
    desc: cleanText(item.body),
  }));
}

function mapStudioValues(group: PublicRepeatableContentGroup): StudioValue[] {
  return group.items.map((item) => ({
    icon: isValueIconKey(item.iconKey) ? item.iconKey : "check",
    title: cleanText(item.title),
    desc: cleanText(item.body),
  }));
}

function mapTestimonials(group: PublicRepeatableContentGroup): Testimonial[] {
  return group.items.map((item) => ({
    name: cleanText(item.title) || "Client",
    location: cleanText(item.location),
    rating: clampRating(item.rating),
    text: cleanText(item.body),
    service: cleanText(item.service),
  }));
}

function mapPrivacySections(group: PublicRepeatableContentGroup): PrivacySection[] {
  return group.items.map((item) => ({
    title: cleanText(item.title),
    body: cleanText(item.body),
  }));
}

function cleanText(value: string | null | undefined): string {
  return value?.trim() ?? "";
}

function isValueIconKey(value: string | null | undefined): value is ValueIconKey {
  return VALUE_ICON_KEYS.includes(value as ValueIconKey);
}

function clampRating(value: number | null | undefined): number {
  if (typeof value !== "number" || !Number.isFinite(value)) return 5;
  return Math.min(5, Math.max(1, Math.round(value)));
}
