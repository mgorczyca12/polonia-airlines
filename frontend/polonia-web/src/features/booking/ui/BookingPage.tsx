import { Link } from '@tanstack/react-router'
import { Alert, Button, Group, Text } from '@mantine/core'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { BookingLayout } from '#/app/layout/BookingLayout'
import { isConfirmed } from '#/entities/reservation'
import { errorMessage } from '#/shared/api'
import type { OfferResponse } from '#/shared/api'
import { formatUtcDateTime } from '#/shared/lib/formatting'
import { useBookingFlight, useBookingSession } from '../model/useBookingSession'
import { useOfferExpired } from '../model/useOfferExpired'
import { useReservationStatus } from '../model/useReservationStatus'
import { BookingFlow } from './BookingFlow'
import type { BookingFlowStep } from './BookingFlow'
import { BookingProgress } from './BookingProgress'
import { BookingSkeleton } from './BookingSkeleton'
import { JourneySummary } from './JourneySummary'
import { ReservationStatus } from './ReservationStatus'

const RETRY_GUIDANCE =
  'Use "Check submission status" before retrying. Retries reuse this offer\'s submission identity.'

export function BookingPage({ offerId }: { offerId?: string }) {
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

  return <BookingSession offerId={offerId} />
}

function BookingSession({ offerId }: { offerId: string }) {
  const session = useBookingSession(offerId)
  const status = useReservationStatus(session.reservationId, session.recovered)
  const reservation = status.reservation
  const offer = reservation ? undefined : session.offer
  const flight = useBookingFlight(
    (reservation ?? session.offer)?.fares.at(0)?.flightId,
  )
  const flightError = flight.error ? errorMessage(flight.error) : null
  const expired = useOfferExpired(offer?.expiresAt)
  const [step, setStep] = useState<BookingFlowStep>('Passengers')

  if (reservation) {
    return (
      <>
        <BookingProgress
          currentStep={isConfirmed(reservation) ? 'Confirmation' : 'Review'}
        />
        <ReservationStatus
          reservation={reservation}
          flight={flight.data}
          flightError={flightError}
          statusProblem={status.statusProblem}
          isRefreshing={status.isRefreshing}
          onRefreshStatus={status.retryStatus}
        />
      </>
    )
  }

  let problem: string | null = null

  if (session.submit.error) {
    problem = `${errorMessage(session.submit.error)} ${RETRY_GUIDANCE}`
  } else if (session.checkSubmission.error) {
    problem = errorMessage(session.checkSubmission.error)
  } else if (session.loadError) {
    problem = errorMessage(session.loadError)
  }

  let content: ReactNode = null

  if (session.isLoading) {
    content = <BookingSkeleton />
  } else if (offer) {
    content = (
      <BookingFlow
        fares={sortedFares(offer)}
        step={step}
        canReview={flight.isSuccess}
        expired={expired}
        submitting={session.submit.isPending}
        onStepChange={setStep}
        onSubmit={(values) => session.submit.mutate(values)}
      />
    )
  }

  return (
    <>
      <BookingProgress currentStep={step} />
      <BookingLayout
        eyebrow="Polonia Airlines"
        title={
          step === 'Review' ? 'Review your reservation.' : 'Who is travelling?'
        }
        description="Payment is not collected. This reservation is not a paid ticket."
        sidebar={
          offer && (
            <JourneySummary
              routeLabel={
                flight.data
                  ? `${flight.data.departure.city} → ${flight.data.arrival.city}`
                  : 'Your flight'
              }
              details={`One way · ${offer.fares.length} ${offer.fares.length === 1 ? 'traveller' : 'travellers'}`}
              flight={flight.data}
              pendingFlightMessage={
                flightError
                  ? `Itinerary details could not be loaded: ${flightError}`
                  : 'Loading itinerary…'
              }
              lines={sortedFares(offer).map((fare) => ({
                key: String(fare.passengerIndex),
                label: `Traveller ${fare.passengerIndex + 1} · ${fare.cabinClass} · ${fare.brand}`,
                amount: fare.amount,
              }))}
              total={offer.totalAmount}
              currency={offer.currency}
              totalLabel="Offer total"
              totalNote={`Offer expires ${formatUtcDateTime(offer.expiresAt)}`}
              notice="No payment is collected for this reservation."
            />
          )
        }
      >
        {expired && (
          <Alert color="yellow">
            This offer has expired. Check any prior submission before choosing a
            new fare; a replacement offer must be reviewed before booking.
          </Alert>
        )}
        {problem && (
          <Alert color="red" title="Booking request needs attention">
            <Text>{problem}</Text>
            <Group mt="sm">
              <Button
                variant="outline"
                loading={session.checkSubmission.isPending}
                onClick={() => session.checkSubmission.mutate()}
              >
                Check submission status
              </Button>
              <Button variant="outline" onClick={session.reload}>
                Reload booking
              </Button>
            </Group>
          </Alert>
        )}
        {content}
        <Button component={Link} to="/" variant="subtle">
          Back to search
        </Button>
      </BookingLayout>
    </>
  )
}

function sortedFares(offer: OfferResponse) {
  return [...offer.fares].sort(
    (first, second) => first.passengerIndex - second.passengerIndex,
  )
}
