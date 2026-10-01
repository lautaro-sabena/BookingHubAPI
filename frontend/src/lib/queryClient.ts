import { QueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";

const MAX_QUERY_RETRIES = 2;

/**
 * Retry transient failures (network, 5xx) a couple of times, never a 4xx: the answer would be the same
 * (404 "no company yet", 403, 401 handled by the auth flow).
 */
export function shouldRetryQuery(failureCount: number, error: unknown): boolean {
  if (isAxiosError(error) && error.response && error.response.status >= 400 && error.response.status < 500) {
    return false;
  }
  return failureCount < MAX_QUERY_RETRIES;
}

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60 * 1000,
        refetchOnWindowFocus: false,
        retry: shouldRetryQuery,
      },
      // A failed write is reported to the user; repeating it automatically could double-book.
      mutations: { retry: false },
    },
  });
}
