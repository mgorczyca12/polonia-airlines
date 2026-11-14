import { Link } from '@tanstack/react-router'
import { Alert, Button } from '@mantine/core'
import { ArrowRight } from 'lucide-react'
import type { ReactNode } from 'react'
import { BookingLayout } from '#/app/layout/BookingLayout'
import { getCityName } from '#/entities/airport'
import { BookingProgress, JourneySummary } from '#/features/booking'
import {
  formatTravellers,
  getSeatedPassengers,
  resolveTravellers,
  SearchRecap,
} from '#/features/flight-search'
import { errorMessage, FLIGHT_SEARCH_PAGE_SIZE } from '#/shared/api'
import type { FlightSearchCriteria } from '#/shared/api'
import { toDateKey } from '#/shared/lib/formatting'
import { useFareSelection } from '../model/fare-selection'
import type { FlightListSearch } from '../model/flight-list-search'
import { useContinueToBooking } from '../model/useContinueToBooking'
import { useFlightResults } from '../model/useFlightResults'
import { FlightResultsList } from './FlightResultsList'
import { FlightResultsSkeleton } from './FlightResultsSkeleton'

export interface FlightListChanges {
  page?: number
  departure?: string
}

interface FlightResultsPageProps {
  search: FlightListSearch
  onSearchChange: (changes: FlightListChanges) => void
}

export function FlightResultsPage({
  search,
  onSearchChange,
}: FlightResultsPageProps) {
  const { from, to, departure } = search

  if (search.unsupported || !from || !to || !departure) {
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
    <FlightResults
      search={search}
      criteria={{ from, to, departure, page: search.page ?? 1 }}
      onSearchChange={onSearchChange}
    />
  )
}

function FlightResults({
  search,
  criteria,
  onSearchChange,
}: FlightResultsPageProps & { criteria: FlightSearchCriteria }) {
  const travellers = resolveTravellers(search)
  const passengers = getSeatedPassengers(travellers)
  const results = useFlightResults(criteria)
  const fareSelection = useFareSelection(
    JSON.stringify(criteria),
    passengers.length,
  )
  const continueToBooking = useContinueToBooking()
  const { selection, completeProductIds } = fareSelection
  const selectedFlight = results.flights?.find(
    (flight) => flight.id === selection?.flightId,
  )
  const selectedFares = passengers.map((_, index) =>
    results.fares?.find(
      (fare) =>
        fare.flightId === String(selection?.flightId) &&
        fare.productId === selection?.productIds[index],
    ),
  )
  const brands = new Set(selectedFares.map((fare) => fare?.brand))
  const fareLabel =
    brands.size > 1 ? 'Mixed fares' : (selectedFares[0]?.brand ?? 'No fare yet')
  const routeLabel = `${getCityName(criteria.from)} → ${getCityName(criteria.to)}`

  const continueBooking = () => {
    if (selection && completeProductIds) {
      continueToBooking.mutate({
        flightId: selection.flightId,
        productIds: completeProductIds,
      })
    }
  }

  let content: ReactNode

  if (results.error) {
    content = (
      <Alert color="red" title="Unable to load flights">
        {errorMessage(results.error)}
        <Button variant="outline" ml="md" onClick={results.retry}>
          Retry search
        </Button>
      </Alert>
    )
  } else if (results.isFirstLoad) {
    content = <FlightResultsSkeleton />
  } else {
    content = (
      <FlightResultsList
        routeLabel={routeLabel}
        departureDateKey={criteria.departure}
        minDateKey={toDateKey(new Date())}
        flights={results.flights ?? []}
        fares={results.fares ?? []}
        passengers={passengers}
        preferredCabin={search.cabin}
        selection={selection}
        page={criteria.page}
        hasNextPage={results.flights?.length === FLIGHT_SEARCH_PAGE_SIZE}
        isUpdating={results.isUpdating}
        disabled={continueToBooking.isPending}
        onSelectFare={fareSelection.select}
        onPageChange={(page) =>
          onSearchChange({ page: page > 1 ? page : undefined })
        }
        onDateChange={(dateKey) =>
          onSearchChange({ departure: dateKey, page: undefined })
        }
      />
    )
  }

  return (
    <>
      <BookingProgress currentStep="Flights" />
      <SearchRecap search={search} />
      <BookingLayout
        eyebrow="Your next journey"
        title="Choose your flight and fares."
        description="Current prices in USD. Seats are confirmed only after you submit the reservation."
        sidebar={
          <JourneySummary
            routeLabel={routeLabel}
            details={`One way · ${formatTravellers(travellers)} · ${fareLabel}`}
            flight={selectedFlight}
            lines={passengers.map((passenger, index) => ({
              key: passenger.id,
              label: `${passenger.label} · ${selectedFares[index]?.brand ?? 'Fare not chosen'}`,
              amount: selectedFares[index]?.amount ?? null,
            }))}
            currency={selectedFares[0]?.currency ?? 'USD'}
            totalLabel="Current total"
            totalNote="Confirmed when your offer is priced"
            notice="No payment is collected for this reservation."
          >
            {continueToBooking.isError && (
              <Alert color="red" title="Unable to price this selection">
                {errorMessage(continueToBooking.error)}
              </Alert>
            )}
            <Button
              className="polonia-action-button"
              rightSection={<ArrowRight size={17} aria-hidden="true" />}
              loading={continueToBooking.isPending}
              disabled={!completeProductIds}
              onClick={continueBooking}
            >
              Review price and add passengers
            </Button>
          </JourneySummary>
        }
      >
        {content}
      </BookingLayout>
    </>
  )
}
