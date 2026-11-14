import { createServerOnlyFn } from '@tanstack/react-start'
import { isRecord } from '#/shared/api/guards'
import { readConfig } from './config.server'
import { errorResponse, HttpError, jsonResponse } from './errors.server'
import { resolveRoute } from './routes.server'
import { openGuestSession } from './session.server'

const API_PREFIX = '/api/web/'

/** Removes the backend's echoed guest access token from any public response. */
function withoutGuestAccessToken(data: unknown): unknown {
  if (!isRecord(data) || !('guestAccessToken' in data)) {
    return data
  }

  const { guestAccessToken: _secret, ...publicData } = data
  return publicData
}

/**
 * Same-origin backend-for-frontend for the customer web app. Resolves an
 * allowlisted route, enforces exact-Origin checks for mutations and keeps the
 * guest token in an encrypted HttpOnly cookie.
 */
export const handleWebApi = createServerOnlyFn(
  async (request: Request): Promise<Response> => {
    const headers = new Headers({
      'Cache-Control': 'no-store',
      'Content-Type': 'application/json',
    })

    try {
      const config = readConfig()

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
      const path = url.pathname.slice(API_PREFIX.length)
      const route = resolveRoute(request.method, path)

      if (!route) {
        throw new HttpError(
          404,
          'route_not_found',
          'This API route is not available.',
        )
      }

      const session =
        route.access === 'guest'
          ? await openGuestSession(request, headers, config)
          : undefined
      const result = await route.handle({ request, url, path, session })

      return jsonResponse(
        withoutGuestAccessToken(result.data),
        result.status,
        headers,
      )
    } catch (error) {
      if (request.signal.aborted) {
        throw error
      }

      return errorResponse(error, headers)
    }
  },
)
