import { Box, Text } from '@mantine/core'
import { getFlights } from '../data/flight.repository'
import styles from '../styles/FlightResults.module.scss'
import { FlightResultCard } from './FlightResultCard'

interface FlightResultsProps {
  from?: string
  to?: string
}

export function FlightResults({ from, to }: FlightResultsProps) {
  const flights = getFlights({ from, to })

  return (
    <Box className={styles.flightresults}>
      {flights.map((flight) => <FlightResultCard key={flight.flightNumber} flight={flight} />)}
      {flights.length === 0 && <Text c="dimmed">No flights found for this route.</Text>}
    </Box>
  )
}