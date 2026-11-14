import { getAirportByCode } from '#/entities/airport'

export function airportLabel(code: string) {
  const airport = getAirportByCode(code)
  return airport ? `${airport.City} (${code})` : code
}

export function formatMoney(amount: number, currency: string) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(
    amount,
  )
}

export function formatUtc(timestamp: string) {
  return `${new Intl.DateTimeFormat('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'UTC',
  }).format(new Date(timestamp))} UTC`
}

export function isConfirmed(reservation: {
  status: string
  workflowStatus: string
}) {
  return (
    reservation.status === 'Reserved' &&
    reservation.workflowStatus === 'Completed'
  )
}

export function isTerminal(reservation: {
  status: string
  workflowStatus: string
}) {
  return ['Completed', 'Cancelled', 'RequiresIntervention'].includes(
    reservation.workflowStatus,
  )
}
