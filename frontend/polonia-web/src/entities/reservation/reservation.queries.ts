import { queryOptions } from '@tanstack/react-query'
import { getReservation, recoverReservation } from '#/shared/api'

export const reservationKeys = {
  all: ['reservations'] as const,
  byRequest: (offerId: string) =>
    [...reservationKeys.all, 'by-request', offerId.toLowerCase()] as const,
  detail: (reservationId: number) =>
    [...reservationKeys.all, 'detail', reservationId] as const,
}

/**
 * The reservation already committed for an offer, or `null` if none exists yet.
 * Always refetched on mount because a lost response may have committed meanwhile.
 */
export function reservationByRequestQuery(offerId: string) {
  return queryOptions({
    queryKey: reservationKeys.byRequest(offerId),
    queryFn: ({ signal }) => recoverReservation(offerId, signal),
    staleTime: 0,
  })
}

export function reservationQuery(reservationId: number) {
  return queryOptions({
    queryKey: reservationKeys.detail(reservationId),
    queryFn: ({ signal }) => getReservation(reservationId, signal),
    staleTime: 0,
  })
}
