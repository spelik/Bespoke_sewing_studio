import type { ReactNode } from "react";
import { useEffect, useRef } from "react";
import { LoaderCircle, Search, X } from "lucide-react";

export interface AdminFilterOption {
  value: string;
  label: string;
  meta?: string;
}

export function AdminActionButton({
  children,
  icon,
  variant = "secondary",
  type = "button",
  disabled = false,
  isLoading = false,
  onClick,
}: {
  children: ReactNode;
  icon?: ReactNode;
  variant?: "secondary" | "primary" | "danger";
  type?: "button" | "submit";
  disabled?: boolean;
  isLoading?: boolean;
  onClick?(): void;
}) {
  const variantClass =
    variant === "primary"
      ? "border-accent bg-accent text-accent-foreground hover:bg-accent/90"
      : variant === "danger"
        ? "border-destructive/35 bg-background text-destructive hover:border-destructive"
        : "border-border bg-background text-foreground hover:border-foreground";

  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled || isLoading}
      className={`inline-flex items-center justify-center gap-2 border px-4 py-2 text-[10px] tracking-wide font-sans transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${variantClass}`}
    >
      {isLoading ? (
        <LoaderCircle size={12} className="animate-spin" aria-hidden="true" />
      ) : (
        icon
      )}
      {children}
    </button>
  );
}

export function AdminSearchInput({
  label,
  value,
  placeholder,
  ariaLabel,
  onChange,
  className = "",
}: {
  label: string;
  value: string;
  placeholder: string;
  ariaLabel: string;
  onChange(value: string): void;
  className?: string;
}) {
  return (
    <label className={`block min-w-0 ${className}`}>
      <span className="mb-1 block text-[9px] uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </span>
      <span className="relative block">
        <Search
          size={13}
          className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground"
          aria-hidden="true"
        />
        <input
          type="text"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={placeholder}
          className="w-full border border-border bg-background pl-8 pr-8 py-2 text-[10px] font-sans text-foreground focus:outline-none focus:border-accent"
          aria-label={ariaLabel}
        />
        {value ? (
          <button
            type="button"
            onClick={() => onChange("")}
            className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-muted-foreground hover:text-foreground"
            aria-label={`Clear ${label.toLowerCase()}`}
          >
            <X size={12} />
          </button>
        ) : null}
      </span>
    </label>
  );
}

