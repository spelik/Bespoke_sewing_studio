import { type FormEvent, type ReactNode, useState } from "react";
import { AlertTriangle, KeyRound, LoaderCircle, LogOut, ShieldCheck, UserCircle } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import { changeOwnPassword, getAccountErrorMessage } from "../../api/authApi";

interface AdminAccountPanelProps {
  email: string;
  roles: readonly string[];
  onLogout(): void;
  onUnauthorized(): void;
}

const MIN_PASSWORD_LENGTH = 12;

export function AdminAccountPanel({
  email,
  roles,
  onLogout,
  onUnauthorized,
}: AdminAccountPanelProps) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const hasCurrentPassword = currentPassword.trim().length > 0;
  const hasNewPasswordMinLength = newPassword.length >= MIN_PASSWORD_LENGTH;
  const hasConfirmPassword = confirmNewPassword.length > 0;
  const passwordsMatch = newPassword === confirmNewPassword;

  const canSubmit =
    hasCurrentPassword &&
    hasNewPasswordMinLength &&
    hasConfirmPassword &&
    passwordsMatch;

  function clearFeedback() {
    if (error) {
      setError(null);
    }

    if (message) {
      setMessage(null);
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setMessage(null);

    if (newPassword !== confirmNewPassword) {
      setError("New password confirmation does not match.");
      return;
    }

    if (currentPassword === newPassword) {
      setError("New password must be different from the current password.");
      return;
    }

    setIsSaving(true);
    try {
      await changeOwnPassword(currentPassword, newPassword, confirmNewPassword);
      setCurrentPassword("");
      setNewPassword("");
      setConfirmNewPassword("");
      setMessage("Password changed. Use the new password the next time you sign in.");
    } catch (reason: unknown) {
      if (
        reason instanceof ApiError &&
        (reason.status === 401 || reason.status === 403)
      ) {
        onUnauthorized();
        return;
      }

      setError(getAccountErrorMessage(reason));
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      <section className="bg-card border border-border">
        <div className="p-5 border-b border-border flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              My account
            </h2>
            <p className="text-[10px] text-muted-foreground font-sans mt-1 max-w-2xl">
              Review your current admin session and change your own password safely.
            </p>
          </div>
          <button
            type="button"
            onClick={onLogout}
            className="inline-flex items-center gap-2 px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground font-sans"
          >
            <LogOut size={12} /> Sign out
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 p-5 border-b border-border">
          <AdminAccountSummaryCard
            icon={<UserCircle size={15} aria-hidden="true" />}
            label="Signed in as"
            value={email}
          />
          <AdminAccountSummaryCard
            icon={<ShieldCheck size={15} aria-hidden="true" />}
            label="Access"
            value={roles.length > 0 ? roles.join(", ") : "Admin"}
            caption="Current model uses one Admin role."
          />
          <AdminAccountSummaryCard
            icon={<KeyRound size={15} aria-hidden="true" />}
            label="Password"
            value="Protected"
            caption="Passwords are never shown or returned by the API."
          />
        </div>

        <form onSubmit={handleSubmit} className="p-5 space-y-4 max-w-2xl">
          <div>
            <h3 className="text-[11px] font-medium tracking-wide text-foreground font-sans">
              Change password
            </h3>
            <p className="text-[10px] text-muted-foreground font-sans mt-1">
              Enter your current password and choose a new password with at least {MIN_PASSWORD_LENGTH} characters.
            </p>
          </div>

          {error ? (
            <div role="alert" className="border border-destructive/30 bg-destructive/5 px-4 py-3 text-[11px] text-destructive font-sans">
              {error}
            </div>
          ) : null}
          {message ? (
            <div role="status" className="border border-emerald-200 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700 font-sans">
              {message}
            </div>
          ) : null}

          <div className="grid grid-cols-1 gap-3">
            <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
              Current password
              <input
                type="password"
                value={currentPassword}
                onChange={(event) => {
                  setCurrentPassword(event.target.value);
                  clearFeedback();
                }}
                required
                autoComplete="current-password"
                className="mt-1 w-full border border-border bg-background px-3 py-2 text-[11px] text-foreground focus:outline-none focus:border-accent"
              />
            </label>
            <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
              New password
              <input
                type="password"
                value={newPassword}
                onChange={(event) => {
                  setNewPassword(event.target.value);
                  clearFeedback();
                }}
                required
                minLength={MIN_PASSWORD_LENGTH}
                autoComplete="new-password"
                className="mt-1 w-full border border-border bg-background px-3 py-2 text-[11px] text-foreground focus:outline-none focus:border-accent"
              />
              {newPassword.length > 0 && !hasNewPasswordMinLength ? (
                <span className="mt-1 block text-[9px] tracking-normal text-destructive">
                  Use at least {MIN_PASSWORD_LENGTH} characters.
                </span>
              ) : null}
            </label>
            <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
              Confirm new password
              <input
                type="password"
                value={confirmNewPassword}
                onChange={(event) => {
                  setConfirmNewPassword(event.target.value);
                  clearFeedback();
                }}
                required
                minLength={MIN_PASSWORD_LENGTH}
                autoComplete="new-password"
                className="mt-1 w-full border border-border bg-background px-3 py-2 text-[11px] text-foreground focus:outline-none focus:border-accent"
              />
              {hasConfirmPassword && !passwordsMatch ? (
                <span className="mt-1 block text-[9px] tracking-normal text-destructive">
                  Password confirmation does not match.
                </span>
              ) : null}
            </label>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <button
              type="submit"
              disabled={isSaving || !canSubmit}
              className="inline-flex items-center justify-center gap-2 border border-accent bg-accent px-4 py-2 text-[10px] tracking-wide text-accent-foreground hover:bg-accent/90 disabled:cursor-not-allowed disabled:border-border disabled:bg-muted disabled:text-muted-foreground disabled:opacity-70 transition-colors font-sans"
            >
              {isSaving ? <LoaderCircle size={12} className="animate-spin" /> : <KeyRound size={12} />}
              Change password
            </button>
            <span className="text-[10px] text-muted-foreground font-sans">
              The current password is checked only after submit. Password changes are recorded in Audit Log without storing the password.
            </span>
          </div>
        </form>
      </section>

      <div className="border border-amber-200 bg-amber-50 px-4 py-3 text-[10px] text-amber-800 font-sans flex gap-2">
        <AlertTriangle size={13} className="shrink-0 mt-0.5" />
        <p>
          After changing your password, existing JWT sessions may remain valid until they expire. Sign out and sign in again when testing the new password.
        </p>
      </div>
    </div>
  );
}

function AdminAccountSummaryCard({
  icon,
  label,
  value,
  caption,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  caption?: string;
}) {
  return (
    <div className="border border-border bg-background px-4 py-3">
      <div className="flex items-center gap-2 text-[9px] uppercase tracking-[0.22em] text-muted-foreground font-sans">
        {icon}
        {label}
      </div>
      <div className="mt-2 break-words font-serif text-[1.15rem] font-light text-foreground">
        {value}
      </div>
      {caption ? <div className="mt-1 text-[10px] text-muted-foreground font-sans">{caption}</div> : null}
    </div>
  );
}
