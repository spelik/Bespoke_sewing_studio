import { type FormEvent, useCallback, useEffect, useState } from "react";
import { KeyRound, RefreshCw, ShieldCheck, ShieldOff } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import { setAccessToken } from "../../api/authTokenStore";
import {
  beginTwoFactorSetup,
  disableTwoFactor,
  enableTwoFactor,
  getAccountErrorMessage,
  getTwoFactorStatus,
  resetAuthenticator,
  resetTwoFactorRecoveryCodes,
  type TwoFactorSetup,
  type TwoFactorStatus,
} from "../../api/authApi";
import { AdminActionButton, AdminConfirmDialog, AdminTableState } from "./AdminUi";

interface AdminTwoFactorPanelProps {
  onUnauthorized(): void;
}

type SecurityAction = "disable" | "recovery-codes" | "authenticator";

export function AdminTwoFactorPanel({ onUnauthorized }: AdminTwoFactorPanelProps) {
  const [status, setStatus] = useState<TwoFactorStatus | null>(null);
  const [setup, setSetup] = useState<Omit<TwoFactorSetup, "token"> | null>(null);
  const [verificationCode, setVerificationCode] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [confirmation, setConfirmation] = useState<SecurityAction | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const handleError = useCallback((reason: unknown) => {
    if (reason instanceof ApiError && (reason.status === 401 || reason.status === 403)) {
      onUnauthorized();
      return;
    }
    setError(getAccountErrorMessage(reason));
  }, [onUnauthorized]);

  const loadStatus = useCallback(async () => {
    setPendingAction("status");
    setError(null);
    try {
      setStatus(await getTwoFactorStatus());
    } catch (reason: unknown) {
      handleError(reason);
    } finally {
      setPendingAction(null);
    }
  }, [handleError]);

  useEffect(() => {
    void loadStatus();
  }, [loadStatus]);

  async function handleSetup() {
    setPendingAction("setup");
    setError(null);
    setMessage(null);
    setRecoveryCodes([]);
    try {
      const result = await beginTwoFactorSetup();
      setAccessToken(result.token.accessToken);
      setSetup({
        sharedKey: result.sharedKey,
        authenticatorUri: result.authenticatorUri,
        issuer: result.issuer,
        accountName: result.accountName,
      });
    } catch (reason: unknown) {
      handleError(reason);
    } finally {
      setPendingAction(null);
    }
  }

  async function handleEnable(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPendingAction("enable");
    setError(null);
    setMessage(null);
    try {
      const result = await enableTwoFactor(verificationCode);
      if (!result.status.isEnabled) {
        throw new ApiError("Two-factor authentication was not enabled.", 500);
      }
      if (result.recoveryCodes.length === 0) {
        throw new ApiError(
          "Two-factor authentication could not be enabled because recovery codes were not returned.",
          500,
        );
      }
      setAccessToken(result.token.accessToken);
      setStatus(result.status);
      setRecoveryCodes(result.recoveryCodes);
      setSetup(null);
      setVerificationCode("");
      setMessage("Two-factor authentication is enabled. Save the recovery codes now; they will not be shown again.");
    } catch (reason: unknown) {
      handleError(reason);
    } finally {
      setPendingAction(null);
    }
  }

  async function handleSecurityAction() {
    if (!confirmation) return;

    const action = confirmation;
    setPendingAction(action);
    setConfirmation(null);
    setError(null);
    setMessage(null);
    setRecoveryCodes([]);
    try {
      if (action === "disable") {
        const result = await disableTwoFactor(currentPassword);
        setAccessToken(result.token.accessToken);
        setStatus(result.status);
        setSetup(null);
        setMessage("Two-factor authentication is disabled. Future sign-ins require only the account password.");
      } else if (action === "recovery-codes") {
        const result = await resetTwoFactorRecoveryCodes(currentPassword);
        if (result.recoveryCodes.length === 0) {
          throw new ApiError("New recovery codes were not returned.", 500);
        }
        setAccessToken(result.token.accessToken);
        setRecoveryCodes(result.recoveryCodes);
        setStatus((current) => current
          ? { ...current, recoveryCodesRemaining: result.recoveryCodesRemaining }
          : current);
        setMessage("New recovery codes were generated. Previous recovery codes no longer work.");
      } else {
        const result = await resetAuthenticator(currentPassword);
        setAccessToken(result.token.accessToken);
        setSetup({
          sharedKey: result.sharedKey,
          authenticatorUri: result.authenticatorUri,
          issuer: result.issuer,
          accountName: result.accountName,
        });
        setStatus({ isEnabled: false, hasAuthenticatorKey: true, recoveryCodesRemaining: 0 });
        setMessage("The authenticator was reset. Complete setup again to re-enable 2FA.");
      }
      setCurrentPassword("");
    } catch (reason: unknown) {
      handleError(reason);
    } finally {
      setPendingAction(null);
    }
  }

  return (
    <section className="border border-border bg-card">
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border p-5">
        <div>
          <h2 className="font-serif text-[1.15rem] font-light text-foreground">
            Two-factor authentication
          </h2>
          <p className="mt-1 max-w-2xl text-[10px] text-muted-foreground font-sans">
            Protect this admin account with a TOTP authenticator app and one-time recovery codes.
          </p>
        </div>
        {status ? (
          <span className={`inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-[10px] font-sans ${
            status.isEnabled ? "bg-emerald-100 text-emerald-700" : "bg-slate-100 text-slate-600"
          }`}>
            {status.isEnabled ? <ShieldCheck size={12} /> : <ShieldOff size={12} />}
            {status.isEnabled ? "Enabled" : "Disabled"}
          </span>
        ) : null}
      </div>

      <div className="space-y-5 p-5">
        {pendingAction === "status" ? <AdminTableState message="Loading 2FA status..." isLoading /> : null}
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

        {recoveryCodes.length > 0 ? (
          <div className="border border-amber-200 bg-amber-50 p-4">
            <h3 className="text-[11px] font-medium text-amber-900 font-sans">Save these recovery codes now</h3>
            <p className="mt-1 text-[10px] text-amber-800 font-sans">
              Each code works once. They will not be shown again after this page state is cleared.
            </p>
            <div className="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-2">
              {recoveryCodes.map((code) => (
                <code key={code} className="border border-amber-200 bg-white px-3 py-2 text-[11px] text-foreground">
                  {code}
                </code>
              ))}
            </div>
          </div>
        ) : null}

        {status && !status.isEnabled && !setup ? (
          <div>
            <p className="mb-3 text-[10px] text-muted-foreground font-sans">
              Setup displays a manual key and an otpauth URI only for this authenticated account.
            </p>
            <AdminActionButton
              variant="primary"
              icon={<ShieldCheck size={12} />}
              isLoading={pendingAction === "setup"}
              onClick={() => void handleSetup()}
            >
              Set up two-factor authentication
            </AdminActionButton>
          </div>
        ) : null}

        {setup ? (
          <form onSubmit={handleEnable} className="space-y-4 border border-border bg-background p-4">
            <div>
              <h3 className="text-[11px] font-medium text-foreground font-sans">Authenticator setup</h3>
              <p className="mt-1 text-[10px] text-muted-foreground font-sans">
                Add the account manually in your authenticator app, then verify the current six-digit code.
              </p>
            </div>
            <dl className="space-y-3 text-[10px] font-sans">
              <div>
                <dt className="text-muted-foreground">Manual setup key</dt>
                <dd className="mt-1 break-all border border-border bg-card px-3 py-2 font-mono text-foreground">{setup.sharedKey}</dd>
              </div>
              <div>
                <dt className="text-muted-foreground">Authenticator URI</dt>
                <dd className="mt-1 break-all border border-border bg-card px-3 py-2 font-mono text-[9px] text-foreground">{setup.authenticatorUri}</dd>
              </div>
            </dl>
            <label className="block text-[10px] tracking-wide text-muted-foreground font-sans">
              Authentication code
              <input
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                required
                value={verificationCode}
                onChange={(event) => setVerificationCode(event.target.value)}
                className="mt-1 w-full max-w-xs border border-border bg-card px-3 py-2 font-mono text-[11px] tracking-[0.18em] text-foreground focus:outline-none focus:border-accent"
              />
            </label>
            <div className="flex flex-wrap gap-2">
              <AdminActionButton type="submit" variant="primary" isLoading={pendingAction === "enable"}>
                Verify and enable
              </AdminActionButton>
              {!status?.isEnabled ? (
                <AdminActionButton onClick={() => { setSetup(null); setVerificationCode(""); }}>
                  Cancel
                </AdminActionButton>
              ) : null}
            </div>
          </form>
        ) : null}

        {status?.isEnabled ? (
          <div className="space-y-4">
            <p className="text-[10px] text-muted-foreground font-sans">
              Recovery codes remaining: <strong className="text-foreground">{status.recoveryCodesRemaining}</strong>
            </p>
            <label className="block max-w-md text-[10px] tracking-wide text-muted-foreground font-sans">
              Current password for security changes
              <input
                type="password"
                autoComplete="current-password"
                value={currentPassword}
                onChange={(event) => setCurrentPassword(event.target.value)}
                className="mt-1 w-full border border-border bg-background px-3 py-2 text-[11px] text-foreground focus:outline-none focus:border-accent"
              />
            </label>
            <div className="flex flex-wrap gap-2">
              <AdminActionButton
                icon={<RefreshCw size={12} />}
                disabled={!currentPassword || pendingAction !== null}
                onClick={() => setConfirmation("recovery-codes")}
              >
                Generate new recovery codes
              </AdminActionButton>
              <AdminActionButton
                variant="danger"
                icon={<KeyRound size={12} />}
                disabled={!currentPassword || pendingAction !== null}
                onClick={() => setConfirmation("authenticator")}
              >
                Reset authenticator
              </AdminActionButton>
              <AdminActionButton
                variant="danger"
                icon={<ShieldOff size={12} />}
                disabled={!currentPassword || pendingAction !== null}
                onClick={() => setConfirmation("disable")}
              >
                Disable 2FA
              </AdminActionButton>
            </div>
          </div>
        ) : null}
      </div>

      {confirmation ? (
        <AdminConfirmDialog
          title={confirmation === "disable"
            ? "Disable two-factor authentication?"
            : confirmation === "authenticator"
              ? "Reset authenticator?"
              : "Generate new recovery codes?"}
          description={confirmation === "disable"
            ? "Password-only sign-in will be allowed again."
            : confirmation === "authenticator"
              ? "The current authenticator will stop working and 2FA will be disabled until setup is completed again."
              : "All existing recovery codes will stop working."}
          confirmLabel={confirmation === "disable"
            ? "Disable 2FA"
            : confirmation === "authenticator"
              ? "Reset authenticator"
              : "Generate codes"}
          isBusy={pendingAction !== null}
          onCancel={() => setConfirmation(null)}
          onConfirm={() => void handleSecurityAction()}
        />
      ) : null}
    </section>
  );
}
