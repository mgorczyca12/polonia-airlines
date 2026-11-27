import {
  BrokenCircuitError,
  CircuitState,
  ConsecutiveBreaker,
  ExponentialBackoff,
  circuitBreaker,
  handleType,
  retry,
} from 'cockatiel'
import { HttpError } from './errors.server'

const UPSTREAM_TIMEOUT_MS = 15000

export type BackendService = 'flight' | 'booking'

class UpstreamFailure extends Error {
  constructor(public readonly status?: number) {
    super('Backend request failed')
  }
}

const transientFailure = handleType(UpstreamFailure)
const readRetry = retry(transientFailure, {
  maxAttempts: 2,
  backoff: new ExponentialBackoff({ initialDelay: 100, maxDelay: 1000 }),
})
const breakers = {
  flight: circuitBreaker(transientFailure, {
    halfOpenAfter: 30_000,
    breaker: new ConsecutiveBreaker(5),
  }),
  booking: circuitBreaker(transientFailure, {
    halfOpenAfter: 30_000,
    breaker: new ConsecutiveBreaker(5),
  }),
}

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

function executeWithCancellation<T>(
  signal: AbortSignal,
  operation: () => Promise<T>,
): Promise<T> {
  signal.throwIfAborted()

  return new Promise((resolve, reject) => {
    const abort = () => reject(signal.reason)
    signal.addEventListener('abort', abort, { once: true })
    operation()
      .then(resolve, reject)
      .finally(() => {
        signal.removeEventListener('abort', abort)
      })
  })
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
  const signal = AbortSignal.any([
    request.signal,
    AbortSignal.timeout(UPSTREAM_TIMEOUT_MS),
  ])
  const send = async (): Promise<Response> => {
    signal.throwIfAborted()

    return breakers[service].execute(async () => {
      let response: Response
      try {
        response = await fetch(target, {
          method: request.method,
          headers: {
            Accept: 'application/json',
            ...(body === undefined
              ? {}
              : { 'Content-Type': 'application/json' }),
            ...(guestToken ? { 'X-Guest-Token': guestToken } : {}),
          },
          body: body === undefined ? undefined : JSON.stringify(body),
          redirect: 'error',
          cache: 'no-store',
          signal,
        })
      } catch (error) {
        if (request.signal.aborted) {
          // A cancelled recovery probe must not close the circuit as a success.
          if (breakers[service].state === CircuitState.HalfOpen) {
            throw new UpstreamFailure()
          }
          throw error
        }
        if (error instanceof TypeError || signal.aborted) {
          throw new UpstreamFailure()
        }
        throw error
      }

      if (response.status >= 500) {
        await response.body?.cancel()
        throw new UpstreamFailure(response.status)
      }

      return response
    }, signal)
  }

  try {
    const response =
      request.method === 'GET'
        ? await executeWithCancellation(signal, () =>
            readRetry.execute(send, signal),
          )
        : await send()
    const data: unknown = await response.json()
    return { status: response.status, data }
  } catch (error) {
    if (request.signal.aborted) {
      throw request.signal.reason
    }
    if (
      !(error instanceof UpstreamFailure) &&
      !(error instanceof BrokenCircuitError) &&
      !signal.aborted
    ) {
      throw error
    }

    console.error('Backend request failed', {
      service,
      status: error instanceof UpstreamFailure ? error.status : undefined,
      circuitOpen: error instanceof BrokenCircuitError,
    })
    throw new HttpError(
      502,
      'backend_unavailable',
      'The booking system is unavailable. Submission may have succeeded; retry status before submitting again.',
    )
  }
}
