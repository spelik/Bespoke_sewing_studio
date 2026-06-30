import { ApiError, apiClient } from "./apiClient";

export interface ManagedAdminUser {
  id: string;
  email: string;
  roles: string[];
  isCurrentUser: boolean;
  isDisabled: boolean;
  canDisable: boolean;
  canDelete: boolean;
  lockoutEnd: string | null;
}

interface CreateAdminUserRequest {
  email: string;
  password: string;
}

interface UpdateAdminUserStatusRequest {
  isDisabled: boolean;
}

interface ResetAdminUserPasswordRequest {
  password: string;
}

export function getAdminUsers(): Promise<ManagedAdminUser[]> {
  return apiClient.get<ManagedAdminUser[]>("/admin/users");
}

export function createAdminUser(email: string, password: string): Promise<ManagedAdminUser> {
  const request: CreateAdminUserRequest = {
    email: email.trim(),
    password,
  };

  return apiClient.post<CreateAdminUserRequest, ManagedAdminUser>("/admin/users", request);
}

export function setAdminUserDisabled(id: string, isDisabled: boolean): Promise<ManagedAdminUser> {
  const request: UpdateAdminUserStatusRequest = { isDisabled };
  return apiClient.patch<UpdateAdminUserStatusRequest, ManagedAdminUser>(
    `/admin/users/${id}/status`,
    request,
  );
}

export function resetAdminUserPassword(id: string, password: string): Promise<ManagedAdminUser> {
  const request: ResetAdminUserPasswordRequest = { password };
  return apiClient.post<ResetAdminUserPasswordRequest, ManagedAdminUser>(
    `/admin/users/${id}/reset-password`,
    request,
  );
}

export function deleteAdminUser(id: string): Promise<void> {
  return apiClient.delete<void>(`/admin/users/${id}`);
}

export function getAdminUsersErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().find(Boolean)
      : undefined;

    return validationMessage ?? error.message;
  }

  return "The admin user request could not be completed. Please try again.";
}
