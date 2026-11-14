import { Plane, ShieldCheck } from 'lucide-react'
import type { ReactNode } from 'react'
import type { FlightSummary } from '#/entities/flight'
import {
  formatDuration,
  formatMoney,
  formatShortDate,
  formatTime,
} from '#/shared/lib/formatting'
import styles from './JourneySummary.module.scss'

export interface JourneySummaryLine {
  key: string
  label: string
  /** `null` while this line has no price yet. */
  amount: number | null
}

interface JourneySummaryProps {
  routeLabel: string
  details: string
  flight?: FlightSummary
  /** Message shown in place of the flight before one is chosen or loaded. */
  pendingFlightMessage?: string
  lines: JourneySummaryLine[]
  /** Authoritative total, e.g. from an offer; otherwise the sum of priced lines. */
  total?: number
  currency: string
  totalLabel: string
  totalNote: string
  notice: string
  children?: ReactNode
}

function OutboundLeg({
  flight,
  pendingMessage,
}: {
  flight?: FlightSummary
  pendingMessage: string
}) {
  if (!flight) {
    return (
      <div className={styles.leg}>
        <p className={styles.legLabel}>Outbound</p>
        <p className={styles.legPending}>{pendingMessage}</p>
      </div>
    )
  }

  const dayOffset =
    flight.arrivalDayOffset > 0 ? ` +${flight.arrivalDayOffset}` : ''

  return (
    <div className={styles.leg}>
      <p className={styles.legLabel}>
        Outbound · {formatShortDate(flight.departureDateKey)}
      </p>
      <p className={styles.legRoute}>
        {flight.departure.airportCode} {formatTime(flight.departure.time)} →{' '}
        {flight.arrival.airportCode} {formatTime(flight.arrival.time)}
        {dayOffset} UTC
      </p>
      <p className={styles.legDetail}>
        {flight.flightNumber} · {formatDuration(flight.durationMinutes)}
      </p>
    </div>
  )
}

export function JourneySummary({
  routeLabel,
  details,
  flight,
  pendingFlightMessage = 'Choose a fare to add this flight.',
  lines,
  total: authoritativeTotal,
  currency,
  totalLabel,
  totalNote,
  notice,
  children,
}: JourneySummaryProps) {
  const isPriced =
    lines.length > 0 && lines.every((line) => line.amount !== null)
  const lineTotal = isPriced
    ? lines.reduce((sum, line) => sum + (line.amount ?? 0), 0)
    : null
  const total = authoritativeTotal ?? lineTotal

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
        <p className={styles.meta}>{details}</p>
      </div>

      <div className={styles.section}>
        <OutboundLeg flight={flight} pendingMessage={pendingFlightMessage} />
      </div>

      <div className={styles.section}>
        <p className={styles.breakdownTitle}>Price breakdown</p>
        {lines.map((line) => (
          <div key={line.key} className={styles.priceItem}>
            <span>{line.label}</span>
            <span className={styles.priceValue}>
              {line.amount === null ? '—' : formatMoney(line.amount, currency)}
            </span>
          </div>
        ))}
        <span className={styles.divider} aria-hidden="true" />
        <div className={styles.total}>
          <div>
            <p className={styles.totalLabel}>{totalLabel}</p>
            <p className={styles.totalNote}>{totalNote}</p>
          </div>
          <p className={styles.totalValue}>
            {total === null ? '—' : formatMoney(total, currency)}
          </p>
        </div>
        {children}
      </div>

      <p className={styles.reassurance}>
        <ShieldCheck size={16} aria-hidden="true" />
        {notice}
      </p>
    </section>
  )
}
