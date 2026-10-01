"use client";

import { createContext, useState, useEffect, useCallback, ReactNode } from "react";
import { useRouter } from "next/navigation";
import { fetchCurrentUser, loginRequest, logoutRequest, registerRequest } from "@/lib/auth";
import { User, UserRole } from "@/types";

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, role: string) => Promise<void>;
  /** Never rejects. On failure the user stays signed in and `logoutError` is set; calling it again retries. */
  logout: () => Promise<void>;
  /** True when the last logout attempt failed, so the session cookie may still be valid. */
  logoutError: boolean;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export { AuthContext };
export type { AuthContextType };

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  // The API could not be reached (network, 5xx, cold start): we do not know whether a session exists.
  const [sessionUnavailable, setSessionUnavailable] = useState(false);
  const [logoutError, setLogoutError] = useState(false);
  const router = useRouter();

  // The session is an httpOnly cookie the page cannot read: ask the API who is signed in.
  const restoreSession = useCallback(async (isCancelled: () => boolean = () => false) => {
    setIsLoading(true);
    setSessionUnavailable(false);
    try {
      const current = await fetchCurrentUser(); // null = 401 = really signed out
      if (!isCancelled()) setUser(current);
    } catch {
      // An outage is not a sign-out: keep everything as is and let the user retry.
      if (!isCancelled()) setSessionUnavailable(true);
    } finally {
      if (!isCancelled()) setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    void restoreSession(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, [restoreSession]);

  const login = async (email: string, password: string) => {
    setUser(await loginRequest({ email, password }));
    router.push("/dashboard");
  };

  const register = async (email: string, password: string, role: string) => {
    setUser(await registerRequest({ email, password, role: role as UserRole }));
    router.push("/dashboard");
  };

  const logout = async () => {
    setLogoutError(false);
    try {
      await logoutRequest();
    } catch {
      // The httpOnly cookie is still valid: do not pretend to be signed out, or the next load restores the
      // session silently (dangerous on a shared computer). Stay signed in and let the user retry.
      setLogoutError(true);
      return;
    }
    setUser(null);
    router.push("/login");
  };

  if (sessionUnavailable) {
    return (
      <div role="alert" className="flex h-screen flex-col items-center justify-center gap-4 p-6 text-center">
        <p>Can&apos;t reach the server right now. Your session is not affected.</p>
        <button
          type="button"
          className="rounded-md border px-4 py-2 text-sm"
          onClick={() => void restoreSession()}
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        login,
        register,
        logout,
        logoutError,
        isAuthenticated: !!user,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
