import { HttpError } from './errors.server'

const UPSTREAM_TIMEOUT_MS = 15000

export type BackendService = 'flight' | 'booking'

export interface UpstreamResult {
  status: number
  data: unknown
}

interface UpstreamRequest {
  service: BackendService
  path: string
  request: Request
  guestToken?: string
  body?: unknown
}

function serviceUrl(service: BackendService, path: string): URL {
  const base =
    service === 'flight'
      ? process.env.FLIGHT_API_URL
      : process.env.BOOKING_API_URL

  if (!base) {
    throw new Error(`${service.toUpperCase()}_API_URL is required.`)
  }

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

  return target
}

function isNetworkFailure(error: unknown): boolean {
  return (
    error instanceof TypeError ||
    (error instanceof DOMException && error.name === 'TimeoutError')
  )
}

/**
 * Calls a backend service with the browser's method and cancellation. Backend
 * outages and 5xx responses become safe 502 errors; other statuses pass through.
 */
export async function callUpstream({
  service,
  path,
  request,
  guestToken,
  body,
}: UpstreamRequest): Promise<UpstreamResult> {
  const target = serviceUrl(service, path)
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
      signal: AbortSignal.any([
        request.signal,
        AbortSignal.timeout(UPSTREAM_TIMEOUT_MS),
      ]),
    })
  } catch (error) {
    if (request.signal.aborted || !isNetworkFailure(error)) {
      throw error
    }

    console.error('Backend request failed', { service, error })
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
