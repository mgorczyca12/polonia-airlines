import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { faresQuery } from '#/entities/fare'
import { flightSearchQuery } from '#/entities/flight'
import type { FlightSearchCriteria } from '#/shared/api'

/**
 * Flights for one result page and their current fares. While another page loads,
 * the previous page stays available as placeholder data.
 */
export function useFlightResults(criteria: FlightSearchCriteria) {
  const flights = useQuery({
    ...flightSearchQuery(criteria),
    placeholderData: keepPreviousData,
  })
  const flightIds = flights.data?.map((flight) => flight.id) ?? []
  const fares = useQuery({
    ...faresQuery(flightIds),
    enabled: flights.isSuccess,
    placeholderData: keepPreviousData,
  })

  const retry = () => {
    void (flights.isError ? flights.refetch() : fares.refetch())
  }

  return {
    flights: flights.data,
    fares: fares.data,
    error: flights.error ?? fares.error,
    isFirstLoad: !flights.data || !fares.data,
    isUpdating: flights.isPlaceholderData || fares.isPlaceholderData,
    retry,
  }
}
