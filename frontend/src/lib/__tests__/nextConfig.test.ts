import { describe, it, expect } from 'vitest';
const nextConfig = require('../../../next.config.js');

describe('next.config headers', () => {
  it('sends the security headers on every route', async () => {
    const rules = await nextConfig.headers();

    expect(rules).toHaveLength(1);
    expect(rules[0].source).toBe('/:path*');

    const headers = Object.fromEntries(
      rules[0].headers.map((h: { key: string; value: string }) => [h.key, h.value]),
    );
    expect(headers).toMatchObject({
      'X-Content-Type-Options': 'nosniff',
      'X-Frame-Options': 'DENY',
      'Referrer-Policy': 'strict-origin-when-cross-origin',
      'Permissions-Policy': 'camera=(), microphone=(), geolocation=()',
    });
    expect(headers['Content-Security-Policy']).toBe(
      "frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self'",
    );
  });

  it('keeps standalone output and hides the framework header', () => {
    expect(nextConfig.output).toBe('standalone');
    expect(nextConfig.poweredByHeader).toBe(false);
  });
});
