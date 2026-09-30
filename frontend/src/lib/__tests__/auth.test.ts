import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AxiosError } from 'axios';

const { get, post } = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }));
vi.mock('../api', () => ({ default: { get, post } }));

import { fetchCurrentUser, loginRequest, logoutRequest, registerRequest } from '../auth';

const user = { id: 'u1', email: 'a@b.com', role: 'Owner' as const, companyId: 'c1' };

function httpError(status: number) {
  return new AxiosError('failed', 'ERR_BAD_REQUEST', undefined, undefined, { status } as never);
}

describe('auth requests (httpOnly cookie session)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('fetchCurrentUser', () => {
    it('returns the user the API reports for the session cookie', async () => {
      get.mockResolvedValue({ data: user });

      await expect(fetchCurrentUser()).resolves.toEqual(user);
      expect(get).toHaveBeenCalledWith('/auth/me');
    });

    it('returns null when there is no session (401)', async () => {
      get.mockRejectedValue(httpError(401));

      await expect(fetchCurrentUser()).resolves.toBeNull();
    });

    it('rethrows anything that is not a 401, so an outage is not mistaken for a sign-out', async () => {
      const serverError = httpError(500);
      get.mockRejectedValue(serverError);

      await expect(fetchCurrentUser()).rejects.toBe(serverError);
    });

    it('rethrows network failures', async () => {
      const networkError = new Error('Network Error');
      get.mockRejectedValue(networkError);

      await expect(fetchCurrentUser()).rejects.toBe(networkError);
    });
  });

  describe('loginRequest / registerRequest', () => {
    it('posts the credentials and returns the user, with no token involved', async () => {
      post.mockResolvedValue({ data: user });

      const result = await loginRequest({ email: 'a@b.com', password: 'secret1' });

      expect(post).toHaveBeenCalledWith('/auth/login', { email: 'a@b.com', password: 'secret1' });
      expect(result).toEqual(user);
      expect(result).not.toHaveProperty('token');
    });

    it('posts the registration and returns the user', async () => {
      post.mockResolvedValue({ data: user });

      const result = await registerRequest({ email: 'a@b.com', password: 'secret1', role: 'Owner' });

      expect(post).toHaveBeenCalledWith('/auth/register', { email: 'a@b.com', password: 'secret1', role: 'Owner' });
      expect(result).toEqual(user);
    });

    it('lets API errors reach the caller (the form shows the message)', async () => {
      const unauthorized = httpError(401);
      post.mockRejectedValue(unauthorized);

      await expect(loginRequest({ email: 'a@b.com', password: 'bad' })).rejects.toBe(unauthorized);
    });
  });

  describe('logoutRequest', () => {
    it('asks the API to clear the cookie', async () => {
      post.mockResolvedValue({ status: 204 });

      await logoutRequest();

      expect(post).toHaveBeenCalledWith('/auth/logout');
    });
  });
});
