import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { getPublicSiteSettings } from "../../api/siteSettingsApi";
import { PUBLIC_SITE_SETTINGS_FALLBACK } from "../../data/siteData";
import type { PublicSiteSettings } from "../types";

interface SiteSettingsContextValue {
  settings: PublicSiteSettings;
  refresh(): Promise<void>;
}

const SiteSettingsContext = createContext<SiteSettingsContextValue | null>(null);

export function SiteSettingsProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<PublicSiteSettings>(
    PUBLIC_SITE_SETTINGS_FALLBACK,
  );

  const refresh = useCallback(async () => {
    const response = await getPublicSiteSettings();
    setSettings(response);
  }, []);

  useEffect(() => {
    let active = true;

    getPublicSiteSettings()
      .then((response) => {
        if (active) {
          setSettings(response);
        }
      })
      .catch(() => {
        // The typed fallback keeps the public website available while the API is offline.
      });

    return () => {
      active = false;
    };
  }, []);

  const value = useMemo(() => ({ settings, refresh }), [refresh, settings]);

  return (
    <SiteSettingsContext.Provider value={value}>
      {children}
    </SiteSettingsContext.Provider>
  );
}

export function useSiteSettings(): SiteSettingsContextValue {
  const context = useContext(SiteSettingsContext);
  if (!context) {
    throw new Error("useSiteSettings must be used within SiteSettingsProvider.");
  }

  return context;
}
