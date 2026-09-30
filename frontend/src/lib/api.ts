import axios, { AxiosError } from "axios";

/**
 * Header the API requires on state-changing requests that use the session cookie (CSRF defence, see the
 * API's CsrfHeaderMiddleware). A cross-site page cannot add a custom header without a CORS preflight.
 */
export const CSRF_HEADER_NAME = "X-Requested-With";
export const CSRF_HEADER_VALUE = "BookingHub";

/**
 * Relative on purpose: the browser only talks to this app's origin and Next.js proxies /api/* to the backend
 * (see `rewrites` in next.config.js). That keeps the httpOnly session cookie first-party. The session token is
 * never visible to scripts, so there is nothing to attach here.
 */
export const api = axios.create({
  baseURL: "/api",
  headers: {
    "Content-Type": "application/json",
    [CSRF_HEADER_NAME]: CSRF_HEADER_VALUE,
  },
});

// A 401 from these calls is an expected answer ("wrong password", "no session yet"), not an expired session.
const AUTH_ENDPOINTS = ["/auth/login", "/auth/register", "/auth/me", "/auth/logout"];
const AUTH_PAGES = ["/login", "/register"];

/**
 * Whether a 401 means "the session ended, go sign in again". False for the auth endpoints themselves and when the
 * user is already on a sign-in page, so restoring the session on /login can never redirect to /login in a loop.
 */
export function shouldRedirectToLogin(requestUrl: string | undefined, pathname: string): boolean {
  const path = (requestUrl ?? "").split("?")[0];
  if (AUTH_ENDPOINTS.some((endpoint) => path.endsWith(endpoint))) return false;
  return !AUTH_PAGES.includes(pathname);
}

/**
 * Fired on `window` when a protected call answers 401 outside the sign-in pages. AuthProvider listens for it, drops
 * the signed-in user and navigates with the Next router, so this module never assigns `window.location`.
 */
export const AUTH_EXPIRED_EVENT = "bookinghub:auth-expired";

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (
      error.response?.status === 401 &&
      typeof window !== "undefined" &&
      shouldRedirectToLogin(error.config?.url, window.location.pathname)
    ) {
      window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT));
    }
    return Promise.reject(error);
  }
);

export default api;
