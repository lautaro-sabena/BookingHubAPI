import { describe, it, expect, afterEach } from 'vitest';
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

  describe('rewrites', () => {
    const original = process.env.BACKEND_URL;
    afterEach(() => {
      if (original === undefined) delete process.env.BACKEND_URL;
      else process.env.BACKEND_URL = original;
    });

    it('proxies /api/* to BACKEND_URL so the browser only talks to the frontend origin', async () => {
      process.env.BACKEND_URL = 'https://api.example.com';

      expect(await nextConfig.rewrites()).toEqual([
        { source: '/api/:path*', destination: 'https://api.example.com/api/:path*' },
      ]);
    });

    it('ignores a trailing slash in BACKEND_URL', async () => {
      process.env.BACKEND_URL = 'https://api.example.com/';

      const [rule] = await nextConfig.rewrites();

      expect(rule.destination).toBe('https://api.example.com/api/:path*');
    });

    it('defaults to the local API', async () => {
      delete process.env.BACKEND_URL;

      const [rule] = await nextConfig.rewrites();

      expect(rule.destination).toBe('http://localhost:5000/api/:path*');
    });
  });
});
