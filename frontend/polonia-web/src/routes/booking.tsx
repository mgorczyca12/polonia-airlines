import { createFileRoute, Link } from '@tanstack/react-router'
import { Alert, Box, Button, Container, Text, Title } from '@mantine/core'
import { BookingSummary, PassengerDetailsForm } from '#/features/booking'
import { getFlightByNumber } from '#/features/flight-results/data/flight.repository'
import { findFare } from '#/features/flight-results/model/flight.helpers'

function BookingRoute() {
  const { flight, fares } = Route.useSearch()
  const selectedFlight = flight ? getFlightByNumber(flight) : undefined
  const selectedFares = selectedFlight
    ? (fares ?? []).flatMap((fareId) => {
        const match = findFare(selectedFlight, fareId)
        return match ? [match.fare] : []
      })
    : []

  if (
    !selectedFlight ||
    selectedFares.length === 0 ||
    selectedFares.length !== fares?.length
  ) {
    return (
      <Container size="sm" py="xl">
        <Alert title="Choose a flight first" color="blue">
          <Box>
            <Text>
              A flight and fare are needed before a booking can begin.
            </Text>
            <Button component={Link} to="/">
              Search flights
            </Button>
          </Box>
        </Alert>
      </Container>
    )
  }

  return (
    <Container size="sm" py="xl">
      <Box>
        <Box>
          <Text c="dimmed" size="sm">
            Booking
          </Text>
          <Title order={1}>Review your flight</Title>
        </Box>
        <BookingSummary flight={selectedFlight} fares={selectedFares} />
        <PassengerDetailsForm />
        <Button component={Link} to="/">
          Change flight
        </Button>
      </Box>
    </Container>
  )
}

export const Route = createFileRoute('/booking')({
  validateSearch: (search: Record<string, unknown>) => ({
    flight: typeof search.flight === 'string' ? search.flight : undefined,
    fares:
      Array.isArray(search.fares) &&
      search.fares.every((fareId) => typeof fareId === 'number')
        ? search.fares
        : undefined,
  }),
  component: BookingRoute,
})
