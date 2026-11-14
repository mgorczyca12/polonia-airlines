import { describe, expect, it } from 'vitest'
import { assignFare, selectedProductIds } from './fare-selection'

describe('fare selection', () => {
  it('assigns fares per passenger and is complete only when every passenger has one', () => {
    let selection = assignFare(null, 1, [0], 'light', 2)
    expect(selectedProductIds(selection, 2)).toBeNull()

    selection = assignFare(selection, 1, [1], 'flex', 2)
    expect(selectedProductIds(selection, 2)).toEqual(['light', 'flex'])
  })

  it('starts a new selection when another flight is chosen', () => {
    const first = assignFare(null, 1, [0, 1], 'light', 2)
    const second = assignFare(first, 2, [0], 'flex', 2)

    expect(second).toEqual({ flightId: 2, productIds: ['flex', undefined] })
    expect(selectedProductIds(second, 2)).toBeNull()
  })
})
