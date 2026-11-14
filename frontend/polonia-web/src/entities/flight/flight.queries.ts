import { queryOptions } from '@tanstack/react-query'
import { getFlight, searchFlights } from '#/shared/api'
import type { FlightSearchCriteria } from '#/shared/api'
import { toFlightSummary } from './flight.model'

export const flightKeys = {
  all: ['flights'] as const,
  search: (criteria: FlightSearchCriteria) =>
    [...flightKeys.all, 'search', criteria] as const,
  detail: (flightId: string) =>
    [...flightKeys.all, 'detail', flightId] as const,
}

export function flightSearchQuery(criteria: FlightSearchCriteria) {
  return queryOptions({
    queryKey: flightKeys.search(criteria),
    queryFn: async ({ signal }) =>
      (await searchFlights(criteria, signal)).map(toFlightSummary),
  })
}

export function flightQuery(flightId: string) {
  return queryOptions({
    queryKey: flightKeys.detail(flightId),
    queryFn: async ({ signal }) =>
      toFlightSummary(await getFlight(flightId, signal)),
  })
}
