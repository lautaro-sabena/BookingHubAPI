// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { useContext } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const { push, fetchCurrentUser, logoutRequest, loginRequest, registerRequest } = vi.hoisted(() => ({
  push: vi.fn(),
  fetchCurrentUser: vi.fn(),
  logoutRequest: vi.fn(),
  loginRequest: vi.fn(),
  registerRequest: vi.fn(),
}));
vi.mock('next/navigation', () => ({ useRouter: () => ({ push }) }));
vi.mock('@/lib/auth', () => ({
  fetchCurrentUser,
  logoutRequest,
  loginRequest,
  registerRequest,
}));

import { AUTH_EXPIRED_EVENT } from '@/lib/api';
import { AuthContext, AuthProvider } from '../AuthProvider';

const user = { id: 'u1', email: 'a@b.com', role: 'Owner' as const, companyId: 'c1' };

function Probe() {
  const auth = useContext(AuthContext)!;
  return (
    <div>
      <span data-testid="state">
        {auth.isLoading ? 'loading' : auth.isAuthenticated ? `in:${auth.user?.email}` : 'out'}
      </span>
      <span data-testid="logout-error">{String(auth.logoutError)}</span>
      <button onClick={() => void auth.logout()}>logout</button>
      <button onClick={() => void auth.login("a@b.com", "pw")}>login</button>
      <button onClick={() => void auth.register("a@b.com", "pw", "Owner")}>register</button>
    </div>
  );
}

let queryClient: QueryClient;

