const ACCESS_TOKEN_KEY = "bespoke-studio.admin-access-token";

function getStorage(): Storage | null {
  return typeof window === "undefined" ? null : window.sessionStorage;
}

export function getAccessToken(): string | null {
  return getStorage()?.getItem(ACCESS_TOKEN_KEY) ?? null;
}

export function setAccessToken(token: string): void {
  getStorage()?.setItem(ACCESS_TOKEN_KEY, token);
}

export function clearAccessToken(): void {
  getStorage()?.removeItem(ACCESS_TOKEN_KEY);
}
