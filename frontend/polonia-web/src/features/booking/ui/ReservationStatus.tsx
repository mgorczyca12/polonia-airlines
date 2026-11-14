import { Link } from '@tanstack/react-router'
import { ActionIcon, Alert, Button } from '@mantine/core'
import {
  Check,
  CircleAlert,
  CircleCheck,
  Clock3,
  ContactRound,
  Copy,
  Headset,
  PlaneLanding,
  RefreshCw,
  UserRound,
} from 'lucide-react'
import { useState } from 'react'
import type { FlightSummary } from '#/entities/flight'
import { deriveReservationView } from '#/entities/reservation'
import type { ReservationView } from '#/entities/reservation'
import type { ReservationResponse } from '#/shared/api'
import { formatMoney } from '#/shared/lib/formatting'
import { PassengerBaggageAllowance } from './PassengerBaggageAllowance'
import { ReservationItinerary } from './ReservationItinerary'
import { statusCopy } from './reservation-status-copy'
import styles from './ReservationStatus.module.scss'

function StatusIcon({ view }: { view: ReservationView }) {
  if (view.kind === 'confirmed') {
    return (
      <span className={styles.successIcon}>
        <CircleCheck size={24} aria-hidden="true" />
      </span>
    )
  }

  const Icon = view.kind === 'pending' ? Clock3 : CircleAlert
  return (
    <span className={styles.pendingIcon}>
      <Icon size={24} aria-hidden="true" />
    </span>
  )
}

interface ReservationStatusProps {
  reservation: ReservationResponse
  flight?: FlightSummary
  flightError: string | null
  statusProblem: string | null
  isRefreshing: boolean
  onRefreshStatus: () => void
}

export function ReservationStatus({
  reservation,
  flight,
  flightError,
  statusProblem,
  isRefreshing,
  onRefreshStatus,
}: ReservationStatusProps) {
  const [copyState, setCopyState] = useState<'idle' | 'copied' | 'failed'>(
    'idle',
  )
  const view = deriveReservationView(reservation)
  const copy = statusCopy(view)
  const fares = [...reservation.fares].sort(
    (first, second) => first.passengerIndex - second.passengerIndex,
  )
  const travellerCount = reservation.passengers.length

  const copyReference = async () => {
    try {
      await navigator.clipboard.writeText(reservation.code)
      setCopyState('copied')
    } catch {
      setCopyState('failed')
    }
  }

  return (
    <div className={styles.confirmation}>
      <div className={styles.heading}>
        <StatusIcon view={view} />
        <div className={styles.headingCopy}>
          <p className={styles.eyebrow}>{copy.eyebrow}</p>
          <h1>{copy.title}</h1>
          <p>
            No payment was collected and no ticket has been issued. Keep your
            reservation reference to check this booking.
          </p>
        </div>
        <div className={styles.reference}>
          <span>Reservation reference</span>
          <strong>{reservation.code}</strong>
          <ActionIcon
            variant="subtle"
            color="gray"
            aria-label={
              copyState === 'copied' ? 'Reference copied' : 'Copy reference'
            }
            onClick={() => void copyReference()}
          >
            <Copy size={16} aria-hidden="true" />
          </ActionIcon>
          {copyState === 'copied' && (
            <span className={styles.copiedMessage} role="status">
              Copied
            </span>
          )}
          {copyState === 'failed' && (
            <span className={styles.copyError} role="status">
              Could not copy reference
            </span>
          )}
        </div>
      </div>

      {copy.alert && (
        <Alert color={copy.alert.color}>{copy.alert.message}</Alert>
      )}
      {statusProblem && (
        <Alert color="yellow" title="Status not updated">
          {statusProblem}
          <Button
            ml="md"
            variant="outline"
            loading={isRefreshing}
            onClick={onRefreshStatus}
          >
            Retry status
          </Button>
        </Alert>
      )}

      <div className={styles.columns}>
        <div className={styles.main}>
          <ReservationItinerary flight={flight} flightError={flightError} />

          <section
            className={styles.panel}
            aria-labelledby="travellers-heading"
          >
            <div className={styles.panelHeading}>
              <h2 id="travellers-heading">Your travellers</h2>
              <ContactRound size={17} aria-hidden="true" />
            </div>
            <ul className={styles.passengerList}>
              {reservation.passengers.map((passenger, index) => {
                const fare = fares.at(index)

                return (
                  <li key={passenger.id}>
                    <UserRound size={15} aria-hidden="true" />
                    <div className={styles.passengerDetails}>
                      <span>
                        {passenger.firstName} {passenger.lastName}
                      </span>
                      {fare && (
                        <>
                          <span className={styles.passengerMeta}>
                            {fare.cabinClass} · {fare.brand}
                          </span>
                          <PassengerBaggageAllowance
                            baggage={fare.baggage}
                            currency={fare.currency}
                          />
                        </>
                      )}
                    </div>
                  </li>
                )
              })}
            </ul>
            <p className={styles.contact}>
              Contact: {reservation.contactName} · {reservation.contactEmail}
            </p>
          </section>
        </div>

        <aside className={styles.sidebar}>
          <section className={styles.summary} aria-labelledby="summary-heading">
            <h2 id="summary-heading">Your journey</h2>
            {flight && (
              <p className={styles.summaryRoute}>
                {flight.departure.airportCode} → {flight.arrival.airportCode}
              </p>
            )}
            <p className={styles.summaryFlight}>
              {flight ? `${flight.flightNumber} · ` : ''}
              {travellerCount} {travellerCount === 1 ? 'traveller' : 'travellers'}
            </p>
            <div className={styles.summaryItems}>
              {fares.map((fare) => (
                <p key={fare.passengerIndex}>
                  <span>
                    Traveller {fare.passengerIndex + 1} · {fare.brand}
                  </span>
                  <span>{formatMoney(fare.amount, fare.currency)}</span>
                </p>
              ))}
            </div>
            <div className={styles.total}>
              <span>Reservation total · {reservation.currency}</span>
              <strong>
                {formatMoney(reservation.totalAmount, reservation.currency)}
              </strong>
            </div>
            <p className={styles.paymentNotice}>
              <Check size={14} aria-hidden="true" />
              No payment was taken
            </p>
          </section>

          <Button
            variant="default"
            leftSection={<RefreshCw size={16} aria-hidden="true" />}
            loading={isRefreshing}
            onClick={onRefreshStatus}
          >
            Refresh reservation status
          </Button>
          <Button
            component={Link}
            to="/"
            variant="subtle"
            leftSection={<PlaneLanding size={16} aria-hidden="true" />}
          >
            Search another flight
          </Button>

          <section className={styles.help}>
            <Headset size={18} aria-hidden="true" />
            <div>
              <h2>Need help with this booking?</h2>
              <p>
                Quote reservation reference {reservation.code} when you contact
                Polonia Airlines.
              </p>
            </div>
          </section>
        </aside>
      </div>
    </div>
  )
}
