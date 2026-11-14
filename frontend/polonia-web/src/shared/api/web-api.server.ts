import { createServerOnlyFn } from '@tanstack/react-start'
import { getIronSession, webCookies } from 'iron-session'
import { isRecord, webReservationRequestId } from './contracts'

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

class HttpError extends Error {
  constructor(
    public status: number,
    public code: string,
    message: string,
  ) {
    super(message)
  }
}

function configuration() {
  const password = process.env.WEB_SESSION_PASSWORD
  const origin = process.env.WEB_ORIGIN
  if (!password || password.length < 32 || !origin) {
    throw new Error(
      'WEB_SESSION_PASSWORD (32+ characters) and WEB_ORIGIN are required.',
    )
  }
  const url = new URL(origin)
  if (url.origin !== origin || !['http:', 'https:'].includes(url.protocol)) {
    throw new Error('WEB_ORIGIN must be an HTTP(S) origin without a path.')
  }
  if (
    url.protocol === 'http:' &&
    !['localhost', '127.0.0.1'].includes(url.hostname)
  ) {
    throw new Error('Non-local web origins must use HTTPS.')
  }
  return { password, origin, secure: url.protocol === 'https:' }
}

async function jsonBody(request: Request) {
  if (!request.headers.get('content-type')?.startsWith('application/json')) {
    throw new HttpError(
      415,
      'json_required',
      'Send an application/json request.',
    )
  }
  const text = await request.text()
  if (new TextEncoder().encode(text).byteLength > 16384)
    throw new HttpError(413, 'request_too_large', 'The request is too large.')
  try {
    const body: unknown = JSON.parse(text)
    if (!isRecord(body))
      throw new HttpError(400, 'invalid_request', 'A JSON object is required.')
    return body
  } catch (error) {
    if (error instanceof SyntaxError) {
      throw new HttpError(
        400,
        'invalid_json',
        'The request contains invalid JSON.',
      )
    }
    throw error
  }
}

