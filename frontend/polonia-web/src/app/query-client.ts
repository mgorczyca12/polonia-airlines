import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '#/shared/api'

const MAX_QUERY_RETRIES = 2

/** Client errors (4xx) are deterministic, so only transient failures are retried. */
function shouldRetry(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError && error.status < 500) {
    return false
  }

  return failureCount < MAX_QUERY_RETRIES
}

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60_000,
        retry: shouldRetry,
        refetchOnWindowFocus: false,
      },
      mutations: {
        // Submissions recover explicitly through their idempotent request identity.
        retry: false,
      },
    },
  })
}
