export interface AppConfig {
  apiBaseUrl: string;
  publicSiteUrl: string | null;
  isPrototypeMode: boolean;
}

function optionalEnv(value: string | undefined): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed.replace(/\/+$/, "") : null;
}

export const appConfig: Readonly<AppConfig> = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5099/api",
  publicSiteUrl: optionalEnv(import.meta.env.VITE_PUBLIC_SITE_URL),
  isPrototypeMode: true,
};
