import { type FormEvent, type ReactNode, useCallback, useEffect, useState } from "react";
import { AlertTriangle, KeyRound, Laptop, LoaderCircle, LogOut, RefreshCw, ShieldCheck, UserCircle } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  changeOwnPassword,
  getAccountErrorMessage,
  getSessions,
  revokeOtherSessions,
  revokeSession,
  type AdminSession,
} from "../../api/authApi";
import { AdminActionButton, AdminConfirmDialog, AdminTableState } from "./AdminUi";
import { AdminTwoFactorPanel } from "./AdminTwoFactorPanel";

interface AdminAccountPanelProps {
  email: string;
  roles: readonly string[];
  onLogout(): Promise<void>;
  onUnauthorized(): void;
}

const MIN_PASSWORD_LENGTH = 12;

type SessionConfirmation =
  | { kind: "session"; session: AdminSession }
  | { kind: "others" }
  | null;

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
  const [sessions, setSessions] = useState<AdminSession[]>([]);
  const [sessionsLoading, setSessionsLoading] = useState(true);
  const [sessionsError, setSessionsError] = useState<string | null>(null);
  const [sessionsMessage, setSessionsMessage] = useState<string | null>(null);
  const [pendingSessionAction, setPendingSessionAction] = useState<string | null>(null);
  const [sessionConfirmation, setSessionConfirmation] = useState<SessionConfirmation>(null);

  const hasCurrentPassword = currentPassword.trim().length > 0;
  const hasNewPasswordMinLength = newPassword.length >= MIN_PASSWORD_LENGTH;
  const hasConfirmPassword = confirmNewPassword.length > 0;
  const passwordsMatch = newPassword === confirmNewPassword;

  const canSubmit =
    hasCurrentPassword &&
    hasNewPasswordMinLength &&
    hasConfirmPassword &&
    passwordsMatch;
  const activeOtherSessions = sessions.filter(
    (session) => !session.isCurrent && session.status === "Active",
  ).length;

  const loadSessions = useCallback(async () => {
    setSessionsLoading(true);
    setSessionsError(null);
    try {
      setSessions(await getSessions());
    } catch (reason: unknown) {
      if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) {
        onUnauthorized();
        return;
      }
      setSessionsError(getAccountErrorMessage(reason));
    } finally {
      setSessionsLoading(false);
    }
  }, [onUnauthorized]);

  useEffect(() => {
    void loadSessions();
  }, [loadSessions]);

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
      await onLogout();
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

  async function handleConfirmSessionAction() {
    if (!sessionConfirmation) {
      return;
    }

    const actionKey = sessionConfirmation.kind === "others"
      ? "others"
      : sessionConfirmation.session.id;
    setPendingSessionAction(actionKey);
    setSessionsError(null);
    setSessionsMessage(null);

    try {
      if (sessionConfirmation.kind === "others") {
        const result = await revokeOtherSessions();
        setSessionsMessage(
          result.revokedCount === 1
            ? "1 other session was revoked."
            : `${result.revokedCount} other sessions were revoked.`,
        );
        setSessionConfirmation(null);
        await loadSessions();
        return;
      }

      const result = await revokeSession(sessionConfirmation.session.id);
      setSessionConfirmation(null);
      if (result.isCurrent) {
        await onLogout();
        return;
      }

      setSessionsMessage(result.revoked ? "Session revoked." : "Session was already inactive.");
      await loadSessions();
    } catch (reason: unknown) {
      if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) {
        onUnauthorized();
        return;
      }
      setSessionsError(getAccountErrorMessage(reason));
    } finally {
      setPendingSessionAction(null);
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

      <AdminTwoFactorPanel onUnauthorized={onUnauthorized} />

      <section className="bg-card border border-border">
        <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border p-5">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              Active sessions
            </h2>
            <p className="mt-1 max-w-2xl text-[10px] text-muted-foreground font-sans">
              Review browsers that can refresh this admin login. Token values and hashes are never shown.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <AdminActionButton
              icon={<RefreshCw size={12} aria-hidden="true" />}
              disabled={sessionsLoading}
              onClick={() => void loadSessions()}
            >
              Refresh
            </AdminActionButton>
            <AdminActionButton
              variant="danger"
              disabled={sessionsLoading || activeOtherSessions === 0}
              onClick={() => setSessionConfirmation({ kind: "others" })}
            >
              Revoke other sessions
            </AdminActionButton>
          </div>
        </div>

        {sessionsError ? (
          <div role="alert" className="mx-5 mt-5 border border-destructive/30 bg-destructive/5 px-4 py-3 text-[11px] text-destructive font-sans">
            {sessionsError}
          </div>
        ) : null}
        {sessionsMessage ? (
          <div role="status" className="mx-5 mt-5 border border-emerald-200 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700 font-sans">
            {sessionsMessage}
          </div>
        ) : null}

        <div className="space-y-3 p-5">
          {sessionsLoading ? <AdminTableState message="Loading active sessions..." isLoading /> : null}
          {!sessionsLoading && sessions.length === 0 ? (
            <AdminTableState message="No refresh sessions were found for this account." />
          ) : null}
          {!sessionsLoading
            ? sessions.map((session) => (
                <article key={session.id} className="border border-border bg-background p-4">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <Laptop size={14} className="text-muted-foreground" aria-hidden="true" />
                        <h3 className="text-[11px] font-medium text-foreground font-sans">
                          {describeUserAgent(session.userAgent)}
                        </h3>
                        {session.isCurrent ? (
                          <span className="rounded-full bg-accent/15 px-2 py-1 text-[9px] text-accent font-sans">
                            Current session
                          </span>
                        ) : null}
                        <span className={`rounded-full px-2 py-1 text-[9px] font-sans ${getSessionStatusClass(session.status)}`}>
                          {session.status}
                        </span>
                      </div>
                      <dl className="mt-3 grid grid-cols-1 gap-x-6 gap-y-2 text-[10px] font-sans sm:grid-cols-2 lg:grid-cols-4">
                        <SessionDetail label="Created" value={formatSessionDate(session.createdAtUtc)} />
                        <SessionDetail label="Last used" value={formatSessionDate(session.lastUsedAtUtc)} />
                        <SessionDetail label="Expires" value={formatSessionDate(session.expiresAtUtc)} />
                        <SessionDetail label="IP" value={session.createdByIp ?? "Not available"} />
                      </dl>
                      {session.revocationReason ? (
                        <p className="mt-3 text-[9px] text-muted-foreground font-sans">
                          Revocation reason: {formatRevocationReason(session.revocationReason)}
                        </p>
                      ) : null}
                    </div>
                    {session.status === "Current" || session.status === "Active" ? (
                      <AdminActionButton
                        variant="danger"
                        disabled={pendingSessionAction !== null}
                        onClick={() => setSessionConfirmation({ kind: "session", session })}
                      >
                        {session.isCurrent ? "Revoke this session" : "Revoke"}
                      </AdminActionButton>
                    ) : null}
                  </div>
                </article>
              ))
            : null}
        </div>
      </section>

      <div className="border border-amber-200 bg-amber-50 px-4 py-3 text-[10px] text-amber-800 font-sans flex gap-2">
        <AlertTriangle size={13} className="shrink-0 mt-0.5" />
        <p>
          Changing your password revokes all refresh sessions and signs you out. Sign in again with the new password.
        </p>
      </div>

      {sessionConfirmation ? (
        <AdminConfirmDialog
          title={sessionConfirmation.kind === "others" ? "Revoke other sessions?" : "Revoke session?"}
          description={
            sessionConfirmation.kind === "others"
              ? "Every active refresh session except this one will be revoked. Other browsers will need to sign in again."
              : sessionConfirmation.session.isCurrent
                ? "This refresh session will be revoked and you will be signed out of Admin."
                : "This browser session will no longer be able to refresh its access token."
          }
          confirmLabel={sessionConfirmation.kind === "others" ? "Revoke other sessions" : "Revoke session"}
          isBusy={pendingSessionAction !== null}
          onCancel={() => setSessionConfirmation(null)}
          onConfirm={() => void handleConfirmSessionAction()}
        />
      ) : null}
    </div>
  );
}

