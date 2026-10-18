import { Collapse, UnstyledButton } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { ChevronDown } from 'lucide-react'
import { useId, useState } from 'react'
import { formatTime, formatUsd } from '#/shared/lib/formatting'
import {
  findCabin,
  findFare,
  getAircraftLabel,
  getCityName,
  getFlightTimes,
  getLowestFare,
} from '../model/flight.helpers'
import type { Flight } from '../model/flight.types'
import styles from '../styles/FlightResultCard.module.scss'
import { CabinSelector } from './CabinSelector'
import { FareOptions } from './FareOptions'
import { FlightDetails } from './FlightDetails'
import { FlightTimeline } from './FlightTimeline'

interface FlightResultCardProps {
  flight: Flight
  isRecommended: boolean
  expanded: boolean
  preferredCabin: string
  selectedFareId: number | null
  onExpand: () => void
  onSelectFare: (fareId: number) => void
}

function initialCabinId(flight: Flight, selectedFareId: number | null, preferredCabin: string) {
  const selectedCabin = selectedFareId === null ? undefined : findFare(flight, selectedFareId)?.cabin
  const cabin = selectedCabin ?? findCabin(flight, preferredCabin) ?? flight.brandedFares.at(0)

  return cabin?.classId ?? null
}

export function FlightResultCard({
  flight,
  isRecommended,
  expanded,
  preferredCabin,
  selectedFareId,
  onExpand,
  onSelectFare,
}: FlightResultCardProps) {
  const detailsId = useId()
  const faresId = useId()
  const [detailsOpened, { toggle: toggleDetails }] = useDisclosure(false)
  const [selectedCabinId, setSelectedCabinId] = useState(() =>
    initialCabinId(flight, selectedFareId, preferredCabin),
  )
  const selectedCabin = flight.brandedFares.find((cabin) => cabin.classId === selectedCabinId)
  const startingFare = getLowestFare(flight, preferredCabin)
  const times = getFlightTimes(flight)
  const departureCity = getCityName(flight.departureAirport.airportCode)
  const arrivalCity = getCityName(flight.arrivalAirport.airportCode)

  return (
    <article className={`${styles.card} ${expanded ? styles.expanded : ''}`}>
      <h3 className="polonia-visually-hidden">
        {flight.flightNumber}
        {times ? `, departs ${departureCity} ${formatTime(times.departure)}` : ''}, arrives{' '}
        {arrivalCity}
      </h3>

      {isRecommended && expanded && (
        <div className={styles.recommendation}>
          <p className={styles.recommendationTitle}>The Polonia recommendation</p>
          <p className={styles.recommendationDetail}>
            Shortest journey · {getAircraftLabel(flight)}
          </p>
        </div>
      )}

      <div className={styles.body}>
        <div className={styles.summary}>
          <div className={styles.schedule}>
            <FlightTimeline
              flight={flight}
              detailsOpened={detailsOpened}
              detailsId={detailsId}
              onToggleDetails={toggleDetails}
            />
          </div>

          {!expanded && (
            <div className={styles.startingFare}>
              {isRecommended && <span className={styles.badge}>Recommended</span>}
              {startingFare !== null && (
                <p className={styles.startingPrice}>
                  <span className={styles.fromLabel}>from</span> {formatUsd(startingFare)}
                </p>
              )}
              <UnstyledButton
                className={styles.viewFares}
                aria-expanded={false}
                aria-controls={faresId}
                onClick={onExpand}
              >
                View fares
                <ChevronDown size={13} aria-hidden="true" />
              </UnstyledButton>
            </div>
          )}
        </div>

        <FlightDetails id={detailsId} flight={flight} opened={detailsOpened} />

        <Collapse expanded={expanded} id={faresId}>
          {selectedCabin && (
            <div className={styles.fares}>
              <CabinSelector
                cabins={flight.brandedFares}
                selectedCabinId={selectedCabin.classId}
                onSelect={setSelectedCabinId}
              />
              <FareOptions
                cabin={selectedCabin}
                selectedFareId={selectedFareId}
                onSelect={onSelectFare}
              />
              <p className={styles.footnote}>
                Prices are per adult in US dollars for this flight. Times are shown in
                UTC; +1 indicates arrival on the next calendar day.
              </p>
            </div>
          )}
        </Collapse>
      </div>
    </article>
  )
}
