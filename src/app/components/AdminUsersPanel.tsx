import { type FormEvent, useEffect, useMemo, useState } from "react";
import { AlertTriangle, KeyRound, LoaderCircle, Trash2, UserPlus, Users } from "lucide-react";
import { ApiError } from "../../api/apiClient";
import {
  createAdminUser,
  deleteAdminUser,
  getAdminUsers,
  getAdminUsersErrorMessage,
  resetAdminUserPassword,
  setAdminUserDisabled,
  type ManagedAdminUser,
} from "../../api/adminUsersApi";

interface AdminUsersPanelProps {
  onUnauthorized(): void;
}

type PendingAction =
  | "load"
  | "create"
  | `status:${string}`
  | `reset:${string}`
  | `delete:${string}`
  | null;

const MIN_PASSWORD_LENGTH = 12;

export function AdminUsersPanel({ onUnauthorized }: AdminUsersPanelProps) {
  const [users, setUsers] = useState<ManagedAdminUser[]>([]);
  const [newEmail, setNewEmail] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [resetPasswordByUserId, setResetPasswordByUserId] = useState<Record<string, string>>({});
  const [pendingAction, setPendingAction] = useState<PendingAction>("load");
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<ManagedAdminUser | null>(null);

  useEffect(() => {
    void loadUsers();
  }, []);

  const activeCount = useMemo(
    () => users.filter((user) => !user.isDisabled).length,
    [users],
  );

  async function loadUsers() {
    setPendingAction("load");
    setError(null);
    try {
      setUsers(await getAdminUsers());
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setPendingAction(null);
    }
  }

  async function handleCreateUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPendingAction("create");
    setError(null);
    setMessage(null);

    try {
      const created = await createAdminUser(newEmail, newPassword);
      setUsers((current) => sortUsers([...current, created]));
      setNewEmail("");
      setNewPassword("");
      setMessage(`Admin user ${created.email} was created.`);
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setPendingAction(null);
    }
  }

  async function handleSetDisabled(user: ManagedAdminUser, isDisabled: boolean) {
    setPendingAction(`status:${user.id}`);
    setError(null);
    setMessage(null);

    try {
      const updated = await setAdminUserDisabled(user.id, isDisabled);
      replaceUser(updated);
      setMessage(`${updated.email} is now ${updated.isDisabled ? "disabled" : "active"}.`);
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setPendingAction(null);
    }
  }

  async function handleResetPassword(user: ManagedAdminUser) {
    const password = resetPasswordByUserId[user.id] ?? "";
    setPendingAction(`reset:${user.id}`);
    setError(null);
    setMessage(null);

    try {
      const updated = await resetAdminUserPassword(user.id, password);
      replaceUser(updated);
      setResetPasswordByUserId((current) => ({ ...current, [user.id]: "" }));
      setMessage(`Password was reset for ${updated.email}.`);
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setPendingAction(null);
    }
  }

  async function handleConfirmDeleteUser() {
    if (!deleteCandidate) {
      return;
    }

    const user = deleteCandidate;
    setPendingAction(`delete:${user.id}`);
    setError(null);
    setMessage(null);

    try {
      await deleteAdminUser(user.id);
      setUsers((current) => current.filter((item) => item.id !== user.id));
      setMessage(`Admin user ${user.email} was deleted.`);
      setDeleteCandidate(null);
    } catch (reason: unknown) {
      handleRequestError(reason);
    } finally {
      setPendingAction(null);
    }
  }

  function replaceUser(user: ManagedAdminUser) {
    setUsers((current) =>
      sortUsers(current.map((item) => (item.id === user.id ? user : item))),
    );
  }

  function handleRequestError(reason: unknown) {
    if (
      reason instanceof ApiError &&
      (reason.status === 401 || reason.status === 403)
    ) {
      onUnauthorized();
      return;
    }

    setError(getAdminUsersErrorMessage(reason));
  }

  return (
    <div className="space-y-6">
      <section className="bg-card border border-border">
        <div className="p-5 border-b border-border flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="font-serif text-[1.15rem] font-light text-foreground">
              Admin users
            </h2>
            <p className="text-[10px] text-muted-foreground font-sans mt-1 max-w-2xl">
              Manage who can sign in to the admin area. All users currently receive full Admin access.
            </p>
          </div>
          <button
            type="button"
            onClick={() => void loadUsers()}
            disabled={pendingAction === "load"}
            className="px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50 font-sans"
          >
            Refresh
          </button>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 p-5 border-b border-border">
          <AdminUsersSummaryCard label="Active admins" value={activeCount} />
          <AdminUsersSummaryCard label="Total users" value={users.length} />
          <AdminUsersSummaryCard
            label="Access model"
            value="Admin"
            caption="Role-based editing can be added later."
          />
        </div>

        <form onSubmit={handleCreateUser} className="p-5 border-b border-border space-y-4">
          <div>
            <h3 className="text-[11px] font-medium tracking-wide text-foreground font-sans">
              Create admin user
            </h3>
            <p className="text-[10px] text-muted-foreground font-sans mt-1">
              Enter an email and temporary password. The password is never shown again.
            </p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-[1fr_1fr_auto] gap-3 items-end">
            <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
              Email
              <input
                type="email"
                value={newEmail}
                onChange={(event) => setNewEmail(event.target.value)}
                required
                maxLength={256}
                className="mt-1 w-full border border-border bg-background px-3 py-2 text-[11px] text-foreground focus:outline-none focus:border-accent"
              />
            </label>
            <label className="text-[10px] tracking-wide text-muted-foreground font-sans">
              Temporary password
              <input
                type="password"
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                required
                minLength={MIN_PASSWORD_LENGTH}
                className="mt-1 w-full border border-border bg-background px-3 py-2 text-[11px] text-foreground focus:outline-none focus:border-accent"
              />
            </label>
            <button
              type="submit"
              disabled={pendingAction === "create"}
              className="inline-flex items-center justify-center gap-2 px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50 font-sans"
            >
              {pendingAction === "create" ? <LoaderCircle size={12} className="animate-spin" /> : <UserPlus size={12} />}
              Create user
            </button>
          </div>
        </form>

        {error ? (
          <div role="alert" className="mx-5 mt-5 border border-destructive/30 bg-destructive/5 px-4 py-3 text-[11px] text-destructive font-sans">
            {error}
          </div>
        ) : null}
        {message ? (
          <div role="status" className="mx-5 mt-5 border border-emerald-200 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700 font-sans">
            {message}
          </div>
        ) : null}

        <div className="p-5">
          {pendingAction === "load" ? (
            <div className="flex items-center gap-2 text-[11px] text-muted-foreground font-sans">
              <LoaderCircle size={14} className="animate-spin" /> Loading admin users...
            </div>
          ) : null}

          {pendingAction !== "load" && users.length === 0 ? (
            <div className="border border-dashed border-border p-6 text-[11px] text-muted-foreground font-sans">
              No admin users were found.
            </div>
          ) : null}

          {users.length > 0 ? (
            <div className="overflow-x-auto border border-border">
              <table className="w-full text-left text-[11px] font-sans">
                <thead className="bg-muted/40 text-muted-foreground uppercase tracking-wide text-[9px]">
                  <tr>
                    <th className="px-3 py-2 font-medium">User</th>
                    <th className="px-3 py-2 font-medium">Status</th>
                    <th className="px-3 py-2 font-medium">Roles</th>
                    <th className="px-3 py-2 font-medium">Reset password</th>
                    <th className="px-3 py-2 font-medium text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {users.map((user) => {
                    const resetPassword = resetPasswordByUserId[user.id] ?? "";
                    const rowPending = pendingAction?.endsWith(user.id) ?? false;
                    return (
                      <tr key={user.id} className="align-top">
                        <td className="px-3 py-3 min-w-[220px]">
                          <div className="font-medium text-foreground flex items-center gap-2">
                            <Users size={12} />
                            {user.email}
                          </div>
                          {user.isCurrentUser ? (
                            <div className="text-[9px] text-muted-foreground mt-1">Current session</div>
                          ) : null}
                        </td>
                        <td className="px-3 py-3">
                          <span className={`inline-flex items-center rounded-full px-2 py-1 text-[9px] ${user.isDisabled ? "bg-slate-100 text-slate-600" : "bg-emerald-100 text-emerald-700"}`}>
                            {user.isDisabled ? "Disabled" : "Active"}
                          </span>
                        </td>
                        <td className="px-3 py-3 text-muted-foreground">
                          {user.roles.length > 0 ? user.roles.join(", ") : "—"}
                        </td>
                        <td className="px-3 py-3 min-w-[260px]">
                          <div className="flex flex-wrap gap-2">
                            <input
                              type="password"
                              value={resetPassword}
                              minLength={MIN_PASSWORD_LENGTH}
                              placeholder="New temporary password"
                              onChange={(event) =>
                                setResetPasswordByUserId((current) => ({
                                  ...current,
                                  [user.id]: event.target.value,
                                }))
                              }
                              className="min-w-[180px] flex-1 border border-border bg-background px-3 py-2 text-[10px] text-foreground focus:outline-none focus:border-accent"
                            />
                            <button
                              type="button"
                              onClick={() => void handleResetPassword(user)}
                              disabled={rowPending || resetPassword.length < MIN_PASSWORD_LENGTH}
                              className="inline-flex items-center gap-2 px-3 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50"
                            >
                              <KeyRound size={11} /> Reset
                            </button>
                          </div>
                        </td>
                        <td className="px-3 py-3">
                          <div className="flex flex-wrap items-center justify-end gap-2">
                            <button
                              type="button"
                              onClick={() => void handleSetDisabled(user, !user.isDisabled)}
                              disabled={rowPending || !user.canDisable}
                              className="px-3 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50"
                              title={!user.canDisable ? "This action is protected for safety." : undefined}
                            >
                              {user.isDisabled ? "Enable" : "Disable"}
                            </button>
                            <button
                              type="button"
                              onClick={() => setDeleteCandidate(user)}
                              disabled={rowPending || !user.canDelete}
                              className="inline-flex items-center gap-2 px-3 py-2 text-[10px] tracking-wide border border-destructive/30 text-destructive bg-background hover:border-destructive disabled:opacity-50"
                              title={!user.canDelete ? "This action is protected for safety." : undefined}
                            >
                              <Trash2 size={11} /> Delete
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ) : null}
        </div>
      </section>

      <div className="border border-amber-200 bg-amber-50 px-4 py-3 text-[10px] text-amber-800 font-sans flex gap-2">
        <AlertTriangle size={13} className="shrink-0 mt-0.5" />
        <p>
          Keep at least one active admin user. Disabled users cannot sign in, and passwords are never returned by the API.
        </p>
      </div>

      {deleteCandidate ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4 py-8"
          role="dialog"
          aria-modal="true"
          aria-labelledby="delete-admin-user-title"
        >
          <div className="w-full max-w-md border border-border bg-card p-6 shadow-2xl">
            <div className="flex items-start gap-3">
              <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-destructive/30 bg-destructive/5 text-destructive">
                <Trash2 size={16} />
              </div>
              <div className="min-w-0">
                <h3
                  id="delete-admin-user-title"
                  className="font-serif text-[1.2rem] font-light text-foreground"
                >
                  Delete admin user?
                </h3>
                <p className="mt-2 text-[11px] leading-5 text-muted-foreground font-sans">
                  This will permanently remove
                  <span className="font-medium text-foreground"> {deleteCandidate.email}</span>
                  . This action cannot be undone.
                </p>
              </div>
            </div>

            <div className="mt-6 flex flex-wrap justify-end gap-2">
              <button
                type="button"
                onClick={() => setDeleteCandidate(null)}
                disabled={pendingAction === `delete:${deleteCandidate.id}`}
                className="px-4 py-2 text-[10px] tracking-wide border border-border bg-background hover:border-foreground disabled:opacity-50 font-sans"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => void handleConfirmDeleteUser()}
                disabled={pendingAction === `delete:${deleteCandidate.id}`}
                className="inline-flex items-center justify-center gap-2 px-4 py-2 text-[10px] tracking-wide border border-destructive/40 bg-destructive text-destructive-foreground hover:border-destructive disabled:opacity-50 font-sans"
              >
                {pendingAction === `delete:${deleteCandidate.id}` ? (
                  <LoaderCircle size={12} className="animate-spin" />
                ) : (
                  <Trash2 size={12} />
                )}
                Delete user
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function AdminUsersSummaryCard({
  label,
  value,
  caption,
}: {
  label: string;
  value: number | string;
  caption?: string;
}) {
  return (
    <div className="border border-border bg-background px-4 py-3">
      <div className="text-[9px] uppercase tracking-[0.22em] text-muted-foreground font-sans">
        {label}
      </div>
      <div className="mt-2 font-serif text-[1.4rem] font-light text-foreground">
        {value}
      </div>
      {caption ? <div className="mt-1 text-[10px] text-muted-foreground font-sans">{caption}</div> : null}
    </div>
  );
}

function sortUsers(users: ManagedAdminUser[]): ManagedAdminUser[] {
  return [...users].sort((left, right) => left.email.localeCompare(right.email));
}
