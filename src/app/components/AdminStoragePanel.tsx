import { useCallback, useEffect, useState } from "react";
import { AlertTriangle, Database, Files, HardDrive, ScanSearch, Trash2 } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  deleteAdminStorageOrphans,
  getStorageMaintenanceErrorMessage,
  scanAdminStorage,
  type StorageCleanupResult,
  type StorageScanResult,
} from "../../api/storageMaintenanceApi";
import { AdminActionButton, AdminConfirmDialog, AdminTableState } from "./AdminUi";
import { formatAdminDate } from "./adminOrderFormatting";

export function AdminStoragePanel({ onUnauthorized }: { onUnauthorized(): void }) {
  const [scan, setScan] = useState<StorageScanResult | null>(null);
  const [cleanup, setCleanup] = useState<StorageCleanupResult | null>(null);
  const [isScanning, setIsScanning] = useState(true);
  const [isDeleting, setIsDeleting] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadScan = useCallback(async () => {
    setIsScanning(true);
    setError(null);

    try {
      setScan(await scanAdminStorage());
    } catch (reason: unknown) {
      if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) {
        onUnauthorized();
        return;
      }

      setError(getStorageMaintenanceErrorMessage(reason));
    } finally {
      setIsScanning(false);
    }
  }, [onUnauthorized]);

  useEffect(() => {
    void loadScan();
  }, [loadScan]);

  async function handleDeleteOrphans() {
    setIsDeleting(true);
    setError(null);

    try {
      const result = await deleteAdminStorageOrphans();
      setCleanup(result);
      setConfirmDelete(false);
      await loadScan();
    } catch (reason: unknown) {
      if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) {
        onUnauthorized();
        return;
      }

      setError(getStorageMaintenanceErrorMessage(reason));
    } finally {
      setIsDeleting(false);
    }
  }

  const summaryCards = [
    { label: "DB files", value: scan?.databaseFileCount ?? "—", icon: Database },
    { label: "Physical files", value: scan?.physicalFileCount ?? "—", icon: Files },
    { label: "Storage size", value: scan ? formatBytes(scan.totalPhysicalBytes) : "—", icon: HardDrive },
    {
      label: "Orphan files",
      value: scan ? `${scan.orphanPhysicalFileCount} · ${formatBytes(scan.orphanPhysicalBytes)}` : "—",
      icon: Trash2,
    },
    { label: "Missing files", value: scan?.missingPhysicalFileCount ?? "—", icon: AlertTriangle },
  ] as const;

  return (
    <div className="space-y-5">
      <section className="border border-border bg-card">
        <div className="flex flex-wrap items-start justify-between gap-4 border-b border-border p-5">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              Upload storage health
            </h2>
            <p className="mt-1 max-w-2xl font-sans text-[10px] leading-5 text-muted-foreground">
              Compare database upload metadata with local physical files. Missing database files are report-only.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <AdminActionButton
              icon={<ScanSearch size={12} aria-hidden="true" />}
              isLoading={isScanning}
              onClick={() => void loadScan()}
            >
              Scan storage
            </AdminActionButton>
            {scan && scan.orphanPhysicalFileCount > 0 ? (
              <AdminActionButton
                variant="danger"
                icon={<Trash2 size={12} aria-hidden="true" />}
                disabled={isScanning}
                onClick={() => setConfirmDelete(true)}
              >
                Delete orphan files
              </AdminActionButton>
            ) : null}
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 p-5 sm:grid-cols-2 xl:grid-cols-5">
          {summaryCards.map((card) => (
            <div key={card.label} className="min-w-0 border border-border bg-background p-4">
              <div className="flex items-center gap-2 text-muted-foreground">
                <card.icon size={13} aria-hidden="true" />
                <span className="font-sans text-[9px] uppercase tracking-[0.16em]">
                  {card.label}
                </span>
              </div>
              <div className="mt-3 truncate font-serif text-[1.25rem] font-light text-foreground">
                {card.value}
              </div>
            </div>
          ))}
        </div>

        {scan ? (
          <div className="border-t border-border px-5 py-3 font-sans text-[9px] text-muted-foreground">
            Last scanned {formatAdminDate(scan.scannedAt)}. Lists are limited for safe admin display; summary counts cover the full scan.
          </div>
        ) : null}
      </section>

      {error ? (
        <div role="alert" className="border border-destructive/30 bg-destructive/5 px-4 py-3 font-sans text-[11px] text-destructive">
          {error}
        </div>
      ) : null}

      {cleanup ? (
        <div className="border border-border bg-card px-4 py-3 font-sans text-[10px] text-muted-foreground">
          Cleanup deleted {cleanup.deletedCount} file{cleanup.deletedCount === 1 ? "" : "s"} ({formatBytes(cleanup.deletedBytes)}), skipped {cleanup.skippedCount}, failed {cleanup.failedCount}.
        </div>
      ) : null}

      <StorageFilesTable
        title="Orphan physical files"
        description="Physical files under the configured upload root with no UploadedFiles metadata row."
        isLoading={isScanning && !scan}
        emptyMessage="No orphan physical files were found."
        columns={[
          { label: "Relative path", width: "w-[58%]" },
          { label: "Size", width: "w-[16%]" },
          { label: "Last modified", width: "w-[26%]" },
        ]}
        rows={(scan?.orphanPhysicalFiles ?? []).map((file) => [
          file.relativePath,
          formatBytes(file.sizeBytes),
          file.lastModifiedAt ? formatAdminDate(file.lastModifiedAt) : "Unknown",
        ])}
      />

      <StorageFilesTable
        title="Missing physical files"
        description="Database upload records whose local physical file is missing. These records are not changed automatically."
        isLoading={isScanning && !scan}
        emptyMessage="No missing physical files were found."
        columns={[
          { label: "Original name", width: "w-[24%]" },
          { label: "Stored relative path", width: "w-[34%]" },
          { label: "Purpose", width: "w-[16%]" },
          { label: "Related", width: "w-[26%]" },
        ]}
        rows={(scan?.missingPhysicalFiles ?? []).map((file) => [
          file.originalFileName,
          file.storageKey,
          file.purpose,
          file.relatedInfo ?? "Not linked",
        ])}
      />

      {confirmDelete && scan ? (
        <AdminConfirmDialog
          title="Delete orphan files?"
          description={
            <>
              This will re-check and permanently delete up to {scan.orphanPhysicalFileCount} physical file{scan.orphanPhysicalFileCount === 1 ? "" : "s"} that {scan.orphanPhysicalFileCount === 1 ? "is" : "are"} not referenced by database upload metadata. Missing database files will not be changed.
            </>
          }
          confirmLabel="Delete orphan files"
          isBusy={isDeleting}
          onCancel={() => setConfirmDelete(false)}
          onConfirm={() => void handleDeleteOrphans()}
        />
      ) : null}
    </div>
  );
}

