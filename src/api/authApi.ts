import { apiClient } from "./apiClient";

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

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiClient.post<LoginRequest, LoginResponse>("/auth/login", {
    email: email.trim(),
    password,
  });
}

export function getMe(): Promise<AdminUser> {
  return apiClient.get<AdminUser>("/auth/me");
}
