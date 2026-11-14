import { createFileRoute } from '@tanstack/react-router'
import {
  FlightResultsPage,
  parseFlightListSearch,
} from '#/features/flight-results'
import type { FlightListChanges } from '#/features/flight-results'

export const Route = createFileRoute('/flightlist')({
  validateSearch: parseFlightListSearch,
  component: FlightListRoute,
})

function FlightListRoute() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  // A new journey starts a fresh results page; paging keeps results mounted.
  const journeyKey = [
    search.from,
    search.to,
    search.departure,
    search.adults,
    search.children,
  ].join('|')

  const changeSearch = (changes: FlightListChanges) => {
    void navigate({ search: (previous) => ({ ...previous, ...changes }) })
  }

  return (
    <FlightResultsPage
      key={journeyKey}
      search={search}
      onSearchChange={changeSearch}
    />
  )
}
