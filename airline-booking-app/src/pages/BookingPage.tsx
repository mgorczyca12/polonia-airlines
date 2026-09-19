import { Alert, Box, Button, Container, Text, Title } from '@mantine/core'
import { Link } from '@tanstack/react-router'
import { BookingSummary, PassengerDetailsForm } from '#/features/booking'
import { getFlightByNumber } from '#/features/flight-results/data/flight.repository'
import styles from './styles/BookingPage.module.scss'

interface BookingPageProps {
  flightNumber?: string
  fareId?: number
}

export function BookingPage({ flightNumber, fareId }: BookingPageProps) {
  const flight = flightNumber ? getFlightByNumber(flightNumber) : undefined
  const selectedFare = flight?.brandedFares
    .flatMap((cabin) => cabin.fares)
    .find((fare) => fare.fareId === fareId)

  if (!flight || !selectedFare) {
    return (
      <Container size="sm" py="xl">
        <Alert title="Choose a flight first" color="blue">
          <Box className={styles.invalidSelection}>
            <Text>A flight and fare are needed before a booking can begin.</Text>
            <Button component={Link} to="/">Search flights</Button>
          </Box>
        </Alert>
      </Container>
    )
  }

  return (
    <Container size="sm" py="xl">
      <Box className={styles.page}>
        <Box>
          <Text c="dimmed" size="sm">Booking</Text>
          <Title order={1}>Review your flight</Title>
        </Box>
        <BookingSummary flight={flight} fare={selectedFare} />
        <PassengerDetailsForm />
        <Button component={Link} to="/">Change flight</Button>
      </Box>
    </Container>
  )
}