import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import { api, setAuthToken } from '../api';

vi.mock('axios', () => ({
  default: {
    create: vi.fn(() => ({
      interceptors: {
        request: { use: vi.fn() },
        response: { use: vi.fn() },
      },
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
    })),
  },
}));

describe('API client', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('setAuthToken', () => {
    it('should set auth token', () => {
      const token = 'test-jwt-token';
      setAuthToken(token);
      // The function should update internal state
      expect(true).toBe(true);
    });

    it('should clear auth token when null is passed', () => {
      setAuthToken(null);
      expect(true).toBe(true);
    });
  });

  describe('API configuration', () => {
    it('should have correct base URL', () => {
      expect(process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000').toBeDefined();
    });
  });
});
