import { createFileRoute } from '@tanstack/react-router'
import { LiveBooking } from '#/features/booking'

function BookingRoute() {
  const search = Route.useSearch()
  return <LiveBooking offerId={search.offer} />
}

export const Route = createFileRoute('/booking')({
  validateSearch: (search: Record<string, unknown>) => ({
    offer:
      typeof search.offer === 'string' &&
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(
        search.offer,
      )
        ? search.offer
        : undefined,
  }),
  remountDeps: ({ search }) => search.offer,
  component: BookingRoute,
})
