import { createFileRoute, Link } from '@tanstack/react-router'
import {
  Alert,
  Button,
  Group,
  Loader,
  Paper,
  Select,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { useEffect, useState } from 'react'
import { BookingLayout } from '#/app/layout/BookingLayout'
import { BookingProgress } from '#/features/booking'
import {
  getSeatedPassengers,
  parseLiveSearchParams,
  resolveTravellers,
  SearchRecap,
} from '#/features/flight-search'
import { airportLabel } from '#/entities/airport'
import { createOffer, errorMessage, getFares, searchFlights } from '#/shared/api'
import type { FlightFareResponse, FlightResponse } from '#/shared/api'
import { formatMoney, formatUtcDateTime as formatUtc } from '#/shared/lib/formatting'

function FlightListingRoute() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  const [flights, setFlights] = useState<FlightResponse[]>([])
  const [fares, setFares] = useState<FlightFareResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [retry, setRetry] = useState(0)
  const [page, setPage] = useState(1)
  const [selection, setSelection] = useState<{
    flightId: number
    products: string[]
  } | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const passengers = getSeatedPassengers(resolveTravellers(search))
  const valid =
    !search.unsupported && Boolean(search.from && search.to && search.departure)

  useEffect(() => {
    setPage(1)
    setSelection(null)
  }, [search.from, search.to, search.departure, search.adults, search.children])

  useEffect(() => {
    const controller = new AbortController()
    if (!valid) {
      setLoading(false)
      return () => controller.abort()
    }
    setLoading(true)
    setError(null)
    setSelection(null)
    void (async () => {
      try {
        const result = await searchFlights(
          {
            from: search.from!,
            to: search.to!,
            departure: search.departure!,
            page,
          },
          controller.signal,
        )
        const prices = result.length
          ? await getFares(
              result.map((flight) => flight.id),
              controller.signal,
            )
          : []
        if (!controller.signal.aborted) {
          setFlights(result)
          setFares(prices)
        }
      } catch (failure) {
        if (!controller.signal.aborted) setError(errorMessage(failure))
      } finally {
        if (!controller.signal.aborted) setLoading(false)
      }
    })()
    return () => controller.abort()
  }, [valid, search.from, search.to, search.departure, page, retry])

  const continueBooking = async () => {
    if (!selection || selection.products.some((product) => !product)) return
    setSubmitting(true)
    setError(null)
    try {
      const offer = await createOffer(selection.flightId, selection.products)
      await navigate({ to: '/booking', search: { offer: offer.id } })
    } catch (failure) {
      setError(errorMessage(failure))
    } finally {
      setSubmitting(false)
    }
  }

  if (!valid) {
    return (
      <BookingLayout eyebrow="Flight search" title="Choose a supported journey">
        <Alert color="yellow">
          Select departure and arrival airports and a departure date. This
          release supports one-way bookings for up to eight travellers aged two
          or older.
        </Alert>
        <Button component={Link} to="/" mt="md">
          Modify search
        </Button>
      </BookingLayout>
    )
  }

  return (
    <>
      <BookingProgress currentStep="Flights" />
      <SearchRecap search={search} />
      <BookingLayout
        eyebrow="Your next journey"
        title="Choose your flight and fares."
        description="Current prices in USD. Capacity is confirmed only after reservation submission; children use the selected product price."
      >
        <Stack>
          {error && (
            <Alert color="red" title="Unable to complete the request">
              {error}
              <Button
                onClick={() => setRetry((value) => value + 1)}
                variant="outline"
                ml="md"
              >
                Retry search
              </Button>
            </Alert>
          )}
          {loading ? (
            <Loader aria-label="Loading flights and fares" />
          ) : (
            !error && (
              <>
                {flights.length === 0 && (
                  <Alert>
                    No scheduled flights match this search. Choose another date
                    or route.
                  </Alert>
                )}
                {flights.map((flight) => {
                  const prices = fares.filter(
                    (fare) => fare.flightId === String(flight.id),
                  )
                  const options = prices.map((fare) => ({
                    value: fare.productId,
                    label: `${fare.cabinClass} · ${fare.brand} · ${formatMoney(fare.amount, fare.currency)}`,
                  }))
                  const chosen =
                    selection?.flightId === flight.id ? selection.products : []
                  return (
                    <Paper key={flight.id} withBorder p="lg" radius="md">
                      <Stack>
                        <Title order={2}>{flight.flightNumber}</Title>
                        <Text>
                          {airportLabel(flight.departureAirport)} →{' '}
                          {airportLabel(flight.arrivalAirport)}
                        </Text>
                        <Text>
                          Departure: {formatUtc(flight.departureTime)}
                        </Text>
                        <Text>Arrival: {formatUtc(flight.arrivalTime)}</Text>
                        {!prices.length ? (
                          <Text>
                            No fares are currently priced for this flight.
                          </Text>
                        ) : (
                          <>
                            {passengers.map((passenger, index) => (
                              <Select
                                key={passenger.id}
                                label={`${passenger.label} fare`}
                                data={options}
                                value={chosen[index] || null}
                                disabled={submitting}
                                allowDeselect={false}
                                onChange={(product) => {
                                  if (!product) return
                                  const products =
                                    selection?.flightId === flight.id
                                      ? [...selection.products]
                                      : passengers.map(() => '')
                                  products[index] = product
                                  setSelection({
                                    flightId: flight.id,
                                    products,
                                  })
                                }}
                              />
                            ))}
                            {chosen.map((product, index) => {
                              const fare = prices.find(
                                (price) => price.productId === product,
                              )
                              return fare ? (
                                <Text key={index} size="sm">
                                  {passengers[index].label}:{' '}
                                  {fare.baggage.includedPieces} checked pieces,
                                  up to {fare.baggage.maxWeightPerPieceKg} kg
                                  per piece.
                                </Text>
                              ) : null
                            })}
                          </>
                        )}
                      </Stack>
                    </Paper>
                  )
                })}
                <Group justify="space-between">
                  <Button
                    disabled={page === 1 || submitting}
                    variant="outline"
                    onClick={() => setPage((value) => value - 1)}
                  >
                    Previous page
                  </Button>
                  <Text>Page {page}</Text>
                  <Button
                    disabled={flights.length < 20 || submitting}
                    variant="outline"
                    onClick={() => setPage((value) => value + 1)}
                  >
                    Next page
                  </Button>
                </Group>
                <Button
                  loading={submitting}
                  disabled={
                    !selection ||
                    selection.products.length !== passengers.length ||
                    selection.products.some((product) => !product)
                  }
                  onClick={() => void continueBooking()}
                >
                  Review priced offer and add passengers
                </Button>
              </>
            )
          )}
        </Stack>
      </BookingLayout>
    </>
  )
}

export const Route = createFileRoute('/flightlist')({
  validateSearch: parseLiveSearchParams,
  component: FlightListingRoute,
})
