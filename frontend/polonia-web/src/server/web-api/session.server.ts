import { getIronSession, webCookies } from 'iron-session'
import type { IronSession } from 'iron-session'
import type { WebApiConfig } from './config.server'
import { HttpError } from './errors.server'

const SESSION_TTL_SECONDS = 86400

export interface GuestSessionData {
  guestToken?: string
}

export type GuestSession = IronSession<GuestSessionData>

/** Opens the encrypted HttpOnly guest cookie. The guest token never reaches browser code. */
export function openGuestSession(
  request: Request,
  responseHeaders: Headers,
  config: WebApiConfig,
): Promise<GuestSession> {
  return getIronSession<GuestSessionData>(
    webCookies(request, responseHeaders),
    {
      cookieName: 'polonia-guest',
      password: config.password,
      ttl: SESSION_TTL_SECONDS,
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
}

export function requireGuestToken(session: GuestSession): string {
  if (!session.guestToken) {
    throw new HttpError(
      401,
      'guest_session_expired',
      'Your guest session has expired. Start a new search.',
    )
  }

  return session.guestToken
}
