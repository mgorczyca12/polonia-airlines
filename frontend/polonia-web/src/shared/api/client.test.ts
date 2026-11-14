import { afterEach, describe, expect, it, vi } from 'vitest'
import { recoverReservation, searchFlights } from './client'
import { isConfirmed } from './presentation'
import { parseLiveSearchParams } from '#/features/flight-search/model/search-params'

afterEach(() => vi.unstubAllGlobals())

describe('live booking boundaries', () => {
  it('does not treat pending or partially completed inventory as a confirmed booking', () => {
    expect(isConfirmed({ status: 'Created', workflowStatus: 'Pending' })).toBe(
      false,
    )
    expect(
      isConfirmed({ status: 'Created', workflowStatus: 'Completed' }),
    ).toBe(false)
    expect(
      isConfirmed({ status: 'Cancelled', workflowStatus: 'Completed' }),
    ).toBe(false)
    expect(
      isConfirmed({ status: 'Reserved', workflowStatus: 'Completed' }),
    ).toBe(true)
  })

  it('rejects stale unsupported journey and traveller parameters', () => {
    expect(parseLiveSearchParams({ tripType: 'roundTrip' }).unsupported).toBe(
      true,
    )
    expect(parseLiveSearchParams({ infants: 1 }).unsupported).toBe(true)
    expect(parseLiveSearchParams({ adults: 9 }).unsupported).toBe(true)
    expect(parseLiveSearchParams({ adults: 8 }).unsupported).not.toBe(true)
    expect(parseLiveSearchParams({ adults: -1 }).unsupported).toBe(true)
    expect(
      parseLiveSearchParams({
        adults: 1,
        children: 1,
        infants: 0,
        tripType: 'oneWay',
      }).unsupported,
    ).not.toBe(true)
    expect(parseLiveSearchParams({ unsupported: true }).unsupported).toBe(true)
  })

  it('treats only an actual not-found response as a reservation that has not committed yet', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)
    fetchMock.mockResolvedValue(
      Response.json({ code: 'not_found' }, { status: 404 }),
    )
    expect(
      await recoverReservation(
        '00112233-4455-6677-8899-AABBCCDDEEFF',
        new AbortController().signal,
      ),
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
    await expect(
      recoverReservation('offer', new AbortController().signal),
    ).rejects.toThrow('API route missing')
    fetchMock.mockResolvedValue(
      Response.json({ detail: 'Backend unavailable' }, { status: 502 }),
    )
    await expect(
      recoverReservation('offer', new AbortController().signal),
    ).rejects.toThrow('Backend unavailable')
  })

  it('does not fall back to sample flights for an invalid backend response', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(Response.json([{ flightNumber: 'DEMO' }])),
    )
    await expect(
      searchFlights(new URLSearchParams(), new AbortController().signal),
    ).rejects.toThrow('Invalid flight search response')
  })
})
