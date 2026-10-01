"use client";

import { createContext, useState, useEffect, ReactNode } from "react";
import { useRouter } from "next/navigation";
import { fetchCurrentUser, loginRequest, logoutRequest, registerRequest } from "@/lib/auth";
import { User, UserRole } from "@/types";

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, role: string) => Promise<void>;
  logout: () => Promise<void>;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export { AuthContext };
export type { AuthContextType };

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  // The session is an httpOnly cookie the page cannot read: ask the API who is signed in.
  useEffect(() => {
    let cancelled = false;
    fetchCurrentUser()
      .then((current) => {
        if (!cancelled) setUser(current);
      })
      .catch(() => {
        // API unreachable: treat as signed out; the next request will surface the real error.
        if (!cancelled) setUser(null);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const login = async (email: string, password: string) => {
    setUser(await loginRequest({ email, password }));
    router.push("/dashboard");
  };

  const register = async (email: string, password: string, role: string) => {
    setUser(await registerRequest({ email, password, role: role as UserRole }));
    router.push("/dashboard");
  };

  const logout = async () => {
    try {
      await logoutRequest();
    } finally {
      // Signed out locally even when the call fails; the cookie then simply expires with the token.
      setUser(null);
      router.push("/login");
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        login,
        register,
        logout,
        isAuthenticated: !!user,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
