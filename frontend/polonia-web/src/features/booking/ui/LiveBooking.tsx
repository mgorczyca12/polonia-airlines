import { Link } from '@tanstack/react-router'
import {
  Alert,
  Button,
  Group,
  Loader,
  Paper,
  Stack,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useEffect, useState } from 'react'
import { BookingLayout } from '#/app/layout/BookingLayout'
import { airportLabel } from '#/entities/airport'
import { isConfirmed, isTerminal } from '#/entities/reservation'
import {
  createReservation,
  errorMessage,
  getFlight,
  getOffer,
  getReservation,
  recoverReservation,
} from '#/shared/api'
import type {
  FlightResponse,
  OfferResponse,
  PassengerRequest,
  ReservationResponse,
} from '#/shared/api'
import { formatMoney, formatUtcDateTime as formatUtc } from '#/shared/lib/formatting'
import { BookingProgress } from './BookingProgress'

export function LiveBooking({ offerId }: { offerId?: string }) {
  const [offer, setOffer] = useState<OfferResponse | null>(null)
  const [reservation, setReservation] = useState<ReservationResponse | null>(
    null,
  )
  const [flight, setFlight] = useState<FlightResponse | null>(null)
  const [passengers, setPassengers] = useState<PassengerRequest[]>([])
  const [contactName, setContactName] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [step, setStep] = useState<'Passengers' | 'Review'>('Passengers')
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [pollError, setPollError] = useState<string | null>(null)
  const [flightError, setFlightError] = useState<string | null>(null)
  const [retry, setRetry] = useState(0)
  const [pollRetry, setPollRetry] = useState(0)
  const [copied, setCopied] = useState(false)
  const [expired, setExpired] = useState(false)

  useEffect(() => {
    if (!offer || reservation) return
    const remaining = Date.parse(offer.expiresAt) - Date.now()
    setExpired(remaining <= 0)
    const timer = setTimeout(() => setExpired(true), Math.max(0, remaining))
    return () => clearTimeout(timer)
  }, [offer, reservation])

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)
    if (!offerId) {
      setLoading(false)
      return () => controller.abort()
    }
    void (async () => {
      try {
        const recovered = await recoverReservation(offerId, controller.signal)
        let flightId: string
        if (recovered) {
          setReservation(recovered)
          setPollError(null)
          if (!isTerminal(recovered)) setPollRetry((value) => value + 1)
          flightId = recovered.fares[0].flightId
        } else {
          const priced = await getOffer(offerId, controller.signal)
          setOffer(priced)
          setPassengers((previous) =>
            previous.length
              ? previous
              : Array.from(
                  {
                    length: new Set(
                      priced.fares.map((fare) => fare.passengerIndex),
                    ).size,
                  },
                  () => ({
                    firstName: '',
                    lastName: '',
                    dateOfBirth: '',
                  }),
                ),
          )
          flightId = priced.fares[0].flightId
        }
        try {
          setFlight(await getFlight(flightId, controller.signal))
          setFlightError(null)
        } catch (failure) {
          if (!controller.signal.aborted) setFlightError(errorMessage(failure))
        }
      } catch (failure) {
        if (!controller.signal.aborted) setError(errorMessage(failure))
      } finally {
        if (!controller.signal.aborted) setLoading(false)
      }
    })()
    return () => controller.abort()
  }, [offerId, retry])

  useEffect(() => {
    if (!reservation || isTerminal(reservation)) return
    const controller = new AbortController()
    let timer: ReturnType<typeof setTimeout> | undefined
    let attempts = 0
    setPollError(null)
    const poll = async () => {
      try {
        const current = await getReservation(reservation.id, controller.signal)
        if (controller.signal.aborted) return
        setReservation(current)
        if (isTerminal(current)) return
        attempts++
        if (attempts >= 40) {
          setPollError(
            'Confirmation is taking longer than expected. Your reservation is still pending; retry status, not a new booking.',
          )
          return
        }
        timer = setTimeout(
          () => void poll(),
          Math.min(1500 + attempts * 500, 5000),
        )
      } catch (failure) {
        if (!controller.signal.aborted) setPollError(errorMessage(failure))
      }
    }
    timer = setTimeout(() => void poll(), 1500)
    return () => {
      controller.abort()
      clearTimeout(timer)
    }
  }, [reservation?.id, pollRetry])

  const submit = async () => {
    if (!offerId || !offer || submitting) return
    setSubmitting(true)
    setError(null)
    try {
      const existing = await recoverReservation(
        offerId,
        new AbortController().signal,
      )
      const created =
        existing ??
        (await createReservation({
          offerId,
          contactName,
          contactEmail,
          passengers,
        }))
      setReservation(created)
    } catch (failure) {
      setError(
        `${errorMessage(failure)} Use "Check submission status" before retrying. Retries reuse this offer's submission identity.`,
      )
    } finally {
      setSubmitting(false)
    }
  }

  const checkSubmission = async () => {
    if (!offerId) return
    setSubmitting(true)
    try {
      const existing = await recoverReservation(
        offerId,
        new AbortController().signal,
      )
      if (existing) {
        setReservation(existing)
        setError(null)
        setPollError(null)
        if (!isTerminal(existing)) setPollRetry((value) => value + 1)
      } else {
        setError(
          'No committed reservation was found yet. You may retry with the original passenger and contact details; do not start another booking if a submission is still in flight.',
        )
      }
    } catch (failure) {
      setError(errorMessage(failure))
    } finally {
      setSubmitting(false)
    }
  }

  if (!offerId) {
    return (
      <BookingLayout eyebrow="Booking" title="Choose a flight first">
        <Alert>A server-priced offer is required to book.</Alert>
        <Button component={Link} to="/" mt="md">
          Search flights
        </Button>
      </BookingLayout>
    )
  }

  const confirmed = reservation ? isConfirmed(reservation) : false
  const title = reservation
    ? confirmed
      ? 'Your reservation is confirmed.'
      : 'Reservation status'
    : step === 'Review'
      ? 'Review your reservation.'
      : 'Who is travelling?'
  const values = reservation ?? offer
  const maxBirthDate = new Date()
  maxBirthDate.setUTCFullYear(maxBirthDate.getUTCFullYear() - 2)

  return (
    <>
      <BookingProgress currentStep={confirmed ? 'Confirmation' : step} />
      <BookingLayout
        eyebrow="Polonia Airlines"
        title={title}
        description="Payment is not collected. This reservation is not a paid ticket."
      >
        <Stack>
          {loading && <Loader aria-label="Loading booking" />}
          {expired && !reservation && (
            <Alert color="yellow">
              This offer has expired. Check any prior submission before choosing
              a new fare; a replacement offer must be reviewed before booking.
            </Alert>
          )}
          {error && (
            <Alert color="red" title="Booking request needs attention">
              <Text>{error}</Text>
              <Group mt="sm">
                <Button
                  variant="outline"
                  loading={submitting}
                  onClick={() => void checkSubmission()}
                >
                  Check submission status
                </Button>
                <Button
                  variant="outline"
                  onClick={() => setRetry((value) => value + 1)}
                >
                  Reload booking
                </Button>
              </Group>
            </Alert>
          )}
          {flightError && (
            <Alert color="yellow">
              Itinerary details could not be loaded: {flightError}
            </Alert>
          )}
          {flight && (
            <Paper withBorder p="lg">
              <Title order={2}>{flight.flightNumber}</Title>
              <Text>
                {airportLabel(flight.departureAirport)} →{' '}
                {airportLabel(flight.arrivalAirport)}
              </Text>
              <Text>Departure: {formatUtc(flight.departureTime)}</Text>
              <Text>Arrival: {formatUtc(flight.arrivalTime)}</Text>
            </Paper>
          )}
          {values && (
            <Paper withBorder p="lg">
              <Title order={2}>Fare summary</Title>
              {values.fares.map((fare) => (
                <Text key={`${fare.flightId}-${fare.passengerIndex}`}>
                  Traveller {fare.passengerIndex + 1}: {fare.cabinClass} ·{' '}
                  {fare.brand} · {formatMoney(fare.amount, fare.currency)}.{' '}
                  {fare.baggage.includedPieces} checked pieces, up to{' '}
                  {fare.baggage.maxWeightPerPieceKg} kg each.
                </Text>
              ))}
              <Text fw={700} mt="sm">
                Total: {formatMoney(values.totalAmount, values.currency)}
              </Text>
              {offer && !reservation && (
                <Text size="sm">
                  Offer expires: {formatUtc(offer.expiresAt)}.
                </Text>
              )}
            </Paper>
          )}
          {reservation ? (
            <Paper withBorder p="lg">
              <Stack>
                <Title order={2}>
                  Reservation reference: {reservation.code}
                </Title>
                <Text>
                  Status: {reservation.status} · Workflow:{' '}
                  {reservation.workflowStatus}
                </Text>
                {!confirmed && (
                  <Alert
                    color={
                      reservation.workflowStatus === 'Cancelled'
                        ? 'red'
                        : 'yellow'
                    }
                  >
                    {reservation.workflowStatus === 'Cancelled'
                      ? `The reservation was cancelled (${reservation.failureCode ?? 'inventory could not be confirmed'}).`
                      : reservation.workflowStatus === 'RequiresIntervention'
                        ? 'The workflow requires operator intervention. Do not create a replacement booking while capacity release is unresolved.'
                        : reservation.workflowStatus === 'Compensating'
                          ? 'Inventory acquisition failed. Release is in progress; cancellation is not complete yet.'
                          : reservation.workflowStatus === 'Completed'
                            ? 'The reservation and workflow states disagree. Contact support with this reference.'
                            : 'Inventory confirmation is pending. Submission alone does not confirm your reservation.'}
                  </Alert>
                )}
                {pollError && (
                  <Alert color="yellow">
                    {pollError}
                    <Button
                      ml="md"
                      variant="outline"
                      onClick={() => setPollRetry((value) => value + 1)}
                    >
                      Retry status
                    </Button>
                  </Alert>
                )}
                <Button
                  variant="outline"
                  onClick={() => setRetry((value) => value + 1)}
                >
                  Refresh reservation status
                </Button>
                <Text>
                  Contact: {reservation.contactName} ·{' '}
                  {reservation.contactEmail}
                </Text>
                {reservation.passengers.map((passenger) => (
                  <Text key={passenger.id}>
                    {passenger.firstName} {passenger.lastName}
                  </Text>
                ))}
                <Button
                  variant="outline"
                  onClick={() => {
                    void navigator.clipboard
                      .writeText(reservation.code)
                      .then(() => setCopied(true))
                      .catch(() =>
                        setError('Could not copy the reservation reference.'),
                      )
                  }}
                >
                  {copied ? 'Reference copied' : 'Copy reference'}
                </Button>
              </Stack>
            </Paper>
          ) : (
            !loading &&
            offer &&
            (step === 'Passengers' ? (
              <form
                onSubmit={(event) => {
                  event.preventDefault()
                  setStep('Review')
                }}
              >
                <Stack>
                  {passengers.map((passenger, index) => (
                    <Paper withBorder p="lg" key={index}>
                      <Stack>
                        <Title order={2}>Traveller {index + 1}</Title>
                        {(
                          ['firstName', 'lastName', 'dateOfBirth'] as const
                        ).map((field) => (
                          <TextInput
                            key={field}
                            label={
                              field === 'firstName'
                                ? 'First name'
                                : field === 'lastName'
                                  ? 'Last name'
                                  : 'Date of birth'
                            }
                            type={field === 'dateOfBirth' ? 'date' : 'text'}
                            required
                            maxLength={100}
                            max={
                              field === 'dateOfBirth'
                                ? maxBirthDate.toISOString().slice(0, 10)
                                : undefined
                            }
                            value={passenger[field]}
                            onChange={(event) => {
                              const value = event.currentTarget.value
                              setPassengers((current) =>
                                current.map((item, position) =>
                                  position === index
                                    ? { ...item, [field]: value }
                                    : item,
                                ),
                              )
                            }}
                          />
                        ))}
                      </Stack>
                    </Paper>
                  ))}
                  <TextInput
                    label="Contact name"
                    required
                    maxLength={200}
                    value={contactName}
                    onChange={(event) =>
                      setContactName(event.currentTarget.value)
                    }
                  />
                  <TextInput
                    label="Contact email"
                    required
                    type="email"
                    maxLength={254}
                    value={contactEmail}
                    onChange={(event) =>
                      setContactEmail(event.currentTarget.value)
                    }
                  />
                  <Text size="sm">
                    All travellers must be aged two or older. Names and dates
                    must match travel documents.
                  </Text>
                  <Button
                    type="submit"
                    disabled={!flight || passengers.length === 0}
                  >
                    Review reservation
                  </Button>
                </Stack>
              </form>
            ) : (
              <Stack>
                <Title order={2}>Review passengers and contact</Title>
                {passengers.map((passenger, index) => (
                  <Text key={index}>
                    {passenger.firstName} {passenger.lastName} ·{' '}
                    {passenger.dateOfBirth}
                  </Text>
                ))}
                <Text>
                  {contactName} · {contactEmail}
                </Text>
                <Text>
                  No charge will be made. Inventory may be unavailable even when
                  a fare is priced.
                </Text>
                <Group>
                  <Button
                    variant="outline"
                    disabled={submitting}
                    onClick={() => setStep('Passengers')}
                  >
                    Edit details
                  </Button>
                  <Button
                    loading={submitting}
                    disabled={expired}
                    onClick={() => void submit()}
                  >
                    Reserve without payment
                  </Button>
                </Group>
              </Stack>
            ))
          )}
          <Button component={Link} to="/" variant="subtle">
            Back to search
          </Button>
        </Stack>
      </BookingLayout>
    </>
  )
}
