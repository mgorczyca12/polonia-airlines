import { UnstyledButton } from '@mantine/core'
import { ChevronDown, Plane } from 'lucide-react'
import { formatDuration, formatTime } from '#/shared/lib/formatting'
import {
  getAircraftLabel,
  getCityName,
  getFlightNumbersLabel,
  getFlightTimes,
  getStopsLabel,
} from '../model/flight.helpers'
import type { Flight } from '../model/flight.types'
import styles from '../styles/FlightTimeline.module.scss'

interface FlightTimelineProps {
  flight: Flight
  detailsOpened: boolean
  detailsId: string
  onToggleDetails: () => void
}

export function FlightTimeline({
  flight,
  detailsOpened,
  detailsId,
  onToggleDetails,
}: FlightTimelineProps) {
  const times = getFlightTimes(flight)

  if (!times) {
    return null
  }

  const departureCode = flight.departureAirport.airportCode
  const arrivalCode = flight.arrivalAirport.airportCode
  const dayOffset = times.arrivalDayOffset

  return (
    <div className={styles.schedule}>
      <div className={styles.endpoint}>
        <p className={styles.time}>{formatTime(times.departure)}</p>
        <p className={styles.place}>
          {departureCode} · {getCityName(departureCode)}
        </p>
      </div>

      <div className={styles.journey}>
        <p className={styles.duration}>
          <span className="polonia-visually-hidden">Journey time </span>
          {formatDuration(flight.totalDurationMinutes)}
        </p>
        <div className={styles.path} aria-hidden="true">
          <span className={styles.line} />
          <Plane className={styles.plane} size={17} strokeWidth={1.75} />
          <span className={styles.line} />
        </div>
        <p className={flight.isNonStop ? styles.nonStop : styles.stops}>
          {getStopsLabel(flight)}
        </p>
      </div>

      <div className={styles.endpoint}>
        <p className={styles.time}>
          {formatTime(times.arrival)}
          {dayOffset > 0 && (
            <sup className={styles.dayOffset}>
              +{dayOffset}
              <span className="polonia-visually-hidden">
                {` day${dayOffset === 1 ? '' : 's'} later`}
              </span>
            </sup>
          )}
        </p>
        <p className={styles.place}>
          {arrivalCode} · {getCityName(arrivalCode)}
        </p>
      </div>

      <span className={styles.divider} aria-hidden="true" />

      <div className={styles.aircraft}>
        <p className={styles.flightNumber}>{getFlightNumbersLabel(flight)}</p>
        <p className={styles.aircraftType}>{getAircraftLabel(flight)}</p>
        <UnstyledButton
          className={styles.detailsButton}
          aria-expanded={detailsOpened}
          aria-controls={detailsId}
          onClick={onToggleDetails}
        >
          {detailsOpened ? 'Hide details' : 'Flight details'}
          <ChevronDown
            className={detailsOpened ? styles.chevronOpen : undefined}
            size={13}
            aria-hidden="true"
          />
        </UnstyledButton>
      </div>
    </div>
  )
}
