import { Box, Card, List, Text } from '@mantine/core'
import type { Flight } from '#/features/flight-results'
import type { BrandedFare } from '#/features/flight-results/model/flight.types'

interface BookingSummaryProps {
  flight: Flight
  fares: BrandedFare[]
}

export function BookingSummary({ flight, fares }: BookingSummaryProps) {
  const total = fares.reduce((sum, fare) => sum + fare.farePriceUsd, 0)

  return (
    <Card withBorder p="lg">
      <Box>
        <Box>
          <Text fw={700}>
            {flight.departureAirport.airportCode} →{' '}
            {flight.arrivalAirport.airportCode}
          </Text>
          <Text c="dimmed">{flight.flightNumber}</Text>
        </Box>
        {fares.map((fare, index) => (
          <div key={index}>
            <Text fw={600}>
              Passenger {index + 1} · {fare.fareName}
            </Text>
            <Text size="xl" fw={700}>
              ${fare.farePriceUsd.toLocaleString()}
            </Text>
            <List size="sm" spacing="xs">
              {fare.perks.map((perk) => (
                <List.Item key={perk}>{perk}</List.Item>
              ))}
            </List>
          </div>
        ))}
        <Text fw={700}>Fare total · ${total.toLocaleString()}</Text>
      </Box>
    </Card>
  )
}
