import { isRecord } from '#/shared/api/guards'
import { webReservationRequestId } from '#/shared/lib/identifiers'
import type { GuestSession } from './session.server'
import { requireGuestToken } from './session.server'
import { callUpstream } from './upstream.server'
import type { UpstreamResult } from './upstream.server'
import {
  readJsonBody,
  validateOfferRequest,
  validateReservationRequest,
} from './validation.server'

const GUEST_TOKEN = /^[0-9a-f]{64}$/i

export interface RouteContext {
  request: Request
  url: URL
  /** Path after `/api/web/`, without the query string. */
  path: string
  session?: GuestSession
}

interface WebApiRoute {
  method: 'GET' | 'POST'
  pattern: RegExp
  /** Guest routes load the encrypted session cookie; public routes never do. */
  access: 'public' | 'guest'
  handle: (context: RouteContext) => Promise<UpstreamResult>
}

function guestSession(context: RouteContext): GuestSession {
  if (!context.session) {
    throw new Error('Guest route resolved without a session.')
  }

  return context.session
}

async function startCheckout(context: RouteContext): Promise<UpstreamResult> {
  const session = guestSession(context)

  if (!session.guestToken) {
    const result = await callUpstream({
      service: 'booking',
      path: '/api/booking/checkouts',
      request: context.request,
    })

    if (
      result.status !== 200 ||
      !isRecord(result.data) ||
      typeof result.data.guestToken !== 'string' ||
      !GUEST_TOKEN.test(result.data.guestToken)
    ) {
      throw new Error('Invalid checkout response.')
    }

    session.guestToken = result.data.guestToken
    await session.save()
  }

  return { status: 200, data: { ready: true } }
}

async function createOffer(context: RouteContext): Promise<UpstreamResult> {
  const guestToken = requireGuestToken(guestSession(context))
  const body = await readJsonBody(context.request)
  validateOfferRequest(body)

  return callUpstream({
    service: 'booking',
    path: '/api/booking/offers',
    request: context.request,
    guestToken,
    body,
  })
}

async function createReservation(
  context: RouteContext,
): Promise<UpstreamResult> {
  const guestToken = requireGuestToken(guestSession(context))
  const body = await readJsonBody(context.request)
  validateReservationRequest(body)

  return callUpstream({
    service: 'booking',
    path: '/api/booking/reservations',
    request: context.request,
    guestToken,
    // The submission identity is derived from the offer, never caller-supplied.
    body: { ...body, requestId: webReservationRequestId(String(body.offerId)) },
  })
}

function readGuestResource(context: RouteContext): Promise<UpstreamResult> {
  return callUpstream({
    service: 'booking',
    path: `/api/booking/${context.path}`,
    request: context.request,
    guestToken: requireGuestToken(guestSession(context)),
  })
}

/**
 * Explicit allowlist of browser-callable operations. Anything not listed,
 * including administrative backend routes, is a 404.
 */
const ROUTES: WebApiRoute[] = [
  {
    method: 'GET',
    pattern: /^flights$/,
    access: 'public',
    handle: ({ request, url }) =>
      callUpstream({
        service: 'flight',
        path: `/api/Flight/flights${url.search}`,
        request,
      }),
  },
  {
    method: 'GET',
    pattern: /^flights\/[1-9]\d*$/,
    access: 'public',
    handle: ({ request, url, path }) =>
      callUpstream({
        service: 'flight',
        path: `/api/Flight/flight/${path.split('/')[1]}${url.search}`,
        request,
      }),
  },
  {
    method: 'GET',
    pattern: /^fares$/,
    access: 'public',
    handle: ({ request, url }) =>
      callUpstream({
        service: 'booking',
        path: `/api/booking/fares${url.search}`,
        request,
      }),
  },
  { method: 'POST', pattern: /^checkout$/, access: 'guest', handle: startCheckout },
  { method: 'POST', pattern: /^offers$/, access: 'guest', handle: createOffer },
  {
    method: 'POST',
    pattern: /^reservations$/,
    access: 'guest',
    handle: createReservation,
  },
  {
    method: 'GET',
    pattern: /^offers\/[0-9a-f-]{36}$/i,
    access: 'guest',
    handle: readGuestResource,
  },
  {
    method: 'GET',
    pattern: /^reservations\/[1-9]\d*$/,
    access: 'guest',
    handle: readGuestResource,
  },
  {
    method: 'GET',
    pattern: /^reservations\/by-request\/web-[0-9a-f-]{36}$/i,
    access: 'guest',
    handle: readGuestResource,
  },
]

export function resolveRoute(
  method: string,
  path: string,
): WebApiRoute | undefined {
  return ROUTES.find(
    (route) => route.method === method && route.pattern.test(path),
  )
}
