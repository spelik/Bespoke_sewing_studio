import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  getMe,
  login as requestLogin,
  logout as requestLogout,
  verifyTwoFactor as requestTwoFactorVerification,
  type AdminUser,
} from "../../api/authApi";
import {
  clearAccessToken,
  setAccessToken,
} from "../../api/authTokenStore";

interface AuthContextValue {
  user: AdminUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login(email: string, password: string): Promise<"authenticated" | "requiresTwoFactor">;
  verifyTwoFactor(code: string): Promise<void>;
  logout(): Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AdminUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const logout = useCallback(async () => {
    try {
      await requestLogout();
    } catch {
      // Local sign-out must still complete when the API is unavailable.
    } finally {
      clearAccessToken();
      setUser(null);
      setIsLoading(false);
    }
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await requestLogin(email, password);
    if ("requiresTwoFactor" in response) {
      clearAccessToken();
      setUser(null);
      setIsLoading(false);
      return "requiresTwoFactor" as const;
    }

    setAccessToken(response.accessToken);
    setUser(response.user);
    setIsLoading(false);
    return "authenticated" as const;
  }, []);

  const verifyTwoFactor = useCallback(async (code: string) => {
    const response = await requestTwoFactorVerification(code);
    setAccessToken(response.accessToken);
    setUser(response.user);
    setIsLoading(false);
  }, []);

  useEffect(() => {
    let active = true;

    getMe()
      .then((currentUser) => {
        if (active) {
          setUser(currentUser);
        }
      })
      .catch(() => {
        if (active) {
          clearAccessToken();
          setUser(null);
        }
      })
      .finally(() => {
        if (active) {
          setIsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      login,
      verifyTwoFactor,
      logout,
    }),
    [isLoading, login, logout, user, verifyTwoFactor],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return context;
}
