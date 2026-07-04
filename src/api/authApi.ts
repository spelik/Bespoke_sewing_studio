import { ApiError, apiClient } from "./apiClient";

export interface AdminUser {
  id: string;
  email: string;
  roles: string[];
}

export interface AuthTokenResponse {
  accessToken: string;
  tokenType: "Bearer";
  expiresAt: string;
  user: AdminUser;
}

export interface TwoFactorRequiredResponse {
  requiresTwoFactor: true;
}

export type LoginResponse = AuthTokenResponse | TwoFactorRequiredResponse;

export interface TwoFactorStatus {
  isEnabled: boolean;
  hasAuthenticatorKey: boolean;
  recoveryCodesRemaining: number;
}

export interface TwoFactorSetup {
  sharedKey: string;
  authenticatorUri: string;
  issuer: string;
  accountName: string;
  token: AuthTokenResponse;
}

export interface TwoFactorEnableResult {
  status: TwoFactorStatus;
  recoveryCodes: string[];
  token: AuthTokenResponse;
}

export interface TwoFactorStatusUpdateResult {
  status: TwoFactorStatus;
  token: AuthTokenResponse;
}

export interface TwoFactorRecoveryCodesResult {
  recoveryCodes: string[];
  recoveryCodesRemaining: number;
  token: AuthTokenResponse;
}

export interface AdminSession {
  id: string;
  createdAtUtc: string;
  expiresAtUtc: string;
  lastUsedAtUtc: string | null;
  revokedAtUtc: string | null;
  isCurrent: boolean;
  isRevoked: boolean;
  userAgent: string | null;
  createdByIp: string | null;
  revocationReason: string | null;
  status: "Current" | "Active" | "Revoked" | "Expired";
}

export interface AdminSessionRevocationResult {
  sessionId: string;
  revoked: boolean;
  isCurrent: boolean;
}

export interface AdminOtherSessionsRevocationResult {
  revokedCount: number;
}

interface LoginRequest {
  email: string;
  password: string;
}

interface ChangeOwnPasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiClient.post<LoginRequest, LoginResponse>("/auth/login", {
    email: email.trim(),
    password,
  });
}

export function verifyTwoFactor(code: string): Promise<AuthTokenResponse> {
  return apiClient.post<{ code: string }, AuthTokenResponse>("/auth/2fa/verify", { code });
}

export function getTwoFactorStatus(): Promise<TwoFactorStatus> {
  return apiClient.get<TwoFactorStatus>("/auth/2fa/status");
}

export function beginTwoFactorSetup(): Promise<TwoFactorSetup> {
  return apiClient.post<Record<string, never>, TwoFactorSetup>("/auth/2fa/setup", {});
}

export function enableTwoFactor(code: string): Promise<TwoFactorEnableResult> {
  return apiClient.post<{ code: string }, TwoFactorEnableResult>("/auth/2fa/enable", { code });
}

export function disableTwoFactor(currentPassword: string): Promise<TwoFactorStatusUpdateResult> {
  return apiClient.post<{ currentPassword: string }, TwoFactorStatusUpdateResult>("/auth/2fa/disable", {
    currentPassword,
  });
}

export function resetTwoFactorRecoveryCodes(
  currentPassword: string,
): Promise<TwoFactorRecoveryCodesResult> {
  return apiClient.post<{ currentPassword: string }, TwoFactorRecoveryCodesResult>(
    "/auth/2fa/recovery-codes/reset",
    { currentPassword },
  );
}

export function resetAuthenticator(currentPassword: string): Promise<TwoFactorSetup> {
  return apiClient.post<{ currentPassword: string }, TwoFactorSetup>(
    "/auth/2fa/authenticator/reset",
    { currentPassword },
  );
}

export function logout(): Promise<void> {
  return apiClient.post<Record<string, never>, void>("/auth/logout", {});
}

export function getMe(): Promise<AdminUser> {
  return apiClient.get<AdminUser>("/auth/me");
}

export function getSessions(): Promise<AdminSession[]> {
  return apiClient.get<AdminSession[]>("/auth/sessions");
}

export function revokeSession(sessionId: string): Promise<AdminSessionRevocationResult> {
  return apiClient.post<Record<string, never>, AdminSessionRevocationResult>(
    `/auth/sessions/${sessionId}/revoke`,
    {},
  );
}

export function revokeOtherSessions(): Promise<AdminOtherSessionsRevocationResult> {
  return apiClient.post<Record<string, never>, AdminOtherSessionsRevocationResult>(
    "/auth/sessions/revoke-others",
    {},
  );
}

export function changeOwnPassword(
  currentPassword: string,
  newPassword: string,
  confirmNewPassword: string,
): Promise<AdminUser> {
  return apiClient.post<ChangeOwnPasswordRequest, AdminUser>("/auth/me/password", {
    currentPassword,
    newPassword,
    confirmNewPassword,
  });
}

export function getAccountErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "The account request could not be completed. Please try again.";
}
