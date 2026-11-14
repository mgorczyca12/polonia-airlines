import { queryOptions } from '@tanstack/react-query'
import { getFares } from '#/shared/api'
import { toFare } from './fare.model'

export const fareKeys = {
  all: ['fares'] as const,
  forFlights: (flightIds: number[]) =>
    [...fareKeys.all, 'flights', flightIds] as const,
}

/** Current indicative prices for a page of flights. Offer creation remains authoritative. */
export function faresQuery(flightIds: number[]) {
  return queryOptions({
    queryKey: fareKeys.forFlights(flightIds),
    queryFn: async ({ signal }) =>
      flightIds.length === 0
        ? []
        : (await getFares(flightIds, signal)).map(toFare),
  })
}
