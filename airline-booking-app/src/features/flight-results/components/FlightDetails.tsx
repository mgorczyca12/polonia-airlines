import { Box, Collapse, Text } from '@mantine/core'
import type { Flight } from '../model/flight.types'
import styles from '../styles/FlightDetails.module.scss'

interface FlightDetailsProps {
  flight: Flight
  opened: boolean
}

export function FlightDetails({ flight, opened }: FlightDetailsProps) {
  return (
    <Collapse expanded={opened}>
      <Box className={styles.panel}>
        <Box className={styles.segments}>
          {flight.segments.map((segment, index) => (
            <Box className={styles.segment} key={`${flight.flightNumber}-${segment.segmentIndex}`}>
              <Box className={styles.segmentHeader}>
                <Text size="sm" fw={600}>
                  {segment.departureAirportCode} → {segment.arrivalAirportCode}
                </Text>
                <Text size="xs" c="dimmed">{segment.flightNumber}</Text>
              </Box>
              <Text size="xs" c="dimmed">
                Departs: {segment.departureDateTimeUtc} | Arrives: {segment.arrivalDateTimeUtc} | {segment.aircraftType}
              </Text>
              {flight.layovers[index] && (
                <Text className={styles.connection} mt="xs" size="xs">
                  Connection in {flight.layovers[index].airportName} ({flight.layovers[index].airportCode}) for{' '}
                  {Math.floor(flight.layovers[index].durationMinutes / 60)}h {flight.layovers[index].durationMinutes % 60}m
                </Text>
              )}
            </Box>
          ))}
        </Box>
      </Box>
    </Collapse>
  )
}