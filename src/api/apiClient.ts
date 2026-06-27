import { appConfig } from "../config/appConfig";

export interface ApiClient {
  readonly baseUrl: string;
  readonly mode: "prototype";
  resolve<T>(mockData: T): T;
}

function assertPrototypeMode() {
  if (!appConfig.isPrototypeMode) {
    throw new Error("A real API client has not been configured yet.");
  }
}

export const apiClient: ApiClient = {
  baseUrl: appConfig.apiBaseUrl,
  mode: "prototype",
  resolve<T>(mockData: T) {
    assertPrototypeMode();
    return mockData;
  },
};