function validateOffer(body: Record<string, unknown>) {
  if (
    body.currency !== 'USD' ||
    typeof body.passengerCount !== 'number' ||
    !Number.isInteger(body.passengerCount) ||
    body.passengerCount < 1 ||
    body.passengerCount > 8 ||
    !Array.isArray(body.selections) ||
    body.selections.length !== body.passengerCount
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
      !/^[1-9]\d*$/.test(selection.flightId) ||
      typeof selection.productId !== 'string' ||
      !UUID.test(selection.productId) ||
      typeof selection.passengerIndex !== 'number' ||
      !Number.isInteger(selection.passengerIndex) ||
      selection.passengerIndex < 0 ||
      selection.passengerIndex >= body.passengerCount
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
  if (flightIds.size !== 1 || indexes.size !== body.passengerCount) {
    throw new HttpError(
      400,
      'unsupported_journey',
      'Only one-way, single-flight bookings are supported.',
    )
  }
}

function validatePassengers(body: Record<string, unknown>) {
  if (
    typeof body.offerId !== 'string' ||
    !UUID.test(body.offerId) ||
    !Array.isArray(body.passengers) ||
    body.passengers.length < 1 ||
    body.passengers.length > 8
  ) {
    throw new HttpError(
      400,
      'invalid_passengers',
      'A valid offer and one to eight passengers are required.',
    )
  }
  const now = new Date()
  const cutoff = new Date(
    Date.UTC(now.getUTCFullYear() - 2, now.getUTCMonth(), now.getUTCDate()),
  )
  for (const passenger of body.passengers) {
    if (
      !isRecord(passenger) ||
      typeof passenger.dateOfBirth !== 'string' ||
      !/^\d{4}-\d{2}-\d{2}$/.test(passenger.dateOfBirth) ||
      !Number.isFinite(Date.parse(passenger.dateOfBirth)) ||
      new Date(passenger.dateOfBirth).toISOString().slice(0, 10) !==
        passenger.dateOfBirth
    ) {
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

async function upstream(
  service: 'flight' | 'booking',
  path: string,
  request: Request,
  guestToken?: string,
  body?: unknown,
) {
  const base =
    service === 'flight'
      ? process.env.FLIGHT_API_URL
      : process.env.BOOKING_API_URL
  if (!base) throw new Error(`${service.toUpperCase()}_API_URL is required.`)
  const target = new URL(path, base)
  if (
    !['http:', 'https:'].includes(target.protocol) ||
    target.username ||
    target.password
  ) {
    throw new Error(
      'Backend API URLs must use HTTP(S), without embedded credentials.',
    )
  }
  let response: Response
  try {
    response = await fetch(target, {
      method: request.method,
      headers: {
        Accept: 'application/json',
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(guestToken ? { 'X-Guest-Token': guestToken } : {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      redirect: 'error',
      cache: 'no-store',
      signal: AbortSignal.any([request.signal, AbortSignal.timeout(15000)]),
    })
  } catch (error) {
    if (request.signal.aborted) throw error
    if (
      !(error instanceof TypeError) &&
      !(error instanceof DOMException && error.name === 'TimeoutError')
    )
      throw error
    console.error('Backend request failed', {
      service,
      error,
    })
    throw new HttpError(
      502,
      'backend_unavailable',
      'The booking system is unavailable. Submission may have succeeded; retry status before submitting again.',
    )
  }
  if (response.status >= 500) {
    console.error('Backend returned a server error', {
      service,
      status: response.status,
    })
    throw new HttpError(
      502,
      'backend_unavailable',
      'The booking system is unavailable. Retry status before submitting again.',
    )
  }
  const data: unknown = await response.json()
  return { status: response.status, data }
}

export const handleWebApi = createServerOnlyFn(
  async (request: Request): Promise<Response> => {
    const headers = new Headers({
      'Cache-Control': 'no-store',
      'Content-Type': 'application/json',
    })
    try {
      const config = configuration()
      if (
        request.method === 'POST' &&
        request.headers.get('origin') !== config.origin
      ) {
        throw new HttpError(
          403,
          'invalid_origin',
          'The request origin is not allowed.',
        )
      }
      const url = new URL(request.url)
      const path = url.pathname.slice('/api/web/'.length)
      let result: { status: number; data: unknown }
      if (
        request.method === 'GET' &&
        (path === 'flights' || /^flights\/[1-9]\d*$/.test(path))
      ) {
        const target =
          path === 'flights'
            ? '/api/Flight/flights'
            : `/api/Flight/flight/${path.split('/')[1]}`
        result = await upstream('flight', `${target}${url.search}`, request)
      } else if (request.method === 'GET' && path === 'fares') {
        result = await upstream(
          'booking',
          `/api/booking/fares${url.search}`,
          request,
        )
      } else {
        const session = await getIronSession<{ guestToken: string }>(
          webCookies(request, headers),
          {
            cookieName: 'polonia-guest',
            password: config.password,
            ttl: 86400,
            cookieOptions: {
              httpOnly: true,
              secure: config.secure,
              sameSite: 'strict',
              path: '/',
            },
            onUnsealError: (reason) =>
              console.warn('Guest session cookie rejected', { reason }),
          },
        )
        if (request.method === 'POST' && path === 'checkout') {
          if (!session.guestToken) {
            result = await upstream(
              'booking',
              '/api/booking/checkouts',
              request,
            )
            if (
              result.status !== 200 ||
              !isRecord(result.data) ||
              typeof result.data.guestToken !== 'string' ||
              !/^[0-9a-f]{64}$/i.test(result.data.guestToken)
            ) {
              throw new Error('Invalid checkout response.')
            }
            session.guestToken = result.data.guestToken
            await session.save()
          }
          result = { status: 200, data: { ready: true } }
        } else {
          if (!session.guestToken)
            throw new HttpError(
              401,
              'guest_session_expired',
              'Your guest session has expired. Start a new search.',
            )
          if (request.method === 'POST' && path === 'offers') {
            const body = await jsonBody(request)
            validateOffer(body)
            result = await upstream(
              'booking',
              '/api/booking/offers',
              request,
              session.guestToken,
              body,
            )
          } else if (request.method === 'POST' && path === 'reservations') {
            const body = await jsonBody(request)
            validatePassengers(body)
            result = await upstream(
              'booking',
              '/api/booking/reservations',
              request,
              session.guestToken,
              {
                ...body,
                requestId: webReservationRequestId(String(body.offerId)),
              },
            )
          } else if (
            request.method === 'GET' &&
            (/^offers\/[0-9a-f-]{36}$/i.test(path) ||
              /^reservations\/[1-9]\d*$/.test(path) ||
              /^reservations\/by-request\/web-[0-9a-f-]{36}$/i.test(path))
          ) {
            result = await upstream(
              'booking',
              `/api/booking/${path}`,
              request,
              session.guestToken,
            )
          } else {
            throw new HttpError(
              404,
              'route_not_found',
              'This API route is not available.',
            )
          }
        }
      }
      if (isRecord(result.data) && 'guestAccessToken' in result.data) {
        const { guestAccessToken: _secret, ...publicResponse } = result.data
        result.data = publicResponse
      }
      return new Response(JSON.stringify(result.data), {
        status: result.status,
        headers,
      })
    } catch (error) {
      if (request.signal.aborted) throw error
      if (error instanceof HttpError) {
        return new Response(
          JSON.stringify({
            status: error.status,
            code: error.code,
            detail: error.message,
          }),
          {
            status: error.status,
            headers,
          },
        )
      }
      const traceId = crypto.randomUUID()
      console.error('Web API failed', { traceId, error })
      return new Response(
        JSON.stringify({
          status: 500,
          code: 'web_api_error',
          detail: `An unexpected error occurred. Reference: ${traceId}`,
        }),
        { status: 500, headers },
      )
    }
  },
)
