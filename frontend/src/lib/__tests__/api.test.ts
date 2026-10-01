import { describe, it, expect } from 'vitest';
import type { InternalAxiosRequestConfig } from 'axios';
import { api, shouldRedirectToLogin, CSRF_HEADER_NAME, CSRF_HEADER_VALUE } from '../api';

/** Runs a request through the real interceptors and returns the config that reached the network layer. */
async function sentConfig(method: 'get' | 'post' | 'delete', url: string) {
  let sent: InternalAxiosRequestConfig | undefined;
  await api.request({
    method,
    url,
    adapter: async (config) => {
      sent = config;
      return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
    },
  });
  return sent!;
}

describe('API client', () => {
  it('calls the same origin under /api, so the session cookie is first-party', () => {
    expect(api.defaults.baseURL).toBe('/api');
  });

  it.each(['get', 'post', 'delete'] as const)('sends the CSRF header on every %s request', async (method) => {
    const config = await sentConfig(method, '/services');

    expect(config.headers.get(CSRF_HEADER_NAME)).toBe(CSRF_HEADER_VALUE);
  });

  it('never attaches an Authorization header (the token is an httpOnly cookie)', async () => {
    const config = await sentConfig('get', '/services');

    expect(config.headers.get('Authorization')).toBeUndefined();
  });
});

describe('shouldRedirectToLogin', () => {
  it('redirects when a normal call returns 401 away from the sign-in pages', () => {
    expect(shouldRedirectToLogin('/reservations', '/dashboard')).toBe(true);
    expect(shouldRedirectToLogin('/services?page=2', '/services')).toBe(true);
  });

  it.each(['/auth/me', '/auth/login', '/auth/register', '/auth/logout'])(
    'does not redirect for a 401 from %s (expected answers, and restoring the session must not loop)',
    (url) => {
      expect(shouldRedirectToLogin(url, '/dashboard')).toBe(false);
    },
  );

  it.each(['/login', '/register'])('does not redirect when already on %s', (pathname) => {
    expect(shouldRedirectToLogin('/reservations', pathname)).toBe(false);
  });

  it('tolerates a request without a url', () => {
    expect(shouldRedirectToLogin(undefined, '/dashboard')).toBe(true);
  });
});
