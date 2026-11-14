import { describe, expect, it } from 'vitest'
import { deriveReservationView, isConfirmed } from './reservation.rules'

describe('reservation rules', () => {
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

  it.each([
    ['Reserved', 'Completed', 'confirmed'],
    ['Created', 'Completed', 'inconsistent'],
    ['Created', 'Pending', 'pending'],
    ['Created', 'AwaitingCapacity', 'pending'],
    ['Created', 'ConfirmingAllocations', 'pending'],
    ['Created', 'Compensating', 'compensating'],
    ['Created', 'RequiresIntervention', 'intervention'],
    ['Cancelled', 'Cancelled', 'cancelled'],
  ])('maps %s/%s to %s', (status, workflowStatus, kind) => {
    expect(deriveReservationView({ status, workflowStatus }).kind).toBe(kind)
  })

  it('reports the failure code for a cancelled reservation, with a safe fallback', () => {
    expect(
      deriveReservationView({
        status: 'Cancelled',
        workflowStatus: 'Cancelled',
        failureCode: 'insufficient_capacity',
      }),
    ).toEqual({ kind: 'cancelled', reason: 'insufficient_capacity' })
    expect(
      deriveReservationView({
        status: 'Cancelled',
        workflowStatus: 'Cancelled',
        failureCode: null,
      }),
    ).toEqual({ kind: 'cancelled', reason: 'inventory could not be confirmed' })
  })
})
