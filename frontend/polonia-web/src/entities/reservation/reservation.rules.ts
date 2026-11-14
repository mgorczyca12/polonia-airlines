interface ReservationState {
  status: string
  workflowStatus: string
  failureCode?: string | null
}

const TERMINAL_WORKFLOW_STATUSES = new Set([
  'Completed',
  'Cancelled',
  'RequiresIntervention',
])

/** Only a reserved booking whose workflow completed is confirmed inventory. */
export function isConfirmed(reservation: ReservationState): boolean {
  return (
    reservation.status === 'Reserved' &&
    reservation.workflowStatus === 'Completed'
  )
}

/** Terminal workflows no longer change on their own, so polling can stop. */
export function isTerminal(reservation: ReservationState): boolean {
  return TERMINAL_WORKFLOW_STATUSES.has(reservation.workflowStatus)
}

export type ReservationView =
  | { kind: 'confirmed' }
  | { kind: 'pending' }
  | { kind: 'compensating' }
  | { kind: 'intervention' }
  | { kind: 'cancelled'; reason: string }
  | { kind: 'inconsistent' }

export function deriveReservationView(
  reservation: ReservationState,
): ReservationView {
  if (isConfirmed(reservation)) {
    return { kind: 'confirmed' }
  }

  switch (reservation.workflowStatus) {
    case 'Cancelled':
      return {
        kind: 'cancelled',
        reason: reservation.failureCode ?? 'inventory could not be confirmed',
      }
    case 'RequiresIntervention':
      return { kind: 'intervention' }
    case 'Compensating':
      return { kind: 'compensating' }
    case 'Completed':
      return { kind: 'inconsistent' }
    default:
      return { kind: 'pending' }
  }
}
