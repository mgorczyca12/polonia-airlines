import { createFileRoute, Link } from '@tanstack/react-router'
import { Alert, Box, Button, Container, Text } from '@mantine/core'
import { Armchair, Headset, ShieldCheck } from 'lucide-react'
import { useState } from 'react'
import { BookingLayout } from '#/app/layout/BookingLayout'
import {
  BookingConfirmation,
  BookingProgress,
  JourneySummary,
  PassengerDetailsForm,
  PaymentDetailsForm,
} from '#/features/booking'
import type {
  BookingContactDetails,
  BookingStep,
  PassengerDetails,
  PaymentMethod,
} from '#/features/booking'
import { getFlightByNumber } from '#/features/flight-results/data/flight.repository'
import { findFare } from '#/features/flight-results/model/flight.helpers'
import {
  getSeatedPassengers,
  resolveTravellers,
} from '#/features/flight-search'
import { parseFlightSearchParams } from '#/features/flight-search/model/search-params'
import styles from '#/features/booking/styles/BookingRoute.module.scss'

type SeatedPassenger = ReturnType<typeof getSeatedPassengers>[number]
type TravellerCounts = ReturnType<typeof resolveTravellers>

function createPassengerDetails(
  seatedPassengers: SeatedPassenger[],
  infantCount: number,
): PassengerDetails[] {
  const seated = seatedPassengers.map((passenger) => ({
    id: passenger.id,
    label: passenger.label,
    type: passenger.id.startsWith('child-')
      ? ('Child' as const)
      : ('Adult' as const),
    title: '',
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    nationality: '',
    membershipNumber: '',
  }))
  const infants = Array.from({ length: infantCount }, (_, index) => ({
    id: `infant-${index + 1}`,
    label: `Infant ${index + 1}`,
    type: 'Infant' as const,
    title: '',
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    nationality: '',
    membershipNumber: '',
  }))

  return [...seated, ...infants]
}

function BookingAsideNotes({ step }: { step: 'Passengers' | 'Payment' }) {
  return (
    <>
      <section className={styles.asideNote}>
        {step === 'Passengers' ? (
          <Armchair size={18} aria-hidden="true" />
        ) : (
          <ShieldCheck size={18} aria-hidden="true" />
        )}
        <div>
          <h2>
            {step === 'Passengers'
              ? 'A little more freedom'
              : 'Book with peace of mind'}
          </h2>
          <p>
            {step === 'Passengers'
              ? 'Review your selected fare benefits before continuing.'
              : 'This is a demo payment. No card details are collected and no charge is made.'}
          </p>
        </div>
      </section>
      <section className={styles.asideNote}>
        <Headset size={18} aria-hidden="true" />
        <div>
          <h2>A little help along the way?</h2>
          <p>
            Booking support will be available when the service is connected.
          </p>
        </div>
      </section>
    </>
  )
}

