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