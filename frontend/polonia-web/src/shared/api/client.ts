import type { ReservationRequest } from './contracts'
import { isFare, isFlight, isOffer, isReservation } from './guards'
import { isNotFound, request } from './http'
import { webReservationRequestId } from '#/shared/lib/identifiers'

export const FLIGHT_SEARCH_PAGE_SIZE = 20

export interface FlightSearchCriteria {
  from: string
  to: string
  departure: string
  page: number
}

export async function searchFlights(
  criteria: FlightSearchCriteria,
  signal?: AbortSignal,
) {
  const query = new URLSearchParams({
    DepartureAirport: criteria.from,
    ArrivalAirport: criteria.to,
    DepartureDate: criteria.departure,
    Page: String(criteria.page),
    PageSize: String(FLIGHT_SEARCH_PAGE_SIZE),
  })
  const data = await request(`flights?${query}`, signal)

  if (!Array.isArray(data) || !data.every(isFlight)) {
    throw new Error('Invalid flight search response.')
  }

  return data
}

export async function getFlight(id: string, signal?: AbortSignal) {
  const data = await request(`flights/${encodeURIComponent(id)}`, signal)

  if (!isFlight(data)) {
    throw new Error('Invalid flight response.')
  }

  return data
}

export async function getFares(flightIds: number[], signal?: AbortSignal) {
  const query = new URLSearchParams({ Currency: 'USD' })
  flightIds.forEach((id) => query.append('FlightIds', String(id)))
  const data = await request(`fares?${query}`, signal)

  if (!Array.isArray(data) || !data.every(isFare)) {
    throw new Error('Invalid fare response.')
  }

  return data
}

/** Starts (or reuses) the guest checkout session, then creates a server-priced offer. */
export async function createOffer(
  flightId: number,
  productIds: string[],
  signal?: AbortSignal,
) {
  await request('checkout', signal, {})
  const data = await request('offers', signal, {
    currency: 'USD',
    passengerCount: productIds.length,
    selections: productIds.map((productId, passengerIndex) => ({
      flightId: String(flightId),
      productId,
      passengerIndex,
    })),
  })

  if (!isOffer(data)) {
    throw new Error('Invalid offer response.')
  }

  return data
}

export async function getOffer(id: string, signal?: AbortSignal) {
  const data = await request(`offers/${encodeURIComponent(id)}`, signal)

  if (!isOffer(data)) {
    throw new Error('Invalid offer response.')
  }

  return data
}

export async function getReservation(id: number, signal?: AbortSignal) {
  const data = await request(`reservations/${id}`, signal)

  if (!isReservation(data)) {
    throw new Error('Invalid reservation response.')
  }

  return data
}

/**
 * Finds a reservation already committed for an offer. Returns `null` only for an
 * actual not-found response; every other failure is rethrown.
 */
export async function recoverReservation(
  offerId: string,
  signal?: AbortSignal,
) {
  const requestId = encodeURIComponent(webReservationRequestId(offerId))

  try {
    const data = await request(`reservations/by-request/${requestId}`, signal)

    if (!isReservation(data)) {
      throw new Error('Invalid reservation response.')
    }

    return data
  } catch (error) {
    if (isNotFound(error)) {
      return null
    }

    throw error
  }
}

export async function createReservation(body: ReservationRequest) {
  const data = await request('reservations', undefined, body)

  if (!isReservation(data)) {
    throw new Error('Invalid reservation response.')
  }

  return data
}
