export interface FlightResponse {
  id: number
  flightNumber: string
  departureTime: string
  arrivalTime: string
  departureAirport: string
  arrivalAirport: string
}

export interface BaggageSnapshot {
  includedPieces: number
  maxWeightPerPieceKg: number
  maxExtraPieces: number
  extraPiecePrice: number
}

export interface FlightFareResponse {
  flightId: string
  productId: string
  productCode: string
  productVersion: number
  cabinId: string
  cabinClass: string
  brand: string
  amount: number
  currency: string
  baggage: BaggageSnapshot
}

export interface FareSnapshot extends FlightFareResponse {
  passengerIndex: number
}

export interface OfferResponse {
  id: string
  expiresAt: string
  currency: string
  totalAmount: number
  fares: FareSnapshot[]
}

export interface ReservationResponse {
  id: number
  code: string
  status: string
  reservationDate: string
  currency: string
  totalAmount: number
  fares: FareSnapshot[]
  workflowId: string
  workflowStatus: string
  failureCode: string | null
  contactName: string
  contactEmail: string
  passengers: Array<{ id: number; firstName: string; lastName: string }>
}

export interface PassengerRequest {
  firstName: string
  lastName: string
  dateOfBirth: string
}

export interface ReservationRequest {
  offerId: string
  contactName: string
  contactEmail: string
  passengers: PassengerRequest[]
}

export function webReservationRequestId(offerId: string) {
  return `web-${offerId.toLowerCase()}`
}

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

export function isFlight(value: unknown): value is FlightResponse {
  return (
    isRecord(value) &&
    Number.isInteger(value.id) &&
    typeof value.id === 'number' &&
    value.id > 0 &&
    [
      'flightNumber',
      'departureTime',
      'arrivalTime',
      'departureAirport',
      'arrivalAirport',
    ].every((key) => typeof value[key] === 'string') &&
    Number.isFinite(Date.parse(String(value.departureTime))) &&
    Number.isFinite(Date.parse(String(value.arrivalTime)))
  )
}

export function isFare(value: unknown): value is FlightFareResponse {
  return (
    isRecord(value) &&
    [
      'flightId',
      'productId',
      'productCode',
      'cabinId',
      'cabinClass',
      'brand',
      'currency',
    ].every((key) => typeof value[key] === 'string') &&
    Number.isInteger(value.productVersion) &&
    typeof value.amount === 'number' &&
    Number.isFinite(value.amount) &&
    value.amount >= 0 &&
    isBaggage(value.baggage)
  )
}

function isBaggage(value: unknown): value is BaggageSnapshot {
  return (
    isRecord(value) &&
    [
      'includedPieces',
      'maxWeightPerPieceKg',
      'maxExtraPieces',
      'extraPiecePrice',
    ].every(
      (key) =>
        typeof value[key] === 'number' &&
        Number.isFinite(value[key]) &&
        value[key] >= 0,
    )
  )
}

function isFareSnapshot(value: unknown): value is FareSnapshot {
  return (
    isFare(value) &&
    isRecord(value) &&
    typeof value.passengerIndex === 'number' &&
    Number.isInteger(value.passengerIndex) &&
    value.passengerIndex >= 0 &&
    value.passengerIndex < 8
  )
}

function hasFareSnapshots(value: Record<string, unknown>) {
  const fares = value.fares
  if (
    typeof value.currency !== 'string' ||
    !/^[A-Z]{3}$/.test(value.currency) ||
    typeof value.totalAmount !== 'number' ||
    !Number.isFinite(value.totalAmount) ||
    value.totalAmount < 0 ||
    !Array.isArray(fares) ||
    fares.length < 1 ||
    fares.length > 8 ||
    !fares.every(isFareSnapshot)
  ) {
    return false
  }
  const indexes = fares
    .map((fare) => fare.passengerIndex)
    .sort((left, right) => left - right)
  return (
    indexes.every((index, position) => index === position) &&
    new Set(fares.map((fare) => fare.flightId)).size === 1 &&
    fares.every((fare) => fare.currency === value.currency)
  )
}

export function isOffer(value: unknown): value is OfferResponse {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.expiresAt === 'string' &&
    Number.isFinite(Date.parse(value.expiresAt)) &&
    hasFareSnapshots(value)
  )
}

export function isReservation(value: unknown): value is ReservationResponse {
  return (
    isRecord(value) &&
    typeof value.id === 'number' &&
    Number.isInteger(value.id) &&
    [
      'code',
      'status',
      'reservationDate',
      'workflowId',
      'workflowStatus',
      'contactName',
      'contactEmail',
    ].every((key) => typeof value[key] === 'string') &&
    (value.failureCode === null || typeof value.failureCode === 'string') &&
    hasFareSnapshots(value) &&
    Array.isArray(value.passengers) &&
    Array.isArray(value.fares) &&
    value.passengers.length === value.fares.length &&
    value.passengers.every(
      (passenger: unknown) =>
        isRecord(passenger) &&
        typeof passenger.id === 'number' &&
        typeof passenger.firstName === 'string' &&
        typeof passenger.lastName === 'string',
    )
  )
}
