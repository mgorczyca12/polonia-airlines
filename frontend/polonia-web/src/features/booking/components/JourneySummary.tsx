import { Plane, ShieldCheck } from 'lucide-react'
import type { BrandedFare, Flight } from '#/features/flight-results'
import {
  getCityName,
  getFlightNumbersLabel,
  getFlightTimes,
  getStopsLabel,
} from '#/features/flight-results'
import { formatTravellers } from '#/features/flight-search'
import type {
  SeatedPassenger,
  TravellerCounts,
  TripType,
} from '#/features/flight-search'
import {
  formatDuration,
  formatShortDate,
  formatTime,
  formatUsd,
  toDateKey,
} from '#/shared/lib/formatting'
import styles from '../styles/JourneySummary.module.scss'

interface PassengerFareLine {
  passenger: SeatedPassenger
  fare?: BrandedFare
}

interface JourneySummaryProps {
  from?: string
  to?: string
  tripType: TripType
  travellers: TravellerCounts
  cabinLabel: string
  returnDate?: string
  flight?: Flight
  passengerFares: PassengerFareLine[]
}

function OutboundLeg({ flight }: { flight?: Flight }) {
  const times = flight ? getFlightTimes(flight) : null

  if (!flight || !times) {
    return (
      <div className={styles.leg}>
        <p className={styles.legLabel}>Outbound</p>
        <p className={styles.legPending}>Choose a fare to add this flight.</p>
      </div>
    )
  }

  const dayOffset =
    times.arrivalDayOffset > 0 ? ` +${times.arrivalDayOffset}` : ''

  return (
    <div className={styles.leg}>
      <p className={styles.legLabel}>
        Outbound · {formatShortDate(toDateKey(times.departure))}
      </p>
      <p className={styles.legRoute}>
        {flight.departureAirport.airportCode} {formatTime(times.departure)} →{' '}
        {flight.arrivalAirport.airportCode} {formatTime(times.arrival)}
        {dayOffset}
      </p>
      <p className={styles.legDetail}>
        {getFlightNumbersLabel(flight)} · {getStopsLabel(flight)} ·{' '}
        {formatDuration(flight.totalDurationMinutes)}
      </p>
    </div>
  )
}

export function JourneySummary({
  from,
  to,
  tripType,
  travellers,
  cabinLabel,
  returnDate,
  flight,
  passengerFares,
}: JourneySummaryProps) {
  const isReturn = tripType === 'roundTrip'
  const selectedFares = passengerFares.flatMap(({ fare }) =>
    fare ? [fare] : [],
  )
  const isComplete =
    passengerFares.length > 0 && selectedFares.length === passengerFares.length
  const fareTotal = isComplete
    ? selectedFares.reduce((total, fare) => total + fare.farePriceUsd, 0)
    : null
  const fareNames = new Set(selectedFares.map((fare) => fare.fareName))
  const fareLabel =
    fareNames.size > 1
      ? 'Mixed fares'
      : (selectedFares.at(0)?.fareName ?? cabinLabel)
  const routeLabel =
    from && to
      ? `${getCityName(from)} ${isReturn ? '↔' : '→'} ${getCityName(to)}`
      : 'Your flights'

  return (
    <section
      className={styles.summary}
      aria-labelledby="journey-summary-heading"
    >
      <div className={styles.heading}>
        <h2 id="journey-summary-heading" className={styles.title}>
          Your journey
        </h2>
        <Plane className={styles.headingIcon} size={22} aria-hidden="true" />
      </div>

      <div className={styles.destination}>
        <p className={styles.route}>{routeLabel}</p>
        <p className={styles.meta}>
          {isReturn ? 'Return' : 'One way'} · {formatTravellers(travellers)} ·{' '}
          {fareLabel}
        </p>
      </div>

      <div className={styles.section}>
        <OutboundLeg flight={flight} />
        {isReturn && (
          <div className={styles.leg}>
            <p className={styles.legLabel}>
              Return{returnDate ? ` · ${formatShortDate(returnDate)}` : ''}
            </p>
            <p className={styles.legPending}>
              Not available in the current schedule.
            </p>
          </div>
        )}
      </div>

      <div className={styles.section}>
        <p className={styles.breakdownTitle}>Price breakdown</p>
        {passengerFares.map(({ passenger, fare }) => (
          <div key={passenger.id} className={styles.priceItem}>
            <span>
              {passenger.label} · {fare?.fareName ?? 'Fare not chosen'}
            </span>
            <span className={styles.priceValue}>
              {fare ? formatUsd(fare.farePriceUsd, { exact: true }) : '—'}
            </span>
          </div>
        ))}
        {travellers.infant > 0 && (
          <div className={styles.priceItem}>
            <span>Infants · {travellers.infant}</span>
            <span className={styles.priceValue}>Priced at booking</span>
          </div>
        )}
        <span className={styles.divider} aria-hidden="true" />
        <div className={styles.total}>
          <div>
            <p className={styles.totalLabel}>Fare total</p>
            <p className={styles.totalNote}>For seated travellers · USD</p>
          </div>
          <p className={styles.totalValue}>
            {fareTotal === null ? '—' : formatUsd(fareTotal, { exact: true })}
          </p>
        </div>
      </div>

      <p className={styles.reassurance}>
        <ShieldCheck size={16} aria-hidden="true" />
        You won't be charged until payment.
      </p>
    </section>
  )
}
