import type { ReservationView } from '#/entities/reservation'

export interface StatusCopy {
  eyebrow: string
  title: string
  alert?: { color: 'yellow' | 'red'; message: string }
}

/** Customer-facing wording for each reservation state; only `confirmed` claims success. */
export function statusCopy(view: ReservationView): StatusCopy {
  switch (view.kind) {
    case 'confirmed':
      return {
        eyebrow: 'Reservation confirmed',
        title: 'Your reservation is confirmed.',
      }
    case 'pending':
      return {
        eyebrow: 'Reservation submitted',
        title: 'Reservation status',
        alert: {
          color: 'yellow',
          message:
            'Inventory confirmation is pending. Submission alone does not confirm your reservation.',
        },
      }
    case 'compensating':
      return {
        eyebrow: 'Reservation not confirmed',
        title: 'Reservation status',
        alert: {
          color: 'yellow',
          message:
            'Inventory acquisition failed. Release is in progress; cancellation is not complete yet.',
        },
      }
    case 'intervention':
      return {
        eyebrow: 'Reservation needs attention',
        title: 'Reservation status',
        alert: {
          color: 'yellow',
          message:
            'The workflow requires operator intervention. Do not create a replacement booking while capacity release is unresolved.',
        },
      }
    case 'cancelled':
      return {
        eyebrow: 'Reservation cancelled',
        title: 'Reservation status',
        alert: {
          color: 'red',
          message: `The reservation was cancelled (${view.reason}).`,
        },
      }
    case 'inconsistent':
      return {
        eyebrow: 'Reservation needs attention',
        title: 'Reservation status',
        alert: {
          color: 'yellow',
          message:
            'The reservation and workflow states disagree. Contact support with this reference.',
        },
      }
  }
}
