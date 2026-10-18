import { createFileRoute } from '@tanstack/react-router'
import { FlightSearch } from '#/features/flight-search'
import { parseFlightSearchParams } from '#/features/flight-search/model/search-params'
import { DestinationHighlights, HomeHero, ServicePromises } from '#/features/home'

export const Route = createFileRoute('/')({
  validateSearch: parseFlightSearchParams,
  component: HomeRoute,
})

function HomeRoute() {
  const search = Route.useSearch()

  return (
    <>
      <HomeHero>
        <FlightSearch initialSearch={search} />
      </HomeHero>
      <ServicePromises />
      <DestinationHighlights />
    </>
  )
}
