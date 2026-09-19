import { createFileRoute } from '@tanstack/react-router'
import { HomePage } from '#/pages/HomePage'

interface HomeSearch {
  from?: string
  to?: string
  departure?: string
  return?: string
  tripType?: 'roundTrip' | 'oneWay'
}

function IndexRoute() {
  const search = Route.useSearch()

  return <HomePage from={search.from} to={search.to} />
}

export const Route = createFileRoute('/')({
  validateSearch: (search: Record<string, unknown>): HomeSearch => ({
    from: typeof search.from === 'string' ? search.from : undefined,
    to: typeof search.to === 'string' ? search.to : undefined,
    departure: typeof search.departure === 'string' ? search.departure : undefined,
    return: typeof search.return === 'string' ? search.return : undefined,
    tripType: search.tripType === 'oneWay' ? 'oneWay' : 'roundTrip',
  }),
  component: IndexRoute,
})
