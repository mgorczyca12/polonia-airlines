import airportData from '#/data/airports.json'
import type { Airport, AirportSelectOption } from './airport.types'

const airports = airportData as Airport[]

export function getAirportSelectOptions(): AirportSelectOption[] {
  return airports.map((airport) => ({
    value: airport.Code,
    label: `${airport.City} ${airport.Airport} (${airport.Code})`,
  }))
}

export function getAirportByCode(airportCode: string): Airport | undefined {
  return airports.find((airport) => airport.Code === airportCode)
}

export function getCityName(airportCode: string): string {
  return getAirportByCode(airportCode)?.City ?? airportCode
}

/** City and code, e.g. "Chicago (ORD)", falling back to the code alone. */
export function airportLabel(airportCode: string): string {
  const airport = getAirportByCode(airportCode)
  return airport ? `${airport.City} (${airportCode})` : airportCode
}