function BookingRoute() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  const selectedFlight = search.flight
    ? getFlightByNumber(search.flight)
    : undefined
  const selectedFares = selectedFlight
    ? (search.fares ?? []).flatMap((fareId) => {
        const match = findFare(selectedFlight, fareId)
        return match ? [match.fare] : []
      })
    : []
  const travellers = resolveBookingTravellers(search, selectedFares.length)
  const seatedPassengers = getSeatedPassengers(travellers)
  const [step, setStep] = useState<BookingStep>('Passengers')
  const [passengerDetails, setPassengerDetails] = useState(() =>
    createPassengerDetails(seatedPassengers, travellers.infant),
  )
  const [contact, setContact] = useState<BookingContactDetails>({
    email: '',
    mobile: '',
    smsUpdates: false,
  })
  const [specialRequests, setSpecialRequests] = useState('')
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('card')
  const [termsAccepted, setTermsAccepted] = useState(false)

  if (
    !selectedFlight ||
    selectedFares.length === 0 ||
    selectedFares.length !== search.fares?.length ||
    selectedFares.length !== seatedPassengers.length
  ) {
    return (
      <Container size="sm" py="xl">
        <Alert title="Choose a flight first" color="blue">
          <Box>
            <Text>
              A flight and matching fare selection are needed before a booking
              can begin.
            </Text>
            <Button component={Link} to="/">
              Search flights
            </Button>
          </Box>
        </Alert>
      </Container>
    )
  }

  const passengerFares = seatedPassengers.map((passenger, index) => ({
    passenger,
    fare: selectedFares[index],
  }))
  const total = selectedFares.reduce((sum, fare) => sum + fare.farePriceUsd, 0)
  const tripType = search.tripType ?? 'oneWay'

  const updatePassenger = <TField extends keyof PassengerDetails>(
    index: number,
    key: TField,
    value: PassengerDetails[TField],
  ) => {
    setPassengerDetails((current) =>
      current.map((passenger, currentIndex) =>
        currentIndex === index ? { ...passenger, [key]: value } : passenger,
      ),
    )
  }

  const updateContact = <TField extends keyof BookingContactDetails>(
    key: TField,
    value: BookingContactDetails[TField],
  ) => {
    setContact((current) => ({ ...current, [key]: value }))
  }

  const returnToFlights = () => {
    void navigate({
      to: '/flightlist',
      search: {
        from: search.from,
        to: search.to,
        departure: search.departure,
        return: search.return,
        tripType: search.tripType,
        adults: search.adults,
        children: search.children,
        infants: search.infants,
      },
    })
  }

  if (step === 'Confirmation') {
    return (
      <>
        <BookingProgress currentStep="Confirmation" />
        <BookingConfirmation
          flight={selectedFlight}
          fares={selectedFares}
          passengers={passengerDetails}
          contact={contact}
          specialRequests={specialRequests}
          onSearchAgain={() => void navigate({ to: '/' })}
        />
      </>
    )
  }

  const isPayment = step === 'Payment'
  const activeStep = isPayment ? 'Payment' : 'Passengers'

  return (
    <>
      <BookingProgress currentStep={activeStep} />
      <BookingLayout
        eyebrow={
          isPayment
            ? 'One last step in your journey'
            : 'The people make the journey'
        }
        title={
          isPayment
            ? 'Your next chapter is almost here.'
            : 'Who’s coming along?'
        }
        description={
          isPayment
            ? 'Review your details. This demo will not process a payment.'
            : 'Enter each traveller’s details as they appear on their travel documents. Required fields are marked.'
        }
        sidebar={
          <>
            <JourneySummary
              from={search.from ?? selectedFlight.departureAirport.airportCode}
              to={search.to ?? selectedFlight.arrivalAirport.airportCode}
              tripType={tripType}
              travellers={travellers}
              cabinLabel={
                selectedFlight.brandedFares[0]?.className ?? 'Economy'
              }
              returnDate={search.return}
              flight={selectedFlight}
              passengerFares={passengerFares}
            />
            <BookingAsideNotes step={activeStep} />
          </>
        }
      >
        {isPayment ? (
          <PaymentDetailsForm
            total={total}
            method={paymentMethod}
            termsAccepted={termsAccepted}
            onMethodChange={setPaymentMethod}
            onTermsChange={setTermsAccepted}
            onContinue={() => setStep('Confirmation')}
            onBack={() => setStep('Passengers')}
          />
        ) : (
          <PassengerDetailsForm
            passengers={passengerDetails}
            fares={selectedFares}
            contact={contact}
            onPassengerChange={updatePassenger}
            onContactChange={updateContact}
            specialRequests={specialRequests}
            onSpecialRequestsChange={setSpecialRequests}
            onContinue={() => setStep('Payment')}
            onBack={returnToFlights}
          />
        )}
      </BookingLayout>
    </>
  )
}

function resolveBookingTravellers(
  search: { adults?: number; children?: number; infants?: number },
  fareCount: number,
): TravellerCounts {
  if (search.adults === undefined && search.children === undefined) {
    return { adult: fareCount || 1, child: 0, infant: search.infants ?? 0 }
  }

  return resolveTravellers(search)
}

export const Route = createFileRoute('/booking')({
  validateSearch: (search: Record<string, unknown>) => {
    const flightSearch = parseFlightSearchParams(search)

    return {
      flight: typeof search.flight === 'string' ? search.flight : undefined,
      fares:
        Array.isArray(search.fares) &&
        search.fares.every((fareId) => typeof fareId === 'number')
          ? search.fares
          : undefined,
      from: flightSearch.from,
      to: flightSearch.to,
      departure: flightSearch.departure,
      return: flightSearch.return,
      tripType: flightSearch.tripType,
      adults: flightSearch.adults,
      children: flightSearch.children,
      infants: flightSearch.infants,
    }
  },
  component: BookingRoute,
})
