import flightData from '#/data/ord-krk-flights.json'
import type { Flight } from '../model/flight.types'

const flights = flightData as Flight[]

export interface FlightSearchFilters {
  from?: string
  to?: string
}

export function getFlights(filters: FlightSearchFilters = {}): Flight[] {
  return flights.filter((flight) => {
    const matchesDeparture = !filters.from || flight.departureAirport.airportCode === filters.from
    const matchesArrival = !filters.to || flight.arrivalAirport.airportCode === filters.to
    return matchesDeparture && matchesArrival
  })
}

export function getFlightByNumber(flightNumber: string): Flight | undefined {
  return flights.find((flight) => flight.flightNumber === flightNumber)
}