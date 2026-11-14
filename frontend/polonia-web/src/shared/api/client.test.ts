import { afterEach, describe, expect, it, vi } from 'vitest'
import { recoverReservation, searchFlights } from './client'

afterEach(() => vi.unstubAllGlobals())

describe('web API client', () => {
  it('treats only an actual not-found response as a reservation that has not committed yet', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
    fetchMock.mockResolvedValue(
      Response.json({ code: 'not_found' }, { status: 404 }),
    )
    expect(
      await recoverReservation('00112233-4455-6677-8899-AABBCCDDEEFF'),
    ).toBeNull()
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/web/reservations/by-request/web-00112233-4455-6677-8899-aabbccddeeff',
    )
    fetchMock.mockResolvedValue(
      Response.json(
        { code: 'route_not_found', detail: 'API route missing' },
        { status: 404 },
      ),
    )
    await expect(recoverReservation('offer')).rejects.toThrow(
      'API route missing',
    )
    fetchMock.mockResolvedValue(
      Response.json({ detail: 'Backend unavailable' }, { status: 502 }),
    )
    await expect(recoverReservation('offer')).rejects.toThrow(
      'Backend unavailable',
    )
  })

  it('sends exact search criteria with a bounded page size', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(Response.json([]))
    vi.stubGlobal('fetch', fetchMock)
    await searchFlights({
      from: 'ORD',
      to: 'KRK',
      departure: '2026-12-01',
      page: 2,
    })
    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      '/api/web/flights?DepartureAirport=ORD&ArrivalAirport=KRK&DepartureDate=2026-12-01&Page=2&PageSize=20',
    )
  })

  it('does not fall back to sample flights for an invalid backend response', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(Response.json([{ flightNumber: 'DEMO' }])),
    )
    await expect(
      searchFlights({ from: 'ORD', to: 'KRK', departure: '2026-12-01', page: 1 }),
    ).rejects.toThrow('Invalid flight search response')
  })
})
