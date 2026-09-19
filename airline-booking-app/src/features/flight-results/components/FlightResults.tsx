import { Box, Text } from '@mantine/core'
import { getFlights } from '../data/flight.repository'
import { FlightResultCard } from './FlightResultCard'
import styles from '../styles/FlightResults.module.scss'

interface FlightResultsProps {
  from?: string
  to?: string
}

export function FlightResults({ from, to }: FlightResultsProps) {
  const flights = getFlights({ from, to })

  return (
    <Box className={styles.results}>
      {flights.map((flight) => <FlightResultCard key={flight.flightNumber} flight={flight} />)}
      {flights.length === 0 && <Text c="dimmed">No flights found for this route.</Text>}
    </Box>
  )
}