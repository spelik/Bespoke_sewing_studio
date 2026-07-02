import { ApiError, apiClient } from "./apiClient";

export interface OrphanPhysicalFile {
  relativePath: string;
  sizeBytes: number;
  lastModifiedAt: string | null;
}

export interface MissingPhysicalFile {
  uploadedFileId: string;
  originalFileName: string;
  storageKey: string;
  purpose: string;
  relatedInfo: string | null;
}

export interface StorageScanResult {
  databaseFileCount: number;
  physicalFileCount: number;
  totalPhysicalBytes: number;
  orphanPhysicalFileCount: number;
  orphanPhysicalBytes: number;
  missingPhysicalFileCount: number;
  scannedAt: string;
  orphanPhysicalFiles: OrphanPhysicalFile[];
  missingPhysicalFiles: MissingPhysicalFile[];
  cleanupJobs: StorageCleanupJobSummary;
}

export interface StorageCleanupJobSummary {
  pendingCount: number;
  processingCount: number;
  failedCount: number;
  succeededCount: number;
  skippedCount: number;
  failedJobs: FailedStorageCleanupJob[];
}

export interface FailedStorageCleanupJob {
  id: string;
  storageKey: string;
  reason: string;
  attempts: number;
  maxAttempts: number;
  lastError: string | null;
  nextAttemptAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface StorageCleanupFailure {
  relativePath: string;
  reason: string;
}

export interface StorageCleanupResult {
  deletedCount: number;
  deletedBytes: number;
  skippedCount: number;
  failedCount: number;
  failedItems: StorageCleanupFailure[];
}

export function scanAdminStorage(): Promise<StorageScanResult> {
  return apiClient.get<StorageScanResult>("/admin/storage/scan");
}

export function deleteAdminStorageOrphans(): Promise<StorageCleanupResult> {
  return apiClient.post<Record<string, never>, StorageCleanupResult>(
    "/admin/storage/delete-orphans",
    {},
  );
}

export function getStorageMaintenanceErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  return "Storage maintenance could not be completed. Please try again.";
}
