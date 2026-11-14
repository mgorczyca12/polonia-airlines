import { Link } from '@tanstack/react-router'
import { Button, Group, Text } from '@mantine/core'
import { PlaneTakeoff } from 'lucide-react'
import { useState } from 'react'
import { faresForFlight, groupFaresByCabin } from '#/entities/fare'
import type { Fare } from '#/entities/fare'
import type { FlightSummary } from '#/entities/flight'
import type { SeatedPassenger } from '#/features/flight-search'
import { formatLongDate } from '#/shared/lib/formatting'
import type { FareSelection } from '../model/fare-selection'
import { DateNavigator } from './DateNavigator'
import { FlightResultCard } from './FlightResultCard'
import styles from './FlightResultsList.module.scss'

interface FlightResultsListProps {
  routeLabel: string
  departureDateKey: string
  minDateKey: string
  flights: FlightSummary[]
  fares: Fare[]
  passengers: SeatedPassenger[]
  preferredCabin?: string
  selection: FareSelection | null
  page: number
  hasNextPage: boolean
  isUpdating: boolean
  disabled: boolean
  onSelectFare: (
    flightId: number,
    passengerIndexes: number[],
    productId: string,
  ) => void
  onPageChange: (page: number) => void
  onDateChange: (dateKey: string) => void
}

export function FlightResultsList({
  routeLabel,
  departureDateKey,
  minDateKey,
  flights,
  fares,
  passengers,
  preferredCabin,
  selection,
  page,
  hasNextPage,
  isUpdating,
  disabled,
  onSelectFare,
  onPageChange,
  onDateChange,
}: FlightResultsListProps) {
  const [expandedFlightId, setExpandedFlightId] = useState<number | null>(null)
  const [sameFareForAll, setSameFareForAll] = useState(true)
  const isListed = (flightId: number | null | undefined) =>
    flights.some((flight) => flight.id === flightId)
  const openFlightId = isListed(expandedFlightId)
    ? expandedFlightId
    : isListed(selection?.flightId)
      ? selection?.flightId
      : flights.at(0)?.id

  return (
    <section className={styles.results} aria-labelledby="outbound-heading">
      <div className={styles.directionHeading}>
        <h2 id="outbound-heading" className={styles.direction}>
          <PlaneTakeoff
            className={styles.directionIcon}
            size={21}
            aria-hidden="true"
          />
          Outbound · {routeLabel}
        </h2>
        <p className={styles.directionDate}>
          {formatLongDate(departureDateKey)}
        </p>
      </div>

      <DateNavigator
        selectedDateKey={departureDateKey}
        minDateKey={minDateKey}
        onSelect={onDateChange}
      />

      <div
        className={`${styles.list} ${isUpdating ? styles.updating : ''}`}
        aria-busy={isUpdating}
      >
        {flights.length === 0 ? (
          <div className={styles.emptyState} role="status">
            <p className={styles.emptyText}>
              No scheduled flights match this search on{' '}
              {formatLongDate(departureDateKey)}
              {page > 1 ? ` after page ${page - 1}` : ''}. Choose another date
              or route.
            </p>
            <Button component={Link} to="/" variant="outline">
              Modify search
            </Button>
          </div>
        ) : (
          <>
            <p className={styles.count} role="status">
              {flights.length} {flights.length === 1 ? 'flight' : 'flights'} on
              this page · Prices per traveller
            </p>
            {flights.map((flight) => (
              <FlightResultCard
                key={flight.id}
                flight={flight}
                cabins={groupFaresByCabin(faresForFlight(fares, flight.id))}
                preferredCabin={preferredCabin}
                expanded={flight.id === openFlightId}
                passengers={passengers}
                productIds={
                  selection?.flightId === flight.id ? selection.productIds : []
                }
                sameFareForAll={sameFareForAll}
                disabled={disabled}
                onSameFareForAllChange={setSameFareForAll}
                onExpand={() => setExpandedFlightId(flight.id)}
                onSelectFare={(passengerIndexes, productId) =>
                  onSelectFare(flight.id, passengerIndexes, productId)
                }
              />
            ))}
          </>
        )}
      </div>

      {(page > 1 || hasNextPage) && (
        <Group justify="space-between">
          <Button
            variant="outline"
            disabled={page === 1 || disabled}
            onClick={() => onPageChange(page - 1)}
          >
            Previous page
          </Button>
          <Text size="sm">Page {page}</Text>
          <Button
            variant="outline"
            disabled={!hasNextPage || disabled}
            onClick={() => onPageChange(page + 1)}
          >
            Next page
          </Button>
        </Group>
      )}
    </section>
  )
}
