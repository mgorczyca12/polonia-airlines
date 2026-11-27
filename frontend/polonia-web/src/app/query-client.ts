import { QueryClient } from '@tanstack/react-query'

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60_000,
        // The web API owns retries so attempts are not multiplied across layers.
        retry: false,
        refetchOnWindowFocus: false,
      },
      mutations: {
        // Submissions recover explicitly through their idempotent request identity.
        retry: false,
      },
    },
  })
}
