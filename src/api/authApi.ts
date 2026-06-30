import { ApiError, apiClient } from "./apiClient";

export interface AdminUser {
  id: string;
  email: string;
  roles: string[];
}

export interface LoginResponse {
  accessToken: string;
  tokenType: "Bearer";
  expiresAt: string;
  user: AdminUser;
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

export function getMe(): Promise<AdminUser> {
  return apiClient.get<AdminUser>("/auth/me");
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
