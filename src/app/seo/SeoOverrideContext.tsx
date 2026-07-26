import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";

export interface SeoOverrideState {
  title?: string;
  description?: string;
  robots?: string;
  canonicalPath?: string;
  ogImageUrl?: string | null;
  structuredData?: Record<string, unknown> | null;
}

interface SeoOverrideContextValue {
  override: SeoOverrideState | null;
  setOverride(next: SeoOverrideState | null): void;
}

const SeoOverrideContext = createContext<SeoOverrideContextValue>({
  override: null,
  setOverride: () => undefined,
});

export function SeoOverrideProvider({ children }: { children: ReactNode }) {
  const [override, setOverride] = useState<SeoOverrideState | null>(null);
  const value = useMemo(() => ({ override, setOverride }), [override]);
  return (
    <SeoOverrideContext.Provider value={value}>
      {children}
    </SeoOverrideContext.Provider>
  );
}

export function useSeoOverride() {
  return useContext(SeoOverrideContext);
}
