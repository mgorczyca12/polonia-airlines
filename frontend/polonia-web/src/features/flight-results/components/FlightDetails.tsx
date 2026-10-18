import { Collapse } from '@mantine/core'
import {
  calendarDayOffset,
  formatDuration,
  formatShortDate,
  formatTime,
  parseScheduleDateTime,
  toDateKey,
} from '#/shared/lib/formatting'
import { getCityName } from '../model/flight.helpers'
import type { Flight, FlightSegment } from '../model/flight.types'
import styles from '../styles/FlightDetails.module.scss'

interface FlightDetailsProps {
  id: string
  flight: Flight
  opened: boolean
}

function SegmentTimes({ segment }: { segment: FlightSegment }) {
  const departure = parseScheduleDateTime(segment.departureDateTimeUtc)
  const arrival = parseScheduleDateTime(segment.arrivalDateTimeUtc)

  if (!departure || !arrival) {
    return null
  }

  const dayOffset = calendarDayOffset(departure, arrival)

  return (
    <p className={styles.segmentTimes}>
      {formatShortDate(toDateKey(departure))} · {formatTime(departure)} –{' '}
      {formatTime(arrival)}
      {dayOffset > 0 && ` (+${dayOffset})`}
    </p>
  )
}

export function FlightDetails({ id, flight, opened }: FlightDetailsProps) {
  return (
    <Collapse expanded={opened} id={id}>
      <ol className={styles.segments}>
        {flight.segments.map((segment, index) => {
          const layover = flight.layovers.at(index)
          const isLastSegment = index === flight.segments.length - 1

          return (
            <li
              key={`${flight.flightNumber}-${segment.segmentIndex}`}
              className={styles.segment}
            >
              <div className={styles.segmentHeader}>
                <p className={styles.segmentRoute}>
                  {getCityName(segment.departureAirportCode)} (
                  {segment.departureAirportCode}) →{' '}
                  {getCityName(segment.arrivalAirportCode)} (
                  {segment.arrivalAirportCode})
                </p>
                <p className={styles.segmentFlight}>{segment.flightNumber}</p>
              </div>
              <SegmentTimes segment={segment} />
              <p className={styles.segmentAircraft}>{segment.aircraftType}</p>
              {layover && !isLastSegment && (
                <p className={styles.connection}>
                  Connection in {layover.airportName} ({layover.airportCode}) ·{' '}
                  {formatDuration(layover.durationMinutes)}
                </p>
              )}
            </li>
          )
        })}
      </ol>
    </Collapse>
  )
}
