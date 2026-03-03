import { describe, it, expect, beforeEach, vi } from 'vitest';
import { getStoredToken, setStoredToken, removeStoredToken, initializeAuth } from '../auth';

describe('auth utilities', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  describe('getStoredToken', () => {
    it('should return null when no token exists', () => {
      expect(getStoredToken()).toBeNull();
    });

    it('should return token when it exists', () => {
      localStorage.setItem('auth_token', 'test-token-123');
      expect(getStoredToken()).toBe('test-token-123');
    });
  });

  describe('setStoredToken', () => {
    it('should store token in localStorage', () => {
      setStoredToken('new-token-456');
      expect(localStorage.getItem('auth_token')).toBe('new-token-456');
    });
  });

  describe('removeStoredToken', () => {
    it('should remove token from localStorage', () => {
      localStorage.setItem('auth_token', 'test-token');
      removeStoredToken();
      expect(localStorage.getItem('auth_token')).toBeNull();
    });
  });

  describe('initializeAuth', () => {
    it('should return null in server-side context', () => {
      const result = initializeAuth();
      expect(result).toBeNull();
    });

    it('should return token when it exists', () => {
      localStorage.setItem('auth_token', 'initial-token');
      const result = initializeAuth();
      expect(result).toBe('initial-token');
    });
  });
});