export function AdminFilterDropdown({
  id,
  label,
  value,
  placeholder,
  options,
  isOpen,
  allowEmpty = true,
  onToggle,
  onClose,
  onChange,
  className = "",
}: {
  id: string;
  label: string;
  value: string;
  placeholder: string;
  options: readonly AdminFilterOption[];
  isOpen: boolean;
  allowEmpty?: boolean;
  onToggle(): void;
  onClose(): void;
  onChange(value: string): void;
  className?: string;
}) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const selectedOption = options.find((option) => option.value === value);
  const displayLabel = selectedOption?.label ?? (value ? value : placeholder);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    function handlePointerDown(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        onClose();
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    return () => document.removeEventListener("mousedown", handlePointerDown);
  }, [isOpen, onClose]);

  return (
    <div ref={containerRef} className={`relative min-w-0 ${className}`}>
      <span className="mb-1 block text-[9px] uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </span>
      <button
        type="button"
        onClick={onToggle}
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            onClose();
          }
        }}
        className="flex w-full items-center justify-between gap-3 border border-border bg-background px-3 py-2 text-left text-[10px] font-sans text-foreground hover:border-foreground focus:outline-none focus:border-accent"
        aria-expanded={isOpen}
        aria-controls={`${id}-dropdown`}
      >
        <span className={value ? "truncate" : "truncate text-muted-foreground"}>
          {displayLabel}
        </span>
        <span className="text-[9px] text-muted-foreground" aria-hidden="true">
          ▾
        </span>
      </button>

      {isOpen ? (
        <div
          id={`${id}-dropdown`}
          className="absolute left-0 right-0 top-full z-40 mt-1 max-h-64 min-w-[220px] overflow-y-auto border border-border bg-background shadow-[0_18px_45px_rgba(0,0,0,0.16)]"
        >
          {allowEmpty ? (
            <button
              type="button"
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => {
                onChange("");
                onClose();
              }}
              className={`flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-[10px] hover:bg-muted ${
                value === "" ? "bg-muted/50 text-foreground" : "text-muted-foreground"
              }`}
            >
              {placeholder}
              {value === "" ? <span aria-hidden="true">✓</span> : null}
            </button>
          ) : null}

          {options.map((option) => (
            <button
              key={option.value}
              type="button"
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => {
                onChange(option.value);
                onClose();
              }}
              className={`w-full border-t border-border/60 px-3 py-2 text-left text-[10px] hover:bg-muted ${
                option.value === value ? "bg-muted/50 text-foreground" : "text-foreground"
              }`}
            >
              <span className="flex items-center justify-between gap-2">
                <span className="truncate">{option.label}</span>
                {option.value === value ? <span aria-hidden="true">✓</span> : null}
              </span>
              {option.meta ? (
                <span className="mt-0.5 block truncate font-mono text-[8px] text-muted-foreground">
                  {option.meta}
                </span>
              ) : null}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

export function AdminTableState({
  message,
  isLoading = false,
}: {
  message: string;
  isLoading?: boolean;
}) {
  return (
    <div className="flex min-h-[120px] flex-col items-center justify-center gap-2 text-center text-[11px] text-muted-foreground">
      {isLoading ? (
        <LoaderCircle size={15} className="animate-spin" aria-hidden="true" />
      ) : null}
      <span>{message}</span>
    </div>
  );
}

export function AdminConfirmDialog({
  title,
  description,
  confirmLabel,
  cancelLabel = "Cancel",
  isBusy = false,
  onCancel,
  onConfirm,
}: {
  title: string;
  description: ReactNode;
  confirmLabel: string;
  cancelLabel?: string;
  isBusy?: boolean;
  onCancel(): void;
  onConfirm(): void;
}) {
  return (
    <div
      className="fixed inset-0 z-[90] flex items-center justify-center bg-black/40 px-4 py-8"
      role="dialog"
      aria-modal="true"
      aria-labelledby="admin-confirm-dialog-title"
    >
      <div className="w-full max-w-md border border-border bg-card p-6 shadow-2xl">
        <h3
          id="admin-confirm-dialog-title"
          className="font-serif text-[1.2rem] font-light text-foreground"
        >
          {title}
        </h3>
        <div className="mt-2 text-[11px] leading-5 text-muted-foreground font-sans">
          {description}
        </div>
        <div className="mt-6 flex flex-wrap justify-end gap-2">
          <AdminActionButton disabled={isBusy} onClick={onCancel}>
            {cancelLabel}
          </AdminActionButton>
          <AdminActionButton
            variant="danger"
            isLoading={isBusy}
            onClick={onConfirm}
          >
            {confirmLabel}
          </AdminActionButton>
        </div>
      </div>
    </div>
  );
}

export function AdminPagination({
  currentPage,
  pageSize,
  totalItems,
  onPageChange,
}: {
  currentPage: number;
  pageSize: number;
  totalItems: number;
  onPageChange(page: number): void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  if (totalPages <= 1) {
    return null;
  }

  const startItem = (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(totalItems, currentPage * pageSize);
  const pages = getVisiblePages(currentPage, totalPages);

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border border-border bg-card px-4 py-3 font-sans text-[10px] text-muted-foreground">
      <span>
        Showing {startItem}-{endItem} of {totalItems}
      </span>
      <div className="flex flex-wrap items-center gap-1">
        <button
          type="button"
          onClick={() => onPageChange(Math.max(1, currentPage - 1))}
          disabled={currentPage <= 1}
          className="border border-border bg-background px-3 py-1.5 text-foreground disabled:cursor-not-allowed disabled:opacity-40"
        >
          Prev
        </button>
        {pages.map((page) => (
          <button
            key={page}
            type="button"
            onClick={() => onPageChange(page)}
            className={`border px-3 py-1.5 ${page === currentPage ? "border-accent bg-accent text-accent-foreground" : "border-border bg-background text-foreground hover:border-foreground"}`}
            aria-current={page === currentPage ? "page" : undefined}
          >
            {page}
          </button>
        ))}
        <button
          type="button"
          onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
          disabled={currentPage >= totalPages}
          className="border border-border bg-background px-3 py-1.5 text-foreground disabled:cursor-not-allowed disabled:opacity-40"
        >
          Next
        </button>
      </div>
    </div>
  );
}

function getVisiblePages(currentPage: number, totalPages: number): number[] {
  if (totalPages <= 7) {
    return Array.from({ length: totalPages }, (_, index) => index + 1);
  }

  const start = Math.max(1, Math.min(currentPage - 2, totalPages - 4));
  return Array.from({ length: 5 }, (_, index) => start + index);
}
