import { Alert, Skeleton } from '@mantine/core'
import { CalendarPlus, Plane, PlaneTakeoff } from 'lucide-react'
import type { FlightSummary } from '#/entities/flight'
import {
  formatDuration,
  formatShortDate,
  formatTime,
} from '#/shared/lib/formatting'
import styles from './ReservationStatus.module.scss'

interface ReservationItineraryProps {
  flight?: FlightSummary
  flightError: string | null
}

function FlightLeg({ flight }: { flight: FlightSummary }) {
  return (
    <div className={styles.leg}>
      <div className={styles.legHeading}>
        <span>
          <PlaneTakeoff size={16} aria-hidden="true" />
          Outbound · {formatShortDate(flight.departureDateKey)}
        </span>
        <span className={styles.timeZone}>Times in UTC</span>
      </div>
      <div className={styles.route}>
        <div className={styles.endpoint}>
          <p>
            {formatTime(flight.departure.time)}{' '}
            <strong>{flight.departure.airportCode}</strong>
          </p>
          <span>{flight.departure.city}</span>
        </div>
        <div className={styles.journey}>
          <span>{formatDuration(flight.durationMinutes)}</span>
          <span className={styles.routeLine}>
            <i />
            <Plane size={15} aria-hidden="true" />
            <i />
          </span>
        </div>
        <div className={styles.endpoint}>
          <p>
            {formatTime(flight.arrival.time)}
            {flight.arrivalDayOffset > 0 && (
              <sup> +{flight.arrivalDayOffset}</sup>
            )}{' '}
            <strong>{flight.arrival.airportCode}</strong>
          </p>
          <span>{flight.arrival.city}</span>
        </div>
      </div>
      <p className={styles.flightDetail}>{flight.flightNumber}</p>
    </div>
  )
}

/** Itinerary panel. A failed flight read is reported here without affecting reservation state. */
export function ReservationItinerary({
  flight,
  flightError,
}: ReservationItineraryProps) {
  let body = (
    <div className={styles.leg} aria-busy="true" aria-label="Loading itinerary">
      <Skeleton height={20} width="60%" />
      <Skeleton height={36} />
    </div>
  )

  if (flight) {
    body = <FlightLeg flight={flight} />
  } else if (flightError) {
    body = (
      <Alert color="yellow" m="md">
        Itinerary details could not be loaded: {flightError}
      </Alert>
    )
  }

  return (
    <section className={styles.panel} aria-labelledby="itinerary-heading">
      <div className={styles.panelHeading}>
        <h2 id="itinerary-heading">Your flight itinerary</h2>
        <CalendarPlus size={17} aria-hidden="true" />
      </div>
      {body}
    </section>
  )
}
