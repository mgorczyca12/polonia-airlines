import { createFileRoute } from '@tanstack/react-router'
import { Button } from '@mantine/core'
import { ArrowRight } from 'lucide-react'
import { useState } from 'react'
import { BookingLayout, BookingNextStep } from '#/app/layout/BookingLayout'
import { BookingProgress, JourneySummary } from '#/features/booking'
import {
  FlightResults,
  ReturnFlightNotice,
  findFare,
  getCityName,
  getFlightByNumber,
} from '#/features/flight-results'
import type { FareSelection } from '#/features/flight-results'
import {
  DEFAULT_CABIN,
  SearchRecap,
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
  const tripType = resolveTripType(search)
  const cabin = search.cabin ?? DEFAULT_CABIN
  const selectedFlight = selection ? getFlightByNumber(selection.flightNumber) : undefined
  const selectedFare =
    selectedFlight && selection ? findFare(selectedFlight, selection.fareId)?.fare : undefined

  const changeDepartureDate = (departure: string) => {
    setSelection(null)
    void navigate({
      search: (previous) => ({
        ...previous,
        departure,
        return: previous.return && previous.return >= departure ? previous.return : undefined,
      }),
      replace: true,
    })
  }

  const continueToPassengers = () => {
    if (selection) {
      void navigate({
        to: '/booking',
        search: { flight: selection.flightNumber, fare: selection.fareId },
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
          <JourneySummary
            from={search.from}
            to={search.to}
            tripType={tripType}
            travellers={travellers}
            cabinLabel={cabin}
            returnDate={search.return}
            flight={selectedFlight}
            fare={selectedFare}
          />
        }
      >
        <FlightResults
          from={search.from}
          to={search.to}
          departureDate={search.departure}
          preferredCabin={cabin}
          selection={selection}
          onSelectFare={setSelection}
          onDepartureDateChange={changeDepartureDate}
        />

        {tripType === 'roundTrip' && search.from && search.to && (
          <ReturnFlightNotice
            routeLabel={`${getCityName(search.to)} to ${getCityName(search.from)}`}
            returnDateLabel={search.return ? formatShortDate(search.return) : undefined}
          />
        )}

        <BookingNextStep
          note={
            selection
              ? 'Review your selection, then add passenger details.'
              : 'Select a fare to continue.'
          }
        >
          <Button
            className="polonia-action-button"
            size="lg"
            disabled={!selection}
            rightSection={<ArrowRight size={17} />}
            onClick={continueToPassengers}
          >
            Continue to passengers
          </Button>
        </BookingNextStep>
      </BookingLayout>
    </>
  )
}

export const Route = createFileRoute('/flightlist')({
  validateSearch: parseFlightSearchParams,
  component: FlightListingRoute,
})
