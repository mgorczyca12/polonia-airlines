import { createFileRoute } from '@tanstack/react-router'
import { BookingPage } from '#/features/booking'
import { isUuid } from '#/shared/lib/identifiers'

export const Route = createFileRoute('/booking')({
  validateSearch: (search: Record<string, unknown>) => ({
    offer: isUuid(search.offer) ? search.offer : undefined,
  }),
  remountDeps: ({ search }) => search.offer,
  component: BookingRoute,
})

function BookingRoute() {
  const { offer } = Route.useSearch()
  return <BookingPage offerId={offer} />
}
