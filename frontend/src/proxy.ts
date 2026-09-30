import { NextRequest, NextResponse } from "next/server";

/**
 * Header the API uses to recognise requests that came through this frontend's /api proxy. It carries the shared
 * secret FRONTEND_PROXY_KEY (server-only env var, also configured on the API). Only a request that proves it came
 * through the frontend gets its extra X-Forwarded-For hop trusted by the API; anyone calling the API directly
 * cannot know the key, so a spoofed X-Forwarded-For cannot evade per-IP rate limits. Keep the name in sync with
 * FrontendProxyKey.HeaderName in the API.
 */
export const FRONTEND_PROXY_KEY_HEADER = "X-Frontend-Proxy-Key";

/**
 * Runs before the /api/* rewrite (see next.config.js). Always replaces whatever the browser sent in the key header
 * and adds the real key when FRONTEND_PROXY_KEY is set, so the header is never client-controlled.
 * Read at request time: it can change without a rebuild.
 */
export function proxy(request: NextRequest): NextResponse {
  const headers = new Headers(request.headers);
  headers.delete(FRONTEND_PROXY_KEY_HEADER);

  const key = process.env.FRONTEND_PROXY_KEY;
  if (key) headers.set(FRONTEND_PROXY_KEY_HEADER, key);

  return NextResponse.next({ request: { headers } });
}

export const config = {
  matcher: "/api/:path*",
};
