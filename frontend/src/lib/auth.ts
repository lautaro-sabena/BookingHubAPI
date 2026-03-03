import { setAuthToken } from "./api";

const TOKEN_KEY = "auth_token";

export const getStoredToken = (): string | null => {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
};

export const setStoredToken = (token: string): void => {
  if (typeof window === "undefined") return;
  localStorage.setItem(TOKEN_KEY, token);
  setAuthToken(token);
};

export const removeStoredToken = (): void => {
  if (typeof window === "undefined") return;
  localStorage.removeItem(TOKEN_KEY);
  setAuthToken(null);
};

export const initializeAuth = (): string | null => {
  const token = getStoredToken();
  if (token) {
    setAuthToken(token);
  }
  return token;
};
