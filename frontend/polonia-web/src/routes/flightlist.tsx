import { createFileRoute } from '@tanstack/react-router'
import { Button } from '@mantine/core'
import { ArrowRight } from 'lucide-react'
import { useState } from 'react'
import { BookingLayout, BookingNextStep } from '#/app/layout/BookingLayout'
import { BookingProgress, JourneySummary } from '#/features/booking'
import {
  FlightResults,
  ReturnFlightNotice,
  assignFare,
  findFare,
  getCityName,
  getFlightByNumber,
} from '#/features/flight-results'
import type { FareSelection } from '#/features/flight-results'
import {
  DEFAULT_CABIN,
  SearchRecap,
  getSeatedPassengers,
  resolveTravellers,
  resolveTripType,
} from '#/features/flight-search'
import { parseFlightSearchParams } from '#/features/flight-search/model/search-params'
import { formatShortDate } from '#/shared/lib/formatting'

function FlightListingRoute() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  const [selection, setSelection] = useState<FareSelection | null>(null)
  const travellers = resolveTravellers(search)
  const passengers = getSeatedPassengers(travellers)
  const tripType = resolveTripType(search)
  const cabin = search.cabin ?? DEFAULT_CABIN
  const selectedFlight = selection
    ? getFlightByNumber(selection.flightNumber)
    : undefined
  const passengerFares = passengers.map((passenger) => {
    const fareId = selection?.fareIds[passenger.id]

    return {
      passenger,
      fare:
        selectedFlight && fareId !== undefined
          ? findFare(selectedFlight, fareId)?.fare
          : undefined,
    }
  })
  const pendingPassengers = passengerFares.filter(({ fare }) => !fare)
  const hasAllFares = passengers.length > 0 && pendingPassengers.length === 0
  const nextStepNote = hasAllFares
    ? 'Review your selection, then add passenger details.'
    : pendingPassengers.length === passengers.length
      ? 'Select a fare to continue.'
      : `Choose a fare for ${pendingPassengers.map(({ passenger }) => passenger.label).join(', ')}.`

  const selectFare = (
    flightNumber: string,
    passengerIds: string[],
    fareId: number,
  ) => {
    setSelection((current) =>
      assignFare(current, flightNumber, passengerIds, fareId),
    )
  }

  const changeDepartureDate = (departure: string) => {
    setSelection(null)
    void navigate({
      search: (previous) => ({
        ...previous,
        departure,
        return:
          previous.return && previous.return >= departure
            ? previous.return
            : undefined,
      }),
      replace: true,
    })
  }

  const continueToPassengers = () => {
    if (selection && hasAllFares) {
      void navigate({
        to: '/booking',
        search: {
          flight: selection.flightNumber,
          fares: passengerFares.flatMap(({ fare }) =>
            fare ? [fare.fareId] : [],
          ),
        },
      })
    }
  }

  return (
    <>
      <BookingProgress currentStep="Flights" />
      <SearchRecap search={search} />
      <BookingLayout
        eyebrow="A journey to look forward to"
        title="Find your perfect flight."
        description="Choose a little more comfort, a little more freedom. Every fare includes our signature service."
        sidebar={
          <>
            <JourneySummary
              from={search.from}
              to={search.to}
              tripType={tripType}
              travellers={travellers}
              cabinLabel={cabin}
              returnDate={search.return}
              flight={selectedFlight}
              passengerFares={passengerFares}
            />
            <BookingNextStep note={nextStepNote}>
              <Button
                className="polonia-action-button"
                size="lg"
                fullWidth
                disabled={!hasAllFares}
                rightSection={<ArrowRight size={17} />}
                onClick={continueToPassengers}
              >
                Continue to passengers
              </Button>
            </BookingNextStep>
          </>
        }
      >
        <FlightResults
          from={search.from}
          to={search.to}
          departureDate={search.departure}
          preferredCabin={cabin}
          selection={selection}
          passengers={passengers}
          onSelectFare={selectFare}
          onDepartureDateChange={changeDepartureDate}
        />

        {tripType === 'roundTrip' && search.from && search.to && (
          <ReturnFlightNotice
            routeLabel={`${getCityName(search.to)} to ${getCityName(search.from)}`}
            returnDateLabel={
              search.return ? formatShortDate(search.return) : undefined
            }
          />
        )}
      </BookingLayout>
    </>
  )
}

export const Route = createFileRoute('/flightlist')({
  validateSearch: parseFlightSearchParams,
  component: FlightListingRoute,
})
