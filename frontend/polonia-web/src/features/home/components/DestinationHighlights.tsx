import { Link } from '@tanstack/react-router'
import { getAirportByCode } from '#/entities/airport'
import { getCityName, getFlights, getLowestFare } from '#/features/flight-results'
import { formatUsd } from '#/shared/lib/formatting'
import { PhotoPlaceholder } from '#/shared/ui/PhotoPlaceholder'
import styles from '../styles/DestinationHighlights.module.scss'

interface Destination {
  originCode: string
  destinationCode: string
  lowestFare: number
}

function getDestinations(): Destination[] {
  const destinations = new Map<string, Destination>()

  for (const flight of getFlights()) {
    const originCode = flight.departureAirport.airportCode
    const destinationCode = flight.arrivalAirport.airportCode
    const lowestFare = getLowestFare(flight)
    const key = `${originCode}-${destinationCode}`
    const existing = destinations.get(key)

    if (lowestFare !== null && (!existing || lowestFare < existing.lowestFare)) {
      destinations.set(key, { originCode, destinationCode, lowestFare })
    }
  }

  return [...destinations.values()]
}

const destinations = getDestinations()

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
        {destinations.map(({ originCode, destinationCode, lowestFare }) => {
          const city = getCityName(destinationCode)

          return (
            <li key={`${originCode}-${destinationCode}`}>
              <Link
                className={styles.card}
                to="/flightlist"
                search={{ from: originCode, to: destinationCode, tripType: 'oneWay' }}
              >
                <PhotoPlaceholder className={styles.photo} />
                <div className={styles.information}>
                  <div className={styles.name}>
                    <p className={styles.country}>
                      {getAirportByCode(destinationCode)?.Country ?? destinationCode}
                    </p>
                    <p className={styles.city}>{city}</p>
                  </div>
                  <div className={styles.fare}>
                    <p className={styles.fareLabel}>One way from {getCityName(originCode)}</p>
                    <p className={styles.farePrice}>{formatUsd(lowestFare)}</p>
                  </div>
                </div>
              </Link>
            </li>
          )
        })}
      </ul>

      <p className={styles.footnote}>
        Lowest published fare per adult, in US dollars. Subject to availability.
      </p>
    </section>
  )
}