function renderProvider() {
  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <Probe />
      </AuthProvider>
    </QueryClientProvider>
  );
}

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient();
  });
  afterEach(cleanup);

  describe('session restore', () => {
    it('signs the user in when the API reports a session', async () => {
      fetchCurrentUser.mockResolvedValue(user);
      renderProvider();

      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));
    });

    it('treats a missing session (401 -> null) as signed out, without an outage screen', async () => {
      fetchCurrentUser.mockResolvedValue(null);
      renderProvider();

      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));
      expect(screen.queryByRole('alert')).toBeNull();
    });

    it('keeps the app rendered with a retry banner on an outage, reporting auth as still loading', async () => {
      fetchCurrentUser.mockRejectedValueOnce(new Error('502')).mockResolvedValueOnce(user);
      renderProvider();

      const retry = await screen.findByRole('button', { name: 'Retry' });
      expect(screen.getByRole('alert').textContent).toContain("Can't reach the server");
      // Public pages keep working; protected pages see "loading" and do not redirect to /login.
      expect(screen.getByTestId('state').textContent).toBe('loading');
      expect(push).not.toHaveBeenCalled();

      retry.click();

      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));
      expect(fetchCurrentUser).toHaveBeenCalledTimes(2);
    });
  });

  it('clears the outage state after a successful login, so auth stops reporting loading', async () => {
    fetchCurrentUser.mockRejectedValue(new Error('502'));
    loginRequest.mockResolvedValue(user);
    renderProvider();
    await screen.findByRole('button', { name: 'Retry' });

    screen.getByText('login').click();

    await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('clears the outage state after a successful register, so auth stops reporting loading', async () => {
    fetchCurrentUser.mockRejectedValue(new Error('502'));
    registerRequest.mockResolvedValue(user);
    renderProvider();
    await screen.findByRole('button', { name: 'Retry' });
    expect(screen.getByTestId('state').textContent).toBe('loading');

    screen.getByText('register').click();

    await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));
    expect(screen.queryByRole('alert')).toBeNull();
    expect(push).toHaveBeenCalledWith('/dashboard');
  });

  describe('a restore that answers after the user signed in', () => {
    function deferred<T>() {
      let resolve!: (value: T) => void;
      let reject!: (reason: unknown) => void;
      const promise = new Promise<T>((res, rej) => {
        resolve = res;
        reject = rej;
      });
      return { promise, resolve, reject };
    }

    it('is ignored when it says "no session" (it must not sign the user out again)', async () => {
      const restore = deferred<typeof user | null>();
      fetchCurrentUser.mockReturnValue(restore.promise);
      loginRequest.mockResolvedValue(user);
      renderProvider();

      screen.getByText('login').click();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));

      restore.resolve(null);
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(screen.getByTestId('state').textContent).toBe('in:a@b.com');
    });

    it('is ignored when it fails (no outage banner, auth not stuck loading)', async () => {
      const restore = deferred<typeof user | null>();
      fetchCurrentUser.mockReturnValue(restore.promise);
      loginRequest.mockResolvedValue(user);
      renderProvider();

      screen.getByText('login').click();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));

      restore.reject(new Error('502'));
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(screen.queryByRole('alert')).toBeNull();
      expect(screen.getByTestId('state').textContent).toBe('in:a@b.com');
    });

    it('is ignored when it comes from a Retry that the sign-in overtook', async () => {
      fetchCurrentUser.mockRejectedValueOnce(new Error('502'));
      renderProvider();
      const retry = await screen.findByRole('button', { name: 'Retry' });

      const late = deferred<typeof user | null>();
      fetchCurrentUser.mockReturnValueOnce(late.promise);
      loginRequest.mockResolvedValue(user);
      retry.click();
      screen.getByText('login').click();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));

      late.resolve(null);
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(screen.getByTestId('state').textContent).toBe('in:a@b.com');
    });
  });

  it('signs out and goes to /login when a protected call reports an expired session', async () => {
    fetchCurrentUser.mockResolvedValue(user);
    renderProvider();
    await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));

    act(() => {
      window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT));
    });

    await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));
    expect(push).toHaveBeenCalledWith('/login');
  });

  describe('an expired session', () => {
    function deferred<T>() {
      let resolve!: (value: T) => void;
      const promise = new Promise<T>((res) => {
        resolve = res;
      });
      return { promise, resolve };
    }

    const expire = () =>
      act(() => {
        window.dispatchEvent(new Event(AUTH_EXPIRED_EVENT));
      });

    it('navigates once when several calls expire together', async () => {
      fetchCurrentUser.mockResolvedValue(user);
      renderProvider();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));

      expire();
      expire();
      expire();

      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));
      expect(push).toHaveBeenCalledTimes(1);
      expect(push).toHaveBeenCalledWith('/login');
    });

    it('handles a later expiry again after the user signed back in', async () => {
      fetchCurrentUser.mockResolvedValue(user);
      loginRequest.mockResolvedValue(user);
      renderProvider();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));
      expire();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));

      screen.getByText('login').click();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));
      push.mockClear();
      expire();

      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));
      expect(push).toHaveBeenCalledWith('/login');
    });

    it('clears the query cache', async () => {
      fetchCurrentUser.mockResolvedValue(user);
      renderProvider();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));
      queryClient.setQueryData(['reservations', 'u1'], [{ id: 'r1' }]);

      expire();

      expect(queryClient.getQueryData(['reservations', 'u1'])).toBeUndefined();
    });

    it('discards a restore still in flight and clears the loading and outage state', async () => {
      const restore = deferred<typeof user | null>();
      fetchCurrentUser.mockReturnValue(restore.promise);
      renderProvider();
      expect(screen.getByTestId('state').textContent).toBe('loading');

      expire();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));

      // The late answer would sign the expired user back in.
      restore.resolve(user);
      await new Promise((resolve) => setTimeout(resolve, 0));
      expect(screen.getByTestId('state').textContent).toBe('out');
    });

    it('clears the outage banner state, so auth stops reporting loading', async () => {
      fetchCurrentUser.mockRejectedValue(new Error('502'));
      renderProvider();
      await screen.findByRole('button', { name: 'Retry' });

      expire();

      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));
      expect(screen.queryByRole('alert')).toBeNull();
    });
  });

  describe('logout', () => {
    async function signedIn() {
      fetchCurrentUser.mockResolvedValue(user);
      renderProvider();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('in:a@b.com'));
    }

    it('signs out and goes to /login once the server confirmed', async () => {
      logoutRequest.mockResolvedValue(undefined);
      await signedIn();

      screen.getByText('logout').click();

      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));
      expect(push).toHaveBeenCalledWith('/login');
      expect(screen.getByTestId('logout-error').textContent).toBe('false');
    });

    it('clears the query cache after a confirmed logout, but not after a failed one', async () => {
      logoutRequest.mockRejectedValueOnce(new Error('network')).mockResolvedValueOnce(undefined);
      await signedIn();
      queryClient.setQueryData(['reservations', 'u1'], [{ id: 'r1' }]);

      screen.getByText('logout').click();
      await waitFor(() => expect(screen.getByTestId('logout-error').textContent).toBe('true'));
      expect(queryClient.getQueryData(['reservations', 'u1'])).toBeDefined();

      screen.getByText('logout').click();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));
      expect(queryClient.getQueryData(['reservations', 'u1'])).toBeUndefined();
    });

    it('stays signed in, reports the error and does not reject when the server call fails', async () => {
      logoutRequest.mockRejectedValue(new Error('network'));
      const unhandled = vi.fn();
      process.on('unhandledRejection', unhandled);
      await signedIn();

      screen.getByText('logout').click();

      await waitFor(() => expect(screen.getByTestId('logout-error').textContent).toBe('true'));
      expect(screen.getByTestId('state').textContent).toBe('in:a@b.com');
      expect(push).not.toHaveBeenCalled();
      await new Promise((resolve) => setTimeout(resolve, 0));
      process.off('unhandledRejection', unhandled);
      expect(unhandled).not.toHaveBeenCalled();
    });

    it('clears the error and signs out when a retry succeeds', async () => {
      logoutRequest.mockRejectedValueOnce(new Error('network')).mockResolvedValueOnce(undefined);
      await signedIn();

      screen.getByText('logout').click();
      await waitFor(() => expect(screen.getByTestId('logout-error').textContent).toBe('true'));

      screen.getByText('logout').click();
      await waitFor(() => expect(screen.getByTestId('state').textContent).toBe('out'));
      expect(screen.getByTestId('logout-error').textContent).toBe('false');
      expect(push).toHaveBeenCalledWith('/login');
    });
  });
});
