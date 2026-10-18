import { Link } from '@tanstack/react-router'
import { SlidersHorizontal } from 'lucide-react'
import { getAirportByCode } from '#/entities/airport'
import { formatDateRange } from '#/shared/lib/formatting'
import {
  DEFAULT_CABIN,
  formatTravellers,
  resolveTravellers,
  resolveTripType,
} from '../model/search-params'
import type { FlightSearchParams } from '../model/search-params'
import styles from '../styles/SearchRecap.module.scss'

interface SearchRecapProps {
  search: FlightSearchParams
}

function airportLabel(code: string | undefined, fallback: string) {
  if (!code) {
    return fallback
  }

  const city = getAirportByCode(code)?.City
  return city ? `${city} ${code}` : code
}

export function SearchRecap({ search }: SearchRecapProps) {
  const isReturn = resolveTripType(search) === 'roundTrip'
  const route = `${airportLabel(search.from, 'Any departure')} ${isReturn ? '↔' : '→'} ${airportLabel(search.to, 'Any destination')}`

  return (
    <section className={styles.recap} aria-label="Your search">
      <div className={styles.inner}>
        <div className={styles.criteria}>
          <p className={styles.route}>{route}</p>
          <span className={styles.divider} aria-hidden="true" />
          <p className={styles.detail}>
            {search.departure
              ? formatDateRange(search.departure, isReturn ? search.return : undefined)
              : 'Flexible dates'}
          </p>
          <p className={styles.detail}>
            {formatTravellers(resolveTravellers(search))} · {search.cabin ?? DEFAULT_CABIN}
          </p>
        </div>
        <Link className={styles.modify} to="/" search={search}>
          <SlidersHorizontal size={17} aria-hidden="true" />
          Modify search
        </Link>
      </div>
    </section>
  )
}
