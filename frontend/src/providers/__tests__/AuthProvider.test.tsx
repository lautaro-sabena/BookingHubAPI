// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { useContext } from 'react';

const { push, fetchCurrentUser, logoutRequest, loginRequest } = vi.hoisted(() => ({
  push: vi.fn(),
  fetchCurrentUser: vi.fn(),
  logoutRequest: vi.fn(),
  loginRequest: vi.fn(),
}));
vi.mock('next/navigation', () => ({ useRouter: () => ({ push }) }));
vi.mock('@/lib/auth', () => ({
  fetchCurrentUser,
  logoutRequest,
  loginRequest,
  registerRequest: vi.fn(),
}));

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
    </div>
  );
}

function renderProvider() {
  return render(
    <AuthProvider>
      <Probe />
    </AuthProvider>
  );
}

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
