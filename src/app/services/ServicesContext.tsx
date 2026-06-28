import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { getPublicServices } from "../../api/servicesApi";
import { PUBLIC_SERVICES_FALLBACK } from "../../data/servicesData";
import type { PublicServiceOffering } from "../types";

interface ServicesContextValue {
  services: PublicServiceOffering[];
  isFallback: boolean;
  refresh(): Promise<void>;
}

const ServicesContext = createContext<ServicesContextValue | null>(null);

export function ServicesProvider({ children }: { children: ReactNode }) {
  const [services, setServices] = useState<PublicServiceOffering[]>([
    ...PUBLIC_SERVICES_FALLBACK,
  ]);
  const [isFallback, setIsFallback] = useState(true);

  const refresh = useCallback(async () => {
    const response = await getPublicServices();
    setServices(response);
    setIsFallback(false);
  }, []);

  useEffect(() => {
    let active = true;
    getPublicServices()
      .then((response) => {
        if (active) {
          setServices(response);
          setIsFallback(false);
        }
      })
      .catch(() => {
        // Typed fallback keeps public pages available while the API is offline.
      });

    return () => {
      active = false;
    };
  }, []);

  const value = useMemo(
    () => ({ services, isFallback, refresh }),
    [isFallback, refresh, services],
  );

  return <ServicesContext.Provider value={value}>{children}</ServicesContext.Provider>;
}

export function useServices(): ServicesContextValue {
  const context = useContext(ServicesContext);
  if (!context) {
    throw new Error("useServices must be used within ServicesProvider.");
  }

  return context;
}
