import { Link } from '@tanstack/react-router'
import { Button, Select } from '@mantine/core'
import { PlaneTakeoff } from 'lucide-react'
import { useMemo, useState } from 'react'
import { addDaysToDateKey, formatLongDate } from '#/shared/lib/formatting'
import { getFlights } from '../data/flight.repository'
import {
  getCityName,
  getDepartureDateKey,
  getLowestFare,
  getRecommendedFlight,
  sortFlights,
} from '../model/flight.helpers'
import type { FareSelection, FlightSortOrder } from '../model/flight.helpers'
import styles from '../styles/FlightResults.module.scss'
import { DateFareStrip } from './DateFareStrip'
import type { DateFare } from './DateFareStrip'
import { FlightResultCard } from './FlightResultCard'

const DATE_STRIP_RADIUS = 2

const SORT_OPTIONS: Array<{ value: FlightSortOrder; label: string }> = [
  { value: 'recommended', label: 'Recommended' },
  { value: 'price', label: 'Lowest price' },
  { value: 'duration', label: 'Shortest journey' },
  { value: 'departure', label: 'Earliest departure' },
]

interface FlightResultsProps {
  from?: string
  to?: string
  departureDate?: string
  preferredCabin: string
  selection: FareSelection | null
  onSelectFare: (selection: FareSelection) => void
  onDepartureDateChange: (dateKey: string) => void
}

function findNearestDate(dateKeys: string[], target: string): string | undefined {
  return dateKeys.find((dateKey) => dateKey >= target) ?? dateKeys.at(-1)
}

export function FlightResults({
  from,
  to,
  departureDate,
  preferredCabin,
  selection,
  onSelectFare,
  onDepartureDateChange,
}: FlightResultsProps) {
  const [sortOrder, setSortOrder] = useState<FlightSortOrder>('recommended')
  const [expandedFlight, setExpandedFlight] = useState<string | null>(null)
  const routeFlights = useMemo(() => getFlights({ from, to }), [from, to])
  const availableDates = useMemo(
    () =>
      [...new Set(routeFlights.map(getDepartureDateKey).filter((key) => key !== null))].sort(),
    [routeFlights],
  )
  const activeDate = departureDate ?? availableDates.at(0)
  const flightsOnDate = routeFlights.filter(
    (flight) => getDepartureDateKey(flight) === activeDate,
  )
  const sortedFlights = sortFlights(flightsOnDate, sortOrder, preferredCabin)
  const recommendedFlight = flightsOnDate.length > 1 ? getRecommendedFlight(flightsOnDate) : undefined
  const isListed = (flightNumber: string | null | undefined) =>
    sortedFlights.some((flight) => flight.flightNumber === flightNumber)
  const openFlight = isListed(expandedFlight)
    ? expandedFlight
    : isListed(selection?.flightNumber)
      ? selection?.flightNumber
      : sortedFlights.at(0)?.flightNumber
  const routeLabel =
    from && to ? `${getCityName(from)} to ${getCityName(to)}` : 'All available routes'

  if (routeFlights.length === 0 || !activeDate) {
    return (
      <section className={styles.emptyState} aria-labelledby="no-route-flights">
        <h2 id="no-route-flights" className={styles.emptyTitle}>
          No flights found for this route
        </h2>
        <p className={styles.emptyText}>
          Try a different departure airport or destination.
        </p>
        <Button component={Link} to="/" variant="outline">
          Modify search
        </Button>
      </section>
    )
  }

  const dateFares: DateFare[] = Array.from(
    { length: DATE_STRIP_RADIUS * 2 + 1 },
    (_, index) => {
      const dateKey = addDaysToDateKey(activeDate, index - DATE_STRIP_RADIUS)
      const fares = routeFlights
        .filter((flight) => getDepartureDateKey(flight) === dateKey)
        .map((flight) => getLowestFare(flight, preferredCabin))
        .filter((fare) => fare !== null)

      return { dateKey, lowestFare: fares.length > 0 ? Math.min(...fares) : null }
    },
  )
  const nearestDate = findNearestDate(availableDates, activeDate)

  return (
    <section className={styles.results} aria-labelledby="outbound-heading">
      <div className={styles.directionHeading}>
        <h2 id="outbound-heading" className={styles.direction}>
          <PlaneTakeoff className={styles.directionIcon} size={21} aria-hidden="true" />
          Outbound · {routeLabel}
        </h2>
        <p className={styles.directionDate}>{formatLongDate(activeDate)}</p>
      </div>

      <DateFareStrip
        dates={dateFares}
        selectedDateKey={activeDate}
        onSelect={onDepartureDateChange}
      />

      {sortedFlights.length === 0 ? (
        <div className={styles.noFlightsOnDate} role="status">
          <p className={styles.emptyText}>
            There are no flights on {formatLongDate(activeDate)}.
          </p>
          {nearestDate && (
            <Button variant="outline" onClick={() => onDepartureDateChange(nearestDate)}>
              View flights on {formatLongDate(nearestDate)}
            </Button>
          )}
        </div>
      ) : (
        <>
          <div className={styles.toolbar}>
            <p className={styles.count} role="status">
              {sortedFlights.length} {sortedFlights.length === 1 ? 'flight' : 'flights'}{' '}
              available · Prices per adult
            </p>
            <Select
              label="Sort by"
              size="xs"
              data={SORT_OPTIONS}
              value={sortOrder}
              allowDeselect={false}
              comboboxProps={{ width: 180, position: 'bottom-end' }}
              classNames={{
                root: styles.sort,
                label: styles.sortLabel,
                input: styles.sortInput,
              }}
              onChange={(value) => {
                const option = SORT_OPTIONS.find((item) => item.value === value)

                if (option) {
                  setSortOrder(option.value)
                }
              }}
            />
          </div>

          <div className={styles.list}>
            {sortedFlights.map((flight) => (
              <FlightResultCard
                key={flight.flightNumber}
                flight={flight}
                isRecommended={flight === recommendedFlight}
                expanded={flight.flightNumber === openFlight}
                preferredCabin={preferredCabin}
                selectedFareId={
                  selection?.flightNumber === flight.flightNumber ? selection.fareId : null
                }
                onExpand={() => setExpandedFlight(flight.flightNumber)}
                onSelectFare={(fareId) =>
                  onSelectFare({ flightNumber: flight.flightNumber, fareId })
                }
              />
            ))}
          </div>
        </>
      )}
    </section>
  )
}