function StorageFilesTable({
  title,
  description,
  isLoading,
  emptyMessage,
  columns,
  rows,
}: {
  title: string;
  description: string;
  isLoading: boolean;
  emptyMessage: string;
  columns: ReadonlyArray<{ label: string; width: string }>;
  rows: string[][];
}) {
  return (
    <section className="min-w-0 border border-border bg-card">
      <div className="border-b border-border p-5">
        <h2 className="font-serif text-[1.05rem] font-light text-foreground">{title}</h2>
        <p className="mt-1 font-sans text-[10px] text-muted-foreground">{description}</p>
      </div>
      {isLoading ? <AdminTableState message="Scanning storage..." isLoading /> : null}
      {!isLoading && rows.length === 0 ? <AdminTableState message={emptyMessage} /> : null}
      {rows.length > 0 ? (
        <div className="max-w-full overflow-x-auto">
          <table className="w-full table-fixed text-left font-sans text-[10px]">
            <colgroup>
              {columns.map((column) => <col key={column.label} className={column.width} />)}
            </colgroup>
            <thead className="bg-muted/40 text-[9px] uppercase tracking-wide text-muted-foreground">
              <tr>
                {columns.map((column) => <th key={column.label} className="px-4 py-2 font-medium">{column.label}</th>)}
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {rows.map((row, rowIndex) => (
                <tr key={`${row[0] ?? "row"}-${rowIndex}`}>
                  {row.map((value, columnIndex) => (
                    <td key={`${columnIndex}-${value}`} className="min-w-0 px-4 py-3 text-foreground">
                      <div className="truncate" title={value}>{value}</div>
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  );
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  return `${(bytes / 1024 / 1024 / 1024).toFixed(2)} GB`;
}
