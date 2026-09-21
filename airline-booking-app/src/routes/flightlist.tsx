import { createFileRoute } from '@tanstack/react-router'
import { FlightListingPage } from '#/pages/FlightListingPage'

interface FlightListSearch {
  from?: string
  to?: string
  departure?: string
  return?: string
  tripType?: 'roundTrip' | 'oneWay'
}

function FlightListingRoute() {
  const { from, to } = Route.useSearch()

  return <FlightListingPage from={from} to={to} />
}

export const Route = createFileRoute('/flightlist')({
  validateSearch: (search: Record<string, unknown>): FlightListSearch => ({
    from: typeof search.from === 'string' ? search.from : undefined,
    to: typeof search.to === 'string' ? search.to : undefined,
    departure: typeof search.departure === 'string' ? search.departure : undefined,
    return: typeof search.return === 'string' ? search.return : undefined,
    tripType: search.tripType === 'oneWay' ? 'oneWay' : 'roundTrip',
  }),
  component: FlightListingRoute,
})
