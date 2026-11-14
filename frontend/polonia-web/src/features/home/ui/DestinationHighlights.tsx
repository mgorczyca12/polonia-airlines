import { Link } from '@tanstack/react-router'
import { getAirportByCode } from '#/entities/airport'
import { PhotoPlaceholder } from '#/shared/ui/PhotoPlaceholder'
import styles from './DestinationHighlights.module.scss'

interface Destination {
  originCode: string
  destinationCode: string
}

const destinations: Destination[] = [
  { originCode: 'ORD', destinationCode: 'KRK' },
]

export function DestinationHighlights() {
  if (destinations.length === 0) {
    return null
  }

  return (
    <section className={styles.section} aria-labelledby="destinations-heading">
      <div className={styles.header}>
        <p className={styles.eyebrow}>Your next chapter</p>
        <h2 id="destinations-heading" className={styles.title}>
          Somewhere extraordinary awaits.
        </h2>
      </div>

      <ul className={styles.cards}>
        {destinations.map(({ originCode, destinationCode }) => {
          const city =
            getAirportByCode(destinationCode)?.City ?? destinationCode

          return (
            <li key={`${originCode}-${destinationCode}`}>
              <Link
                className={styles.card}
                to="/"
                search={{
                  from: originCode,
                  to: destinationCode,
                  tripType: 'oneWay',
                }}
              >
                <PhotoPlaceholder className={styles.photo} />
                <div className={styles.information}>
                  <div className={styles.name}>
                    <p className={styles.country}>
                      {getAirportByCode(destinationCode)?.Country ??
                        destinationCode}
                    </p>
                    <p className={styles.city}>{city}</p>
                  </div>
                  <div className={styles.fare}>
                    <p className={styles.fareLabel}>Explore this destination</p>
                    <p className={styles.farePrice}>Choose dates</p>
                  </div>
                </div>
              </Link>
            </li>
          )
        })}
      </ul>

      <p className={styles.footnote}>
        Destination inspiration only. Search your dates for scheduled flights
        and current prices.
      </p>
    </section>
  )
}
