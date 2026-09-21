import { createFileRoute } from '@tanstack/react-router'
import { Box, Container, Text, Title } from '@mantine/core'
import { FlightResults } from '#/features/flight-results/components/FlightResults'

interface FlightListSearch {
  from?: string
  to?: string
  departure?: string
  return?: string
  tripType?: 'roundTrip' | 'oneWay'
}

function FlightListingRoute() {
  const { from, to } = Route.useSearch()

  return (
    <Container>
      <Box>
        <Text c="dimmed" size="sm">Available flights</Text>
        <Title order={2} size="h3">Search results</Title>
      </Box>
      <FlightResults from={from} to={to} />
    </Container>
  )
}

export const Route = createFileRoute('/flightlist')({
  validateSearch: (search: Record<string, unknown>): FlightListSearch => ({
    from: typeof search.from === 'string' ? search.from : undefined,
    to: typeof search.to === 'string' ? search.to : undefined,
    departure: typeof search.departure === 'string' ? search.departure : undefined,
    return: typeof search.return === 'string' ? search.return : undefined,
    tripType: search.tripType === 'oneWay' ? 'oneWay' : 'roundTrip',
  }),
  component: FlightListingRoute,
})
