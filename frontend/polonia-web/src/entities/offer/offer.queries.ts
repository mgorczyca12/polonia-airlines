import { queryOptions } from '@tanstack/react-query'
import { getOffer } from '#/shared/api'

export const offerKeys = {
  all: ['offers'] as const,
  detail: (offerId: string) => [...offerKeys.all, offerId] as const,
}

export function offerQuery(offerId: string) {
  return queryOptions({
    queryKey: offerKeys.detail(offerId),
    queryFn: ({ signal }) => getOffer(offerId, signal),
  })
}

/** Milliseconds until the offer expires; zero or negative once expired. */
export function offerTimeRemaining(
  expiresAt: string,
  now = Date.now(),
): number {
  return Date.parse(expiresAt) - now
}
