/**
 * Security headers for every route.
 *
 * The CSP is intentionally limited to directives that need no nonces: it blocks framing, plugins,
 * <base> hijacking and cross-origin form posts. script-src / style-src / connect-src are NOT
 * restricted here, because Next.js injects inline scripts (hydration data, the theme toggle bootstrap)
 * and inline styles that a strict policy would break without a nonce-based setup.
 */
const securityHeaders = [
  { key: 'X-Content-Type-Options', value: 'nosniff' },
  { key: 'X-Frame-Options', value: 'DENY' },
  { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
  { key: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=()' },
  {
    key: 'Content-Security-Policy',
    value: "frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self'",
  },
];

/**
 * Where /api/* is proxied to. Server-side only (not NEXT_PUBLIC_*): the browser never learns the API origin.
 *
 * The browser only talks to this frontend's origin, so the API's httpOnly session cookie is first-party. Calling
 * the API origin directly would make it a third-party cookie, which Safari and Chrome block, and
 * bookinghubapi-frontend-*.onrender.com and the API are different sites (onrender.com is on the Public Suffix List).
 *
 * The rewrite destination is baked in at `next build`: BACKEND_URL must be set when the image is built (Docker
 * build arg), not only when it runs.
 */
function backendUrl() {
  return (process.env.BACKEND_URL || 'http://localhost:5000').replace(/\/+$/, '');
}

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  reactStrictMode: true,
  productionBrowserSourceMaps: false,
  images: {
    formats: ['image/avif', 'image/webp'],
    deviceSizes: [640, 750, 828, 1080, 1200, 1920],
    imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
  },
  compiler: {
    removeConsole: process.env.NODE_ENV === 'production',
  },
  poweredByHeader: false,
  compress: true,
  experimental: {
    // Rewrites are cut off after 30 s by default; a free Render API instance can need about a minute to wake up.
    proxyTimeout: 90_000,
  },
  async headers() {
    return [{ source: '/:path*', headers: securityHeaders }];
  },
  async rewrites() {
    return [{ source: '/api/:path*', destination: `${backendUrl()}/api/:path*` }];
  },
};

module.exports = nextConfig;
