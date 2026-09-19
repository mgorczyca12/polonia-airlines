import { createFileRoute } from '@tanstack/react-router'
import { BookingPage } from '#/pages/BookingPage'

function BookingRoute() {
  const { flight, fare } = Route.useSearch()

  return <BookingPage flightNumber={flight} fareId={fare} />
}

export const Route = createFileRoute('/booking')({
  validateSearch: (search: Record<string, unknown>) => ({
    flight: typeof search.flight === 'string' ? search.flight : undefined,
    fare: typeof search.fare === 'number' ? search.fare : undefined,
  }),
  component: BookingRoute,
})