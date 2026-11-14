import { describe, expect, it } from 'vitest'
import {
  describeCheckedBaggage,
  faresForFlight,
  groupFaresByCabin,
} from './fare.model'
import type { Fare } from './fare.model'

const baggage = {
  includedPieces: 1,
  maxWeightPerPieceKg: 23,
  maxExtraPieces: 2,
  extraPiecePrice: 50,
}

function fare(overrides: Partial<Fare>): Fare {
  return {
    productId: 'product',
    flightId: '1',
    cabinClass: 'Economy',
    brand: 'Light',
    amount: 100,
    currency: 'USD',
    baggage,
    ...overrides,
  }
}

describe('fare model', () => {
  it('groups fares by cabin with the cheapest cabin and fare first', () => {
    const cabins = groupFaresByCabin([
      fare({ productId: 'b-flex', cabinClass: 'Business', amount: 900 }),
      fare({ productId: 'e-flex', brand: 'Flex', amount: 250 }),
      fare({ productId: 'e-light', brand: 'Light', amount: 120 }),
    ])

    expect(cabins.map((cabin) => cabin.cabinClass)).toEqual([
      'Economy',
      'Business',
    ])
    expect(cabins[0].fares.map((item) => item.productId)).toEqual([
      'e-light',
      'e-flex',
    ])
    expect(cabins[0].lowestAmount).toBe(120)
  })

  it('matches fares to a numeric flight ID by its canonical string form', () => {
    const fares = [fare({ flightId: '1' }), fare({ flightId: '12' })]

    expect(faresForFlight(fares, 1)).toEqual([fares[0]])
  })

  it('describes included checked baggage from the fare terms', () => {
    expect(describeCheckedBaggage(baggage)).toBe(
      '1 checked bag, up to 23 kg each',
    )
    expect(describeCheckedBaggage({ ...baggage, includedPieces: 0 })).toBe(
      'No checked bags included',
    )
  })
})
