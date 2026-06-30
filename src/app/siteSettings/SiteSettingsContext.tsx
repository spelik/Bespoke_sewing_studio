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
import { getPublicBrandSettings } from "../../api/brandSettingsApi";
import { PUBLIC_BRAND_SETTINGS_FALLBACK, PUBLIC_SITE_SETTINGS_FALLBACK } from "../../data/siteData";
import type { PublicBrandSettings, PublicSiteSettings } from "../types";

interface SiteSettingsContextValue {
  settings: PublicSiteSettings;
  brand: PublicBrandSettings;
  refresh(): Promise<void>;
}

const SiteSettingsContext = createContext<SiteSettingsContextValue | null>(null);

export function SiteSettingsProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<PublicSiteSettings>(
    PUBLIC_SITE_SETTINGS_FALLBACK,
  );
  const [brand,setBrand]=useState<PublicBrandSettings>(PUBLIC_BRAND_SETTINGS_FALLBACK);

  const refresh = useCallback(async () => {
    const [siteResponse,brandResponse] = await Promise.all([getPublicSiteSettings(),getPublicBrandSettings()]);
    setSettings(siteResponse); setBrand(brandResponse);
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

  useEffect(() => {
    let active = true;

    getPublicBrandSettings()
      .then((value) => {
        if (active) {
          setBrand(value);
        }
      })
      .catch(() => {
        // The typed fallback keeps the public website available while the API is offline.
      });

    return () => {
      active = false;
    };
  }, []);

  const value = useMemo(() => ({ settings, brand, refresh }), [brand, refresh, settings]);

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
