import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getIronSession, webCookies } from 'iron-session'
import { handleWebApi } from './web-api.server'

const TOKEN = 'a'.repeat(64)
const OFFER = '12345678-1234-1234-1234-123456789abc'
const PASSWORD = 'test-only-session-password-with-at-least-32-characters'

function apiRequest(
  method: string,
  path: string,
  body?: unknown,
  cookie?: string,
  origin = 'http://localhost:3000',
) {
  return new Request(`http://localhost:3000/api/web/${path}`, {
    method,
    headers: {
      Origin: origin,
      ...(cookie ? { Cookie: cookie } : {}),
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

async function checkout() {
  const response = await handleWebApi(apiRequest('POST', 'checkout', {}))
  const cookie = response.headers.get('set-cookie')?.split(';')[0]
  expect(response.status).toBe(200)
  expect(cookie).toBeDefined()
  return { response, cookie: cookie! }
}

describe('guest web API', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubEnv('WEB_SESSION_PASSWORD', PASSWORD)
    vi.stubEnv('WEB_ORIGIN', 'http://localhost:3000')
    vi.stubEnv('FLIGHT_API_URL', 'http://flight.test')
    vi.stubEnv('BOOKING_API_URL', 'http://booking.test')
    vi.stubGlobal('fetch', fetchMock)
    fetchMock.mockReset()
    fetchMock.mockResolvedValue(Response.json({ guestToken: TOKEN }))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.unstubAllEnvs()
    vi.restoreAllMocks()
  })

  it('encrypts the token in an HttpOnly strict cookie without returning it to JavaScript', async () => {
    const { response, cookie } = await checkout()
    const header = response.headers.get('set-cookie')!
    expect(header).toContain('HttpOnly')
    expect(header).toContain('SameSite=Strict')
    expect(header).not.toContain(TOKEN)
    expect(await response.json()).toEqual({ ready: true })
    expect(response.headers.get('cache-control')).toBe('no-store')

    const session = await getIronSession<{ guestToken: string }>(
      webCookies(apiRequest('GET', 'offers', undefined, cookie), new Headers()),
      { password: PASSWORD, cookieName: 'polonia-guest', ttl: 86400 },
    )
    expect(session.guestToken).toBe(TOKEN)
  })

  it('requires the configured origin on mutations before contacting a backend', async () => {
    const response = await handleWebApi(
      apiRequest('POST', 'checkout', {}, undefined, 'https://other.test'),
    )
    expect(response.status).toBe(403)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('uses Secure cookies for an HTTPS web origin', async () => {
    vi.stubEnv('WEB_ORIGIN', 'https://polonia.test')
    const response = await handleWebApi(
      apiRequest('POST', 'checkout', {}, undefined, 'https://polonia.test'),
    )
    expect(response.headers.get('set-cookie')).toContain('Secure')
  })

  it('rejects an invalid or expired guest cookie rather than calling the booking API anonymously', async () => {
    vi.spyOn(console, 'warn').mockImplementation(() => {})
    const response = await handleWebApi(
      apiRequest('GET', `offers/${OFFER}`, undefined, 'polonia-guest=tampered'),
    )
    expect(response.status).toBe(401)
    expect(fetchMock).not.toHaveBeenCalled()
    expect(console.warn).toHaveBeenCalled()
  })

  it('passes the secret only upstream and strips the echoed token from reservation responses', async () => {
    const { cookie } = await checkout()
    fetchMock.mockResolvedValue(
      Response.json({ id: 1, code: 'ABC123', guestAccessToken: TOKEN }),
    )
    const response = await handleWebApi(
      apiRequest('GET', 'reservations/1', undefined, cookie),
    )
    expect(await response.json()).toEqual({ id: 1, code: 'ABC123' })
    expect(fetchMock.mock.calls.at(-1)?.[1]?.headers).toMatchObject({
      'X-Guest-Token': TOKEN,
    })
  })

  it('uses the same submission identity for the same offer, ignoring a caller-supplied request ID', async () => {
    const { cookie } = await checkout()
    fetchMock.mockResolvedValue(Response.json({ id: 1 }))
    const body = {
      offerId: OFFER,
      requestId: 'attempt-to-change-identity',
      contactName: 'Guest',
      contactEmail: 'guest@example.test',
      passengers: [
        { firstName: 'First', lastName: 'Last', dateOfBirth: '1990-01-01' },
      ],
    }
    await handleWebApi(apiRequest('POST', 'reservations', body, cookie))
    await handleWebApi(
      apiRequest(
        'POST',
        'reservations',
        { ...body, offerId: OFFER.toUpperCase() },
        cookie,
      ),
    )
    const requests = fetchMock.mock.calls
      .slice(1)
      .map((call) => JSON.parse(String(call[1]?.body)))
    expect(requests.map((request) => request.requestId)).toEqual([
      `web-${OFFER}`,
      `web-${OFFER}`,
    ])
  })

  it('rejects infant passengers and multi-flight selections without contacting the mutation endpoint', async () => {
    const { cookie } = await checkout()
    fetchMock.mockClear()
    const infant = await handleWebApi(
      apiRequest(
        'POST',
        'reservations',
        {
          offerId: OFFER,
          contactName: 'Guest',
          contactEmail: 'guest@example.test',
          passengers: [
            {
              firstName: 'First',
              lastName: 'Last',
              dateOfBirth: new Date().toISOString().slice(0, 10),
            },
          ],
        },
        cookie,
      ),
    )
    expect(infant.status).toBe(400)
    const multipleFlights = await handleWebApi(
      apiRequest(
        'POST',
        'offers',
        {
          currency: 'USD',
          passengerCount: 2,
          selections: [
            { flightId: '1', productId: OFFER, passengerIndex: 0 },
            { flightId: '2', productId: OFFER, passengerIndex: 1 },
          ],
        },
        cookie,
      ),
    )
    expect(multipleFlights.status).toBe(400)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('does not proxy administrative routes', async () => {
    const { cookie } = await checkout()
    fetchMock.mockClear()
    const response = await handleWebApi(
      apiRequest('POST', 'configurations', {}, cookie),
    )
    expect(response.status).toBe(404)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('bounds JSON requests by encoded bytes, including non-ASCII input', async () => {
    const { cookie } = await checkout()
    const body = {
      currency: 'USD',
      passengerCount: 1,
      selections: [{ flightId: '1', productId: OFFER, passengerIndex: 0 }],
      padding: '',
    }
    const remaining =
      16384 - new TextEncoder().encode(JSON.stringify(body)).byteLength
    body.padding =
      '\u00e9'.repeat(Math.floor(remaining / 2)) + 'a'.repeat(remaining % 2)
    fetchMock.mockResolvedValue(Response.json({ id: OFFER }, { status: 201 }))
    const accepted = await handleWebApi(
      apiRequest('POST', 'offers', body, cookie),
    )
    expect(accepted.status).toBe(201)

    fetchMock.mockClear()
    body.padding += 'a'
    const response = await handleWebApi(
      apiRequest('POST', 'offers', body, cookie),
    )
    expect(response.status).toBe(413)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('returns a safe failure rather than backend exception details or fake success', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    fetchMock.mockResolvedValue(
      Response.json({ detail: 'private database exception' }, { status: 500 }),
    )
    const response = await handleWebApi(
      apiRequest('GET', 'flights?DepartureAirport=ORD'),
    )
    expect(response.status).toBe(502)
    expect(await response.text()).not.toContain('private database')
    expect(console.error).toHaveBeenCalled()
  })
})
