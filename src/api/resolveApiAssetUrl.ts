import { appConfig } from "../config/appConfig";

const ABSOLUTE_HTTP_URL = /^https?:\/\//i;
const ABSOLUTE_API_BASE = /^https?:\/\//i;
/** Schemes other than http(s); backend asset URLs are root-relative `/api/...` or http(s). */
const DISALLOWED_SCHEME = /^[a-zA-Z][a-zA-Z0-9+.-]*:/;

/**
 * Resolves CMS/API asset URLs for browser use without requiring window/document.
 * Uses `new URL` only when `apiBaseUrl` is an absolute http(s) origin.
 */
export function resolveApiAssetUrl(
  assetUrl: string | null | undefined,
  apiBaseUrl: string = appConfig.apiBaseUrl,
): string | null {
  if (assetUrl == null) {
    return null;
  }

  const url = assetUrl.trim();
  if (!url) {
    return null;
  }

  if (ABSOLUTE_HTTP_URL.test(url) || url.startsWith("//")) {
    return url;
  }

  if (DISALLOWED_SCHEME.test(url)) {
    return null;
  }

  const absoluteBase = toAbsoluteUrlBase(apiBaseUrl);
  if (absoluteBase !== null) {
    return new URL(url, absoluteBase).toString();
  }

  return url.startsWith("/") ? url : `/${url}`;
}

function toAbsoluteUrlBase(apiBaseUrl: string): string | null {
  const trimmed = apiBaseUrl.trim();
  if (!ABSOLUTE_API_BASE.test(trimmed)) {
    return null;
  }

  const withoutApiSuffix = trimmed.replace(/\/api\/?$/i, "");
  return withoutApiSuffix.endsWith("/") ? withoutApiSuffix : `${withoutApiSuffix}/`;
}
