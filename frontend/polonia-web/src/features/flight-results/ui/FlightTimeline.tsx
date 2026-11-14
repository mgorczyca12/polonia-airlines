import { Plane } from 'lucide-react'
import type { FlightSummary } from '#/entities/flight'
import { formatDuration, formatTime } from '#/shared/lib/formatting'
import styles from './FlightTimeline.module.scss'

interface FlightTimelineProps {
  flight: FlightSummary
}

export function FlightTimeline({ flight }: FlightTimelineProps) {
  const { departure, arrival, arrivalDayOffset } = flight

  return (
    <div className={styles.schedule}>
      <div className={styles.endpoint}>
        <p className={styles.time}>{formatTime(departure.time)}</p>
        <p className={styles.place}>
          {departure.airportCode} · {departure.city}
        </p>
      </div>

      <div className={styles.journey}>
        <p className={styles.duration}>
          <span className="polonia-visually-hidden">Journey time </span>
          {formatDuration(flight.durationMinutes)}
        </p>
        <div className={styles.path} aria-hidden="true">
          <span className={styles.line} />
          <Plane className={styles.plane} size={17} strokeWidth={1.75} />
          <span className={styles.line} />
        </div>
        <p className={styles.timeZone}>Times in UTC</p>
      </div>

      <div className={styles.endpoint}>
        <p className={styles.time}>
          {formatTime(arrival.time)}
          {arrivalDayOffset > 0 && (
            <sup className={styles.dayOffset}>
              +{arrivalDayOffset}
              <span className="polonia-visually-hidden">
                {` day${arrivalDayOffset === 1 ? '' : 's'} later`}
              </span>
            </sup>
          )}
        </p>
        <p className={styles.place}>
          {arrival.airportCode} · {arrival.city}
        </p>
      </div>

      <span className={styles.divider} aria-hidden="true" />

      <p className={styles.flightNumber}>{flight.flightNumber}</p>
    </div>
  )
}
