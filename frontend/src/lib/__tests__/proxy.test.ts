import { describe, it, expect, afterEach } from 'vitest';
import { NextRequest } from 'next/server';
import { proxy, config, FRONTEND_PROXY_KEY_HEADER } from '../../proxy';

// NextResponse.next({ request: { headers } }) reports the forwarded request headers as x-middleware-request-*.
const forwarded = (response: Response, name: string) =>
  response.headers.get(`x-middleware-request-${name.toLowerCase()}`);
const overrides = (response: Response) => response.headers.get('x-middleware-override-headers') ?? '';

describe('proxy (adds the frontend proxy key to /api requests)', () => {
  const original = process.env.FRONTEND_PROXY_KEY;
  afterEach(() => {
    if (original === undefined) delete process.env.FRONTEND_PROXY_KEY;
    else process.env.FRONTEND_PROXY_KEY = original;
  });

  it('only applies to /api/*', () => {
    expect(config.matcher).toBe('/api/:path*');
  });

  it('sets the key header from the server-only env var', () => {
    process.env.FRONTEND_PROXY_KEY = 's3cret';

    const response = proxy(new NextRequest('http://localhost/api/auth/me'));

    expect(forwarded(response, FRONTEND_PROXY_KEY_HEADER)).toBe('s3cret');
  });

  it('replaces a key the browser tried to send', () => {
    process.env.FRONTEND_PROXY_KEY = 's3cret';

    const response = proxy(
      new NextRequest('http://localhost/api/auth/me', { headers: { [FRONTEND_PROXY_KEY_HEADER]: 'forged' } }),
    );

    expect(forwarded(response, FRONTEND_PROXY_KEY_HEADER)).toBe('s3cret');
  });

  it('drops a browser-supplied key when no key is configured', () => {
    delete process.env.FRONTEND_PROXY_KEY;

    const response = proxy(
      new NextRequest('http://localhost/api/auth/me', { headers: { [FRONTEND_PROXY_KEY_HEADER]: 'forged' } }),
    );

    expect(forwarded(response, FRONTEND_PROXY_KEY_HEADER)).toBeNull();
    expect(overrides(response).toLowerCase()).not.toContain(FRONTEND_PROXY_KEY_HEADER.toLowerCase());
  });
});
