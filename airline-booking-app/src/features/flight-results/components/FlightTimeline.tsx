import { Box, Text, UnstyledButton } from '@mantine/core'
import type { CSSProperties } from 'react'
import type { Flight } from '../model/flight.types'
import styles from '../styles/FlightTimeline.module.scss'

interface FlightTimelineProps {
  flight: Flight
  detailsOpened: boolean
  onToggleDetails: () => void
}

function formatFlightTime(dateTime: string): string {
  const time = dateTime.split(' ')[1]

  if (!time) {
    return dateTime
  }

  const [hours, minutes] = time.split(':').map(Number)
  const period = hours >= 12 ? 'PM' : 'AM'
  const displayHours = hours % 12 || 12

  return `${displayHours}:${String(minutes).padStart(2, '0')} ${period}`
}

export function FlightTimeline({ flight, detailsOpened, onToggleDetails }: FlightTimelineProps) {
  const firstSegment = flight.segments[0]
  const lastSegment = flight.segments[flight.segments.length - 1]

  if (!firstSegment || !lastSegment) {
    return null
  }

  return (
    <Box className={styles.timeline}>
      <Box className={styles.route}>
        <Text className={styles.endpoint} fw={700} size="lg">
          {formatFlightTime(firstSegment.departureDateTimeUtc)} {flight.departureAirport.airportCode}
        </Text>
        <Box className={styles.track}>
          <Box className={styles.line} />
          {flight.layovers.map((_, index) => (
            <Box
              key={`${flight.flightNumber}-layover-${index}`}
              className={styles.stopMarker}
              style={{ '--stop-position': `${((index + 1) / (flight.layovers.length + 1)) * 100}%` } as CSSProperties}
            />
          ))}
        </Box>
        <Text className={styles.endpoint} fw={700} size="lg">
          {formatFlightTime(lastSegment.arrivalDateTimeUtc)} {flight.arrivalAirport.airportCode}
        </Text>
      </Box>
      <Box className={styles.metadata}>
        <Text size="xs" c="dimmed">
          Duration: {Math.floor(flight.totalDurationMinutes / 60)}h {flight.totalDurationMinutes % 60}m
        </Text>
        <Text size="xs" c="dimmed">•</Text>
        <Text className={styles.stopText} size="xs" fw={600}>
          {flight.isNonStop ? 'Nonstop' : `${flight.layovers.length} stop${flight.layovers.length === 1 ? '' : 's'}`}
        </Text>
        <UnstyledButton className={styles.detailsButton} onClick={onToggleDetails}>
          <Text className={styles.detailsText} size="xs" fw={500}>
            {detailsOpened ? 'Hide flight details' : 'View flight details'}
          </Text>
        </UnstyledButton>
      </Box>
    </Box>
  )
}