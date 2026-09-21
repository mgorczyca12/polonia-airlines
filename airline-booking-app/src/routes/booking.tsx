import { createFileRoute } from '@tanstack/react-router'
import { Alert, Box, Button, Container, Text, Title } from '@mantine/core'
import { Link } from '@tanstack/react-router'
import { BookingSummary, PassengerDetailsForm } from '#/features/booking'
import { getFlightByNumber } from '#/features/flight-results/data/flight.repository'

function BookingRoute() {
  const { flight, fare } = Route.useSearch()
  const selectedFlight = flight ? getFlightByNumber(flight) : undefined
  const selectedFare = selectedFlight?.brandedFares
    .flatMap((cabin) => cabin.fares)
    .find((brandedFare) => brandedFare.fareId === fare)

  if (!selectedFlight || !selectedFare) {
    return (
      <Container size="sm" py="xl">
        <Alert title="Choose a flight first" color="blue">
          <Box>
            <Text>A flight and fare are needed before a booking can begin.</Text>
            <Button component={Link} to="/">Search flights</Button>
          </Box>
        </Alert>
      </Container>
    )
  }

  return (
    <Container size="sm" py="xl">
      <Box>
        <Box>
          <Text c="dimmed" size="sm">Booking</Text>
          <Title order={1}>Review your flight</Title>
        </Box>
        <BookingSummary flight={selectedFlight} fare={selectedFare} />
        <PassengerDetailsForm />
        <Button component={Link} to="/">Change flight</Button>
      </Box>
    </Container>
  )
}

export const Route = createFileRoute('/booking')({
  validateSearch: (search: Record<string, unknown>) => ({
    flight: typeof search.flight === 'string' ? search.flight : undefined,
    fare: typeof search.fare === 'number' ? search.fare : undefined,
  }),
  component: BookingRoute,
})