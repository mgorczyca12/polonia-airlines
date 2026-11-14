import { createFileRoute } from '@tanstack/react-router'
import { Alert } from '@mantine/core'
import { FlightSearch, parseLiveSearchParams } from '#/features/flight-search'
import {
  DestinationHighlights,
  HomeHero,
  ServicePromises,
} from '#/features/home'

export const Route = createFileRoute('/')({
  validateSearch: parseLiveSearchParams,
  component: HomeRoute,
})

function HomeRoute() {
  const search = Route.useSearch()

  return (
    <>
      <HomeHero>
        {search.unsupported && (
          <Alert color="yellow">
            The previous journey includes unsupported options. Choose a new
            one-way search without infants; no return or infant booking will be
            submitted.
          </Alert>
        )}
        <FlightSearch initialSearch={search} />
      </HomeHero>
      <ServicePromises />
      <DestinationHighlights />
    </>
  )
}
