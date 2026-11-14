import { isRecord } from '#/shared/api/guards'
import { isUuid } from '#/shared/lib/identifiers'
import { HttpError } from './errors.server'

const MAX_BODY_BYTES = 16384
const MAX_PASSENGERS = 8
const FLIGHT_ID = /^[1-9]\d*$/
const DATE_KEY = /^\d{4}-\d{2}-\d{2}$/

/** Reads a JSON object body, bounded by encoded byte length rather than characters. */
export async function readJsonBody(
  request: Request,
): Promise<Record<string, unknown>> {
  if (!request.headers.get('content-type')?.startsWith('application/json')) {
    throw new HttpError(
      415,
      'json_required',
      'Send an application/json request.',
    )
  }

  const text = await request.text()

  if (new TextEncoder().encode(text).byteLength > MAX_BODY_BYTES) {
    throw new HttpError(413, 'request_too_large', 'The request is too large.')
  }

  let body: unknown

  try {
    body = JSON.parse(text)
  } catch {
    throw new HttpError(
      400,
      'invalid_json',
      'The request contains invalid JSON.',
    )
  }

  if (!isRecord(body)) {
    throw new HttpError(400, 'invalid_request', 'A JSON object is required.')
  }

  return body
}

/** Allows one flight and one fare per seated passenger, in USD. */
export function validateOfferRequest(body: Record<string, unknown>) {
  const passengerCount = body.passengerCount

  if (
    body.currency !== 'USD' ||
    typeof passengerCount !== 'number' ||
    !Number.isInteger(passengerCount) ||
    passengerCount < 1 ||
    passengerCount > MAX_PASSENGERS ||
    !Array.isArray(body.selections) ||
    body.selections.length !== passengerCount
  ) {
    throw new HttpError(
      400,
      'unsupported_journey',
      'Select one flight and one fare for each of up to eight seated travellers.',
    )
  }

  const flightIds = new Set<string>()
  const indexes = new Set<number>()

  for (const selection of body.selections) {
    if (
      !isRecord(selection) ||
      typeof selection.flightId !== 'string' ||
      !FLIGHT_ID.test(selection.flightId) ||
      !isUuid(selection.productId) ||
      typeof selection.passengerIndex !== 'number' ||
      !Number.isInteger(selection.passengerIndex) ||
      selection.passengerIndex < 0 ||
      selection.passengerIndex >= passengerCount
    ) {
      throw new HttpError(
        400,
        'invalid_selection',
        'Select a valid flight and fare for each traveller.',
      )
    }

    flightIds.add(selection.flightId)
    indexes.add(selection.passengerIndex)
  }

  if (flightIds.size !== 1 || indexes.size !== passengerCount) {
    throw new HttpError(
      400,
      'unsupported_journey',
      'Only one-way, single-flight bookings are supported.',
    )
  }
}

function isCalendarDate(value: unknown): value is string {
  return (
    typeof value === 'string' &&
    DATE_KEY.test(value) &&
    Number.isFinite(Date.parse(value)) &&
    new Date(value).toISOString().slice(0, 10) === value
  )
}

/** Requires a valid offer and one to eight passengers aged two or older (UTC). */
export function validateReservationRequest(
  body: Record<string, unknown>,
  now = new Date(),
) {
  if (
    !isUuid(body.offerId) ||
    !Array.isArray(body.passengers) ||
    body.passengers.length < 1 ||
    body.passengers.length > MAX_PASSENGERS
  ) {
    throw new HttpError(
      400,
      'invalid_passengers',
      'A valid offer and one to eight passengers are required.',
    )
  }

  const cutoff = new Date(
    Date.UTC(now.getUTCFullYear() - 2, now.getUTCMonth(), now.getUTCDate()),
  )

  for (const passenger of body.passengers) {
    if (!isRecord(passenger) || !isCalendarDate(passenger.dateOfBirth)) {
      throw new HttpError(
        400,
        'invalid_birth_date',
        'A valid date of birth is required.',
      )
    }

    if (new Date(passenger.dateOfBirth) > cutoff) {
      throw new HttpError(
        400,
        'infants_deferred',
        'Bookings for children under two are not available yet.',
      )
    }
  }
}
