"use client";

import { createContext, useState, useEffect, useCallback, useRef, ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { AUTH_EXPIRED_EVENT } from "@/lib/api";
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
  const queryClient = useQueryClient();
  // Bumped by every restore attempt and by sign-in: only the latest restore may touch the state. A slow /me answer
  // that arrives after the user signed in (or after a newer attempt) is ignored.
  const restoreGeneration = useRef(0);
  // Several protected calls can fail with 401 together: only the first one ends the session and navigates.
  const expiryHandled = useRef(false);

  // The session is an httpOnly cookie the page cannot read: ask the API who is signed in. State is only set once
  // the answer arrives, and only if nothing newer (a sign-in, a retry) happened meanwhile.
  const restoreSession = useCallback((isCancelled: () => boolean = () => false) => {
    const generation = ++restoreGeneration.current;
    const isCurrent = () => !isCancelled() && generation === restoreGeneration.current;
    return fetchCurrentUser() // null = 401 = really signed out
      .then((current) => {
        if (!isCurrent()) return;
        if (current) expiryHandled.current = false;
        setUser(current);
      })
      .catch(() => {
        // An outage is not a sign-out: keep everything as is and let the user retry.
        if (isCurrent()) setSessionUnavailable(true);
      })
      .finally(() => {
        if (isCurrent()) setIsLoading(false);
      });
  }, []);

  const retrySession = () => {
    setIsLoading(true);
    setSessionUnavailable(false);
    void restoreSession();
  };

  useEffect(() => {
    let cancelled = false;
    void restoreSession(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, [restoreSession]);

  // The API answered 401 to a protected call: the session is over. The interceptor only announces it.
  useEffect(() => {
    const onExpired = () => {
      if (expiryHandled.current) return;
      expiryHandled.current = true;
      // Any restore still in flight is obsolete, and an outage banner has nothing left to retry.
      restoreGeneration.current++;
      setUser(null);
      setIsLoading(false);
      setSessionUnavailable(false);
      // Nothing of the previous user may stay in memory for the next sign-in on this tab.
      queryClient.clear();
      router.push("/login");
    };
    window.addEventListener(AUTH_EXPIRED_EVENT, onExpired);
    return () => window.removeEventListener(AUTH_EXPIRED_EVENT, onExpired);
  }, [router, queryClient]);

  // A successful sign-in proves the API is reachable and settles the session question, so any restore still in
  // flight (or a pending Retry) is obsolete and must not overwrite it.
  const signedIn = (current: User) => {
    restoreGeneration.current++;
    expiryHandled.current = false;
    setUser(current);
    setSessionUnavailable(false);
    setIsLoading(false);
    router.push("/dashboard");
  };

  const login = async (email: string, password: string) => {
    signedIn(await loginRequest({ email, password }));
  };

  const register = async (email: string, password: string, role: string) => {
    signedIn(await registerRequest({ email, password, role: role as UserRole }));
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
    queryClient.clear();
    router.push("/login");
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        // While the session is unknown, report "loading" so protected pages wait instead of redirecting to /login.
        isLoading: isLoading || sessionUnavailable,
        login,
        register,
        logout,
        logoutError,
        isAuthenticated: !!user,
      }}
    >
      {sessionUnavailable && (
        // A banner, not a gate: public pages keep working during an outage or a cold start.
        <div role="alert" className="flex items-center justify-center gap-4 border-b p-3 text-center text-sm">
          <p>Can&apos;t reach the server right now. Your session is not affected.</p>
          <button
            type="button"
            className="rounded-md border px-3 py-1"
            onClick={retrySession}
          >
            Retry
          </button>
        </div>
      )}
      {children}
    </AuthContext.Provider>
  );
}
