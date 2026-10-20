import { isDateKey } from '#/shared/lib/formatting'

export const CABIN_OPTIONS = [
  'Economy',
  'Premium Economy',
  'Business Class',
  'First Class',
] as const

export type CabinPreference = (typeof CABIN_OPTIONS)[number]
export type TripType = 'roundTrip' | 'oneWay'

export const MAX_SEATED_TRAVELLERS = 8

export interface TravellerCounts {
  adult: number
  child: number
  infant: number
}

export const DEFAULT_TRAVELLERS: TravellerCounts = {
  adult: 1,
  child: 0,
  infant: 0,
}
export const DEFAULT_CABIN: CabinPreference = 'Economy'

/** Flight search criteria carried in the URL between the search and results pages. */
export interface FlightSearchParams {
  from?: string
  to?: string
  departure?: string
  return?: string
  tripType?: TripType
  adults?: number
  children?: number
  infants?: number
  cabin?: CabinPreference
}

const AIRPORT_CODE = /^[A-Z]{3}$/

function readAirportCode(value: unknown): string | undefined {
  return typeof value === 'string' && AIRPORT_CODE.test(value)
    ? value
    : undefined
}

function readCount(value: unknown, minimum: number): number | undefined {
  return typeof value === 'number' &&
    Number.isInteger(value) &&
    value >= minimum &&
    value <= MAX_SEATED_TRAVELLERS
    ? value
    : undefined
}

function isCabinPreference(value: unknown): value is CabinPreference {
  return CABIN_OPTIONS.some((cabin) => cabin === value)
}

export function isValidTravellerCounts({
  adult,
  child,
  infant,
}: TravellerCounts): boolean {
  return (
    adult >= 1 &&
    child >= 0 &&
    infant >= 0 &&
    adult + child <= MAX_SEATED_TRAVELLERS &&
    infant <= adult
  )
}

export function parseFlightSearchParams(
  search: Record<string, unknown>,
): FlightSearchParams {
  const tripType: TripType | undefined =
    search.tripType === 'oneWay' || search.tripType === 'roundTrip'
      ? search.tripType
      : undefined
  const departure = isDateKey(search.departure) ? search.departure : undefined
  const returnDate =
    tripType !== 'oneWay' &&
    isDateKey(search.return) &&
    (!departure || search.return >= departure)
      ? search.return
      : undefined
  const travellers = {
    adult: readCount(search.adults, 1),
    child: readCount(search.children, 0),
    infant: readCount(search.infants, 0),
  }
  const hasValidTravellers = isValidTravellerCounts({
    adult: travellers.adult ?? DEFAULT_TRAVELLERS.adult,
    child: travellers.child ?? DEFAULT_TRAVELLERS.child,
    infant: travellers.infant ?? DEFAULT_TRAVELLERS.infant,
  })

  return {
    from: readAirportCode(search.from),
    to: readAirportCode(search.to),
    departure,
    return: returnDate,
    tripType,
    adults: hasValidTravellers ? travellers.adult : undefined,
    children: hasValidTravellers ? travellers.child : undefined,
    infants: hasValidTravellers ? travellers.infant : undefined,
    cabin: isCabinPreference(search.cabin) ? search.cabin : undefined,
  }
}

export function resolveTravellers(params: FlightSearchParams): TravellerCounts {
  return {
    adult: params.adults ?? DEFAULT_TRAVELLERS.adult,
    child: params.children ?? DEFAULT_TRAVELLERS.child,
    infant: params.infants ?? DEFAULT_TRAVELLERS.infant,
  }
}

export function resolveTripType(params: FlightSearchParams): TripType {
  return params.tripType ?? 'roundTrip'
}

function pluralize(count: number, singular: string, plural: string): string {
  return `${count} ${count === 1 ? singular : plural}`
}

export function formatTravellers({
  adult,
  child,
  infant,
}: TravellerCounts): string {
  return [
    pluralize(adult, 'adult', 'adults'),
    child > 0 ? pluralize(child, 'child', 'children') : null,
    infant > 0 ? pluralize(infant, 'infant', 'infants') : null,
  ]
    .filter(Boolean)
    .join(', ')
}

export function seatedTravellerCount({
  adult,
  child,
}: TravellerCounts): number {
  return adult + child
}

export interface SeatedPassenger {
  id: string
  label: string
}

/** Seated travellers in booking order. Infants travel on a lap and have no fare of their own. */
export function getSeatedPassengers({
  adult,
  child,
}: TravellerCounts): SeatedPassenger[] {
  const adults = Array.from({ length: adult }, (_, index) => ({
    id: `adult-${index + 1}`,
    label: `Adult ${index + 1}`,
  }))
  const children = Array.from({ length: child }, (_, index) => ({
    id: `child-${index + 1}`,
    label: `Child ${index + 1}`,
  }))

  return [...adults, ...children]
}
