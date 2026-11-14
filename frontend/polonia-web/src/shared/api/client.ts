import {
  isFare,
  isFlight,
  isOffer,
  isRecord,
  isReservation,
  webReservationRequestId,
} from './contracts'
import type { ReservationRequest } from './contracts'

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    message: string,
    public readonly errors: Record<string, string[]> = {},
  ) {
    super(message)
  }
}

async function request(
  path: string,
  signal?: AbortSignal,
  body?: unknown,
): Promise<unknown> {
  const response = await fetch(`/api/web/${path}`, {
    method: body === undefined ? 'GET' : 'POST',
    credentials: 'same-origin',
    cache: 'no-store',
    signal,
    headers:
      body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  const data: unknown = await response.json()
  if (!response.ok) {
    const errors: Record<string, string[]> = {}
    if (isRecord(data) && isRecord(data.errors)) {
      for (const [key, messages] of Object.entries(data.errors)) {
        if (
          Array.isArray(messages) &&
          messages.every((message) => typeof message === 'string')
        ) {
          errors[key] = messages
        }
      }
    }
    throw new ApiError(
      response.status,
      isRecord(data) && typeof data.code === 'string'
        ? data.code
        : 'request_failed',
      isRecord(data) && typeof data.detail === 'string'
        ? data.detail
        : 'The request failed. Please try again.',
      errors,
    )
  }
  return data
}

export async function searchFlights(
  params: URLSearchParams,
  signal: AbortSignal,
) {
  const data = await request(`flights?${params}`, signal)
  if (!Array.isArray(data) || !data.every(isFlight))
    throw new Error('Invalid flight search response.')
  return data
}

export async function getFlight(id: string, signal: AbortSignal) {
  const data = await request(`flights/${encodeURIComponent(id)}`, signal)
  if (!isFlight(data)) throw new Error('Invalid flight response.')
  return data
}

export async function getFares(ids: number[], signal: AbortSignal) {
  const query = new URLSearchParams({ Currency: 'USD' })
  ids.forEach((id) => query.append('FlightIds', String(id)))
  const data = await request(`fares?${query}`, signal)
  if (!Array.isArray(data) || !data.every(isFare))
    throw new Error('Invalid fare response.')
  return data
}

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
  if (!isOffer(data)) throw new Error('Invalid offer response.')
  return data
}

export async function getOffer(id: string, signal: AbortSignal) {
  const data = await request(`offers/${encodeURIComponent(id)}`, signal)
  if (!isOffer(data)) throw new Error('Invalid offer response.')
  return data
}

export async function getReservation(id: number, signal: AbortSignal) {
  const data = await request(`reservations/${id}`, signal)
  if (!isReservation(data)) throw new Error('Invalid reservation response.')
  return data
}

export async function recoverReservation(offerId: string, signal: AbortSignal) {
  try {
    const data = await request(
      `reservations/by-request/${encodeURIComponent(webReservationRequestId(offerId))}`,
      signal,
    )
    if (!isReservation(data)) throw new Error('Invalid reservation response.')
    return data
  } catch (error) {
    if (
      error instanceof ApiError &&
      error.status === 404 &&
      error.code === 'not_found'
    )
      return null
    throw error
  }
}

export async function createReservation(body: ReservationRequest) {
  const data = await request('reservations', undefined, body)
  if (!isReservation(data)) throw new Error('Invalid reservation response.')
  return data
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const fields = Object.values(error.errors).flat().join(' ')
    return fields ? `${error.message} ${fields}` : error.message
  }
  return error instanceof Error
    ? error.message
    : 'An unexpected error occurred.'
}
