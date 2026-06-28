import type {
  AdminSiteSettings,
  EmailNotificationResult,
  PublicSiteSettings,
  UpdateSiteSettingsRequest,
} from "../app/types";
import { apiClient } from "./apiClient";

export function getPublicSiteSettings(): Promise<PublicSiteSettings> {
  return apiClient.get<PublicSiteSettings>("site-settings/public");
}

export function getAdminSiteSettings(): Promise<AdminSiteSettings> {
  return apiClient.get<AdminSiteSettings>("admin/site-settings");
}

export function updateAdminSiteSettings(
  request: UpdateSiteSettingsRequest,
): Promise<AdminSiteSettings> {
  return apiClient.patch<UpdateSiteSettingsRequest, AdminSiteSettings>(
    "admin/site-settings",
    request,
  );
}

export function sendTestEmailNotification(): Promise<EmailNotificationResult> {
  return apiClient.post<Record<string, never>, EmailNotificationResult>(
    "admin/notifications/test-email",
    {},
  );
}
