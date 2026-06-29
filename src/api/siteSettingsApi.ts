import type {
  AdminEmailDeliverySettings,
  AdminSiteSettings,
  EmailNotificationResult,
  PublicSiteSettings,
  UpdateEmailDeliverySettingsRequest,
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

export function getAdminEmailDeliverySettings(): Promise<AdminEmailDeliverySettings> {
  return apiClient.get<AdminEmailDeliverySettings>("admin/email-delivery");
}

export function updateAdminEmailDeliverySettings(
  request: UpdateEmailDeliverySettingsRequest,
): Promise<AdminEmailDeliverySettings> {
  return apiClient.patch<
    UpdateEmailDeliverySettingsRequest,
    AdminEmailDeliverySettings
  >("admin/email-delivery", request);
}

export function sendTestEmailNotification(): Promise<EmailNotificationResult> {
  return apiClient.post<Record<string, never>, EmailNotificationResult>(
    "admin/notifications/test-email",
    {},
  );
}
