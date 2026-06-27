export interface AppConfig {
  apiBaseUrl: string;
  isPrototypeMode: boolean;
}

export const appConfig: Readonly<AppConfig> = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5099/api",
  isPrototypeMode: true,
};