function SessionDetail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="uppercase tracking-[0.16em] text-muted-foreground">{label}</dt>
      <dd className="mt-0.5 text-foreground">{value}</dd>
    </div>
  );
}

function formatSessionDate(value: string | null): string {
  if (!value) {
    return "Not yet";
  }
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Not available" : date.toLocaleString();
}

function describeUserAgent(userAgent: string | null): string {
  if (!userAgent) {
    return "Unknown browser";
  }
  const browser = /Edg\//.test(userAgent)
    ? "Microsoft Edge"
    : /Firefox\//.test(userAgent)
      ? "Firefox"
      : /Chrome\//.test(userAgent)
        ? "Chrome"
        : /Safari\//.test(userAgent)
          ? "Safari"
          : "Browser";
  const device = /iPhone|iPad/.test(userAgent)
    ? "iOS"
    : /Android/.test(userAgent)
      ? "Android"
      : /Windows/.test(userAgent)
        ? "Windows"
        : /Macintosh/.test(userAgent)
          ? "macOS"
          : "device";
  return `${browser} on ${device}`;
}

function getSessionStatusClass(status: AdminSession["status"]): string {
  if (status === "Current") return "bg-emerald-100 text-emerald-700";
  if (status === "Active") return "bg-sky-100 text-sky-700";
  if (status === "Revoked") return "bg-rose-100 text-rose-700";
  return "bg-slate-100 text-slate-600";
}

function formatRevocationReason(reason: string): string {
  return reason.replace(/_/g, " ");
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
