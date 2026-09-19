import { Box, Card, List, Text } from '@mantine/core'
import type { Flight } from '#/features/flight-results'
import type { BrandedFare } from '#/features/flight-results/model/flight.types'
import styles from '../styles/BookingSummary.module.scss'

interface BookingSummaryProps {
  flight: Flight
  fare: BrandedFare
}

export function BookingSummary({ flight, fare }: BookingSummaryProps) {
  return (
    <Card withBorder p="lg">
      <Box className={styles.summary}>
        <Box className={styles.route}>
          <Text fw={700}>{flight.departureAirport.airportCode} → {flight.arrivalAirport.airportCode}</Text>
          <Text c="dimmed">{flight.flightNumber}</Text>
        </Box>
        <div>
          <Text fw={600}>{fare.fareName}</Text>
          <Text size="xl" fw={700}>${fare.farePriceUsd.toLocaleString()}</Text>
        </div>
        <List size="sm" spacing="xs">
          {fare.perks.map((perk) => <List.Item key={perk}>{perk}</List.Item>)}
        </List>
      </Box>
    </Card>
  )
}