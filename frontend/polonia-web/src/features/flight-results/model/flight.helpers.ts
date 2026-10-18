import { getAirportByCode } from '#/entities/airport'
import {
  calendarDayOffset,
  parseScheduleDateTime,
  toDateKey,
} from '#/shared/lib/formatting'
import type { BrandedFare, Flight, FlightCabin } from './flight.types'

export interface FareSelection {
  flightNumber: string
  fareId: number
}

export type FlightSortOrder = 'recommended' | 'price' | 'duration' | 'departure'

export interface FlightTimes {
  departure: Date
  arrival: Date
  arrivalDayOffset: number
}

export function getCityName(airportCode: string): string {
  return getAirportByCode(airportCode)?.City ?? airportCode
}

export function getFlightTimes(flight: Flight): FlightTimes | null {
  const firstSegment = flight.segments.at(0)
  const lastSegment = flight.segments.at(-1)
  const departure = firstSegment
    ? parseScheduleDateTime(firstSegment.departureDateTimeUtc)
    : null
  const arrival = lastSegment
    ? parseScheduleDateTime(lastSegment.arrivalDateTimeUtc)
    : null

  if (!departure || !arrival) {
    return null
  }

  return {
    departure,
    arrival,
    arrivalDayOffset: calendarDayOffset(departure, arrival),
  }
}

export function getDepartureDateKey(flight: Flight): string | null {
  const times = getFlightTimes(flight)
  return times ? toDateKey(times.departure) : null
}

export function getFlightNumbersLabel(flight: Flight): string {
  return flight.segments.map((segment) => segment.flightNumber).join(' / ')
}

export function getAircraftLabel(flight: Flight): string {
  return [...new Set(flight.segments.map((segment) => segment.aircraftType))].join(' · ')
}

export function getStopsLabel(flight: Flight): string {
  if (flight.isNonStop || flight.layovers.length === 0) {
    return 'Non-stop'
  }

  const stopCount = flight.layovers.length
  const places = flight.layovers
    .map((layover) => {
      const city = getAirportByCode(layover.airportCode)?.City ?? layover.airportName
      return `${city} (${layover.airportCode})`
    })
    .join(', ')

  return `${stopCount} ${stopCount === 1 ? 'stop' : 'stops'} · ${places}`
}

export function findCabin(flight: Flight, cabinName: string): FlightCabin | undefined {
  return flight.brandedFares.find((cabin) => cabin.className === cabinName)
}

export function findFare(
  flight: Flight,
  fareId: number,
): { cabin: FlightCabin; fare: BrandedFare } | undefined {
  for (const cabin of flight.brandedFares) {
    const fare = cabin.fares.find((candidate) => candidate.fareId === fareId)

    if (fare) {
      return { cabin, fare }
    }
  }

  return undefined
}

function lowestPrice(fares: BrandedFare[]): number | null {
  return fares.length > 0
    ? Math.min(...fares.map((fare) => fare.farePriceUsd))
    : null
}

export function getLowestCabinFare(cabin: FlightCabin): number | null {
  return lowestPrice(cabin.fares)
}

/** Lowest fare in the preferred cabin, falling back to the lowest fare on the flight. */
export function getLowestFare(flight: Flight, preferredCabin?: string): number | null {
  const cabin = preferredCabin ? findCabin(flight, preferredCabin) : undefined
  const cabinPrice = cabin ? lowestPrice(cabin.fares) : null

  return cabinPrice ?? lowestPrice(flight.brandedFares.flatMap((item) => item.fares))
}

export function getRecommendedFlight(flights: Flight[]): Flight | undefined {
  return [...flights].sort(
    (first, second) =>
      first.totalDurationMinutes - second.totalDurationMinutes ||
      first.basePriceUsd - second.basePriceUsd,
  )[0]
}

export function sortFlights(
  flights: Flight[],
  order: FlightSortOrder,
  preferredCabin?: string,
): Flight[] {
  const departureTime = (flight: Flight) =>
    getFlightTimes(flight)?.departure.getTime() ?? Number.MAX_SAFE_INTEGER
  const price = (flight: Flight) =>
    getLowestFare(flight, preferredCabin) ?? Number.MAX_SAFE_INTEGER
  const recommended = getRecommendedFlight(flights)

  return [...flights].sort((first, second) => {
    switch (order) {
      case 'price':
        return price(first) - price(second) || departureTime(first) - departureTime(second)
      case 'duration':
        return first.totalDurationMinutes - second.totalDurationMinutes || price(first) - price(second)
      case 'departure':
        return departureTime(first) - departureTime(second)
      case 'recommended':
        return (
          Number(second === recommended) - Number(first === recommended) ||
          departureTime(first) - departureTime(second)
        )
    }
  })
}
