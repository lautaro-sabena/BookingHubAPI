"use client";

import { createContext, useContext, useState, useEffect, ReactNode } from "react";
import { useRouter } from "next/navigation";
import api, { setAuthToken } from "@/lib/api";
import { initializeAuth, removeStoredToken, setStoredToken } from "@/lib/auth";
import { User, AuthResponse, LoginRequest, RegisterRequest } from "@/types";

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, role: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export { AuthContext };
export type { AuthContextType };

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const token = initializeAuth();
    if (token) {
      const storedUser = localStorage.getItem("user");
      if (storedUser) {
        setUser(JSON.parse(storedUser));
      }
    }
    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    const response = await api.post<AuthResponse>("/auth/login", {
      email,
      password,
    } as LoginRequest);

    const { token, userId, email: userEmail, role } = response.data;
    
    setStoredToken(token);
    const userData: User = { id: userId, email: userEmail, role };
    setUser(userData);
    localStorage.setItem("user", JSON.stringify(userData));

    router.push(role === "Owner" ? "/dashboard" : "/services");
  };

  const register = async (email: string, password: string, role: string) => {
    const response = await api.post<AuthResponse>("/auth/register", {
      email,
      password,
      role,
    } as RegisterRequest);

    const { token, userId, email: userEmail, role: userRole } = response.data;
    
    setStoredToken(token);
    const userData: User = { id: userId, email: userEmail, role: userRole as "Owner" | "Customer" };
    setUser(userData);
    localStorage.setItem("user", JSON.stringify(userData));

    router.push(userData.role === "Owner" ? "/dashboard" : "/services");
  };

  const logout = () => {
    removeStoredToken();
    setUser(null);
    localStorage.removeItem("user");
    router.push("/login");
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
