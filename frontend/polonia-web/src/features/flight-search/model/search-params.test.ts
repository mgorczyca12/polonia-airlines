import { describe, expect, it } from 'vitest'
import { parseLiveSearchParams } from './search-params'

describe('live search parameters', () => {
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
})
