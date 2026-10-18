import { Plane, ShieldCheck } from 'lucide-react'
import type { BrandedFare, Flight } from '#/features/flight-results'
import {
  getCityName,
  getFlightNumbersLabel,
  getFlightTimes,
  getStopsLabel,
} from '#/features/flight-results'
import { formatTravellers, seatedTravellerCount } from '#/features/flight-search'
import type { TravellerCounts, TripType } from '#/features/flight-search'
import {
  formatDuration,
  formatShortDate,
  formatTime,
  formatUsd,
  toDateKey,
} from '#/shared/lib/formatting'
import styles from '../styles/JourneySummary.module.scss'

interface JourneySummaryProps {
  from?: string
  to?: string
  tripType: TripType
  travellers: TravellerCounts
  cabinLabel: string
  returnDate?: string
  flight?: Flight
  fare?: BrandedFare
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

  const dayOffset = times.arrivalDayOffset > 0 ? ` +${times.arrivalDayOffset}` : ''

  return (
    <div className={styles.leg}>
      <p className={styles.legLabel}>Outbound · {formatShortDate(toDateKey(times.departure))}</p>
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
  fare,
}: JourneySummaryProps) {
  const isReturn = tripType === 'roundTrip'
  const seatedTravellers = seatedTravellerCount(travellers)
  const fareTotal = fare ? fare.farePriceUsd * seatedTravellers : null
  const seatedTravellerLabel = formatTravellers({ ...travellers, infant: 0 })
  const routeLabel =
    from && to
      ? `${getCityName(from)} ${isReturn ? '↔' : '→'} ${getCityName(to)}`
      : 'Your flights'

  return (
    <section className={styles.summary} aria-labelledby="journey-summary-heading">
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
          {fare?.fareName ?? cabinLabel}
        </p>
      </div>

      <div className={styles.section}>
        <OutboundLeg flight={flight} />
        {isReturn && (
          <div className={styles.leg}>
            <p className={styles.legLabel}>
              Return{returnDate ? ` · ${formatShortDate(returnDate)}` : ''}
            </p>
            <p className={styles.legPending}>Not available in the current schedule.</p>
          </div>
        )}
      </div>

      <div className={styles.section}>
        <p className={styles.breakdownTitle}>Price breakdown</p>
        <div className={styles.priceItem}>
          <span>Fares · {seatedTravellerLabel}</span>
          <span className={styles.priceValue}>
            {fareTotal === null ? '—' : formatUsd(fareTotal, { exact: true })}
          </span>
        </div>
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
