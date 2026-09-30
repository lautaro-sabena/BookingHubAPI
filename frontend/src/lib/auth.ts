import { isAxiosError } from "axios";
import api from "./api";
import { AuthResponse, LoginRequest, RegisterRequest, User } from "@/types";

/*
 * The session JWT lives in an httpOnly cookie set by the API on login/register and cleared on logout. Scripts
 * cannot read it, so nothing here stores or decodes a token: the cookie rides along on same-origin requests and
 * the signed-in user always comes from the API.
 */

/** The signed-in user, or null when there is no (valid) session. Other failures (network, 5xx) are thrown. */
export async function fetchCurrentUser(): Promise<User | null> {
  try {
    const response = await api.get<User>("/auth/me");
    return response.data;
  } catch (error) {
    if (isAxiosError(error) && error.response?.status === 401) return null;
    throw error;
  }
}

export async function loginRequest(credentials: LoginRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>("/auth/login", credentials);
  return response.data;
}

export async function registerRequest(details: RegisterRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>("/auth/register", details);
  return response.data;
}

export async function logoutRequest(): Promise<void> {
  await api.post("/auth/logout");
}
