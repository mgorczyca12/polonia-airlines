import { getCityName } from '#/entities/airport'
import type { FlightResponse } from '#/shared/api'
import { calendarDayOffset, toDateKey } from '#/shared/lib/formatting'

export interface FlightEndpoint {
  airportCode: string
  city: string
  /** Original UTC timestamp with offset, as returned by the API. */
  timestamp: string
  time: Date
}

/** Display model for one flight. Contains only data the flight API returns. */
export interface FlightSummary {
  id: number
  flightNumber: string
  departure: FlightEndpoint
  arrival: FlightEndpoint
  durationMinutes: number
  /** Calendar days between UTC departure and arrival, e.g. 1 for "+1". */
  arrivalDayOffset: number
  /** UTC calendar date of departure as `YYYY-MM-DD`. */
  departureDateKey: string
}

function toEndpoint(airportCode: string, timestamp: string): FlightEndpoint {
  return {
    airportCode,
    city: getCityName(airportCode),
    timestamp,
    time: new Date(timestamp),
  }
}

export function toFlightSummary(flight: FlightResponse): FlightSummary {
  const departure = toEndpoint(flight.departureAirport, flight.departureTime)
  const arrival = toEndpoint(flight.arrivalAirport, flight.arrivalTime)

  return {
    id: flight.id,
    flightNumber: flight.flightNumber,
    departure,
    arrival,
    durationMinutes: Math.round(
      (arrival.time.getTime() - departure.time.getTime()) / 60_000,
    ),
    arrivalDayOffset: calendarDayOffset(departure.time, arrival.time),
    departureDateKey: toDateKey(departure.time),
  }
}
