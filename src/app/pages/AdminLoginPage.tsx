import { type FormEvent, useState } from "react";
import { LockKeyhole } from "lucide-react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { ApiError } from "../../api/apiClient";
import { useAuth } from "../auth/AuthContext";

interface LoginLocationState {
  from?: string;
}

export function AdminLoginPage() {
  const { isAuthenticated, isLoading, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!isLoading && isAuthenticated) {
    return <Navigate to="/admin" replace />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      await login(email, password);
      const state = location.state as LoginLocationState | null;
      navigate(state?.from === "/admin" ? state.from : "/admin", { replace: true });
    } catch (submitError) {
      if (submitError instanceof ApiError && submitError.status === 401) {
        setError("The email or password is incorrect.");
      } else if (submitError instanceof ApiError) {
        setError(submitError.message);
      } else {
        setError("Sign in could not be completed. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="pt-[72px] min-h-screen bg-[#F5F0E8] flex items-center justify-center px-5 py-12">
      <section className="w-full max-w-md bg-card border border-border p-7 sm:p-10">
        <div className="w-10 h-10 border border-border bg-secondary/60 flex items-center justify-center mb-6 text-foreground">
          <LockKeyhole size={17} />
        </div>
        <p className="text-[10px] tracking-[0.28em] uppercase text-accent font-sans mb-2">
          Studio Administration
        </p>
        <h1 className="font-serif text-[2rem] font-light text-foreground">Admin sign in</h1>
        <p className="text-[12px] text-muted-foreground mt-2 mb-7 font-sans">
          Use your studio administrator account to manage enquiries.
        </p>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div>
            <label htmlFor="admin-email" className="block text-[11px] text-foreground mb-2 font-sans">
              Email
            </label>
            <input
              id="admin-email"
              type="email"
              autoComplete="username"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors"
            />
          </div>
          <div>
            <label htmlFor="admin-password" className="block text-[11px] text-foreground mb-2 font-sans">
              Password
            </label>
            <input
              id="admin-password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors"
            />
          </div>

          {error ? (
            <p role="alert" className="border border-destructive/20 bg-destructive/5 px-4 py-3 text-[11px] text-destructive font-sans">
              {error}
            </p>
          ) : null}

          <button
            type="submit"
            disabled={isSubmitting || isLoading}
            className="w-full bg-foreground text-primary-foreground px-5 py-3 text-[11px] tracking-[0.14em] uppercase hover:bg-accent disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isSubmitting ? "Signing in..." : "Sign in"}
          </button>
        </form>
      </section>
    </div>
  );
}
