import { Checkbox, Collapse, UnstyledButton } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { ChevronDown } from 'lucide-react'
import { useId, useState } from 'react'
import type { SeatedPassenger } from '#/features/flight-search'
import { formatTime, formatUsd } from '#/shared/lib/formatting'
import {
  findCabin,
  findFare,
  getAircraftLabel,
  getCityName,
  getFlightTimes,
  getLowestFare,
} from '../model/flight.helpers'
import type { PassengerFareIds } from '../model/flight.helpers'
import type { Flight } from '../model/flight.types'
import styles from '../styles/FlightResultCard.module.scss'
import { CabinSelector } from './CabinSelector'
import { FareOptions } from './FareOptions'
import { FlightDetails } from './FlightDetails'
import { FlightTimeline } from './FlightTimeline'
import { PassengerFareSelector } from './PassengerFareSelector'

interface FlightResultCardProps {
  flight: Flight
  isRecommended: boolean
  expanded: boolean
  preferredCabin: string
  passengers: SeatedPassenger[]
  fareIds: PassengerFareIds
  sameFareForAll: boolean
  onSameFareForAllChange: (checked: boolean) => void
  onExpand: () => void
  onSelectFare: (passengerIds: string[], fareId: number) => void
}

function initialCabinId(
  flight: Flight,
  selectedFareId: number | undefined,
  preferredCabin: string,
) {
  const selectedCabin =
    selectedFareId === undefined
      ? undefined
      : findFare(flight, selectedFareId)?.cabin
  const cabin =
    selectedCabin ??
    findCabin(flight, preferredCabin) ??
    flight.brandedFares.at(0)

  return cabin?.classId ?? null
}

export function FlightResultCard({
  flight,
  isRecommended,
  expanded,
  preferredCabin,
  passengers,
  fareIds,
  sameFareForAll,
  onSameFareForAllChange,
  onExpand,
  onSelectFare,
}: FlightResultCardProps) {
  const detailsId = useId()
  const faresId = useId()
  const [detailsOpened, { toggle: toggleDetails }] = useDisclosure(false)
  const hasMultiplePassengers = passengers.length > 1
  const passengerIds = passengers.map((passenger) => passenger.id)
  const firstPendingPassenger = passengers.find(
    (passenger) => fareIds[passenger.id] === undefined,
  )
  const [activePassengerId, setActivePassengerId] = useState(
    () => (firstPendingPassenger ?? passengers.at(0))?.id,
  )
  const activePassenger =
    passengers.find((passenger) => passenger.id === activePassengerId) ??
    passengers.at(0)
  const activeFareId = activePassenger ? fareIds[activePassenger.id] : undefined
  const [selectedCabinId, setSelectedCabinId] = useState(() =>
    initialCabinId(
      flight,
      activeFareId ?? Object.values(fareIds).at(0),
      preferredCabin,
    ),
  )
  const selectedCabin = flight.brandedFares.find(
    (cabin) => cabin.classId === selectedCabinId,
  )
  const startingFare = getLowestFare(flight, preferredCabin)
  const times = getFlightTimes(flight)
  const departureCity = getCityName(flight.departureAirport.airportCode)
  const arrivalCity = getCityName(flight.arrivalAirport.airportCode)
  const choosePerPassenger = hasMultiplePassengers && !sameFareForAll
  const fareNames = Object.fromEntries(
    passengers.map((passenger) => {
      const fareId = fareIds[passenger.id]
      return [
        passenger.id,
        fareId === undefined
          ? undefined
          : findFare(flight, fareId)?.fare.fareName,
      ]
    }),
  )
  const assignedLabels: Record<number, string[]> = {}

  if (choosePerPassenger) {
    for (const passenger of passengers) {
      const fareId = fareIds[passenger.id]

      if (fareId !== undefined && passenger.id !== activePassenger?.id) {
        assignedLabels[fareId] = [
          ...(assignedLabels[fareId] ?? []),
          passenger.label,
        ]
      }
    }
  }

  const activatePassenger = (passengerId: string) => {
    const fareId = fareIds[passengerId]
    const cabinId =
      fareId === undefined ? undefined : findFare(flight, fareId)?.cabin.classId

    setActivePassengerId(passengerId)

    if (cabinId !== undefined) {
      setSelectedCabinId(cabinId)
    }
  }

  const handleSelectFare = (fareId: number) => {
    if (!activePassenger) {
      return
    }

    if (!choosePerPassenger) {
      onSelectFare(passengerIds, fareId)
      return
    }

    onSelectFare([activePassenger.id], fareId)

    const nextPassenger = passengers.find(
      (passenger) =>
        passenger.id !== activePassenger.id &&
        fareIds[passenger.id] === undefined,
    )

    if (nextPassenger) {
      setActivePassengerId(nextPassenger.id)
    }
  }

  const handleSameFareChange = (checked: boolean) => {
    const referenceFareId = activeFareId ?? Object.values(fareIds).at(0)

    onSameFareForAllChange(checked)

    if (checked && referenceFareId !== undefined) {
      onSelectFare(passengerIds, referenceFareId)
    }
  }

  return (
    <article className={`${styles.card} ${expanded ? styles.expanded : ''}`}>
      <h3 className="polonia-visually-hidden">
        {flight.flightNumber}
        {times
          ? `, departs ${departureCity} ${formatTime(times.departure)}`
          : ''}
        , arrives {arrivalCity}
      </h3>

      {isRecommended && expanded && (
        <div className={styles.recommendation}>
          <p className={styles.recommendationTitle}>
            The Polonia recommendation
          </p>
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
              {isRecommended && (
                <span className={styles.badge}>Recommended</span>
              )}
              {startingFare !== null && (
                <p className={styles.startingPrice}>
                  <span className={styles.fromLabel}>from</span>{' '}
                  {formatUsd(startingFare)}
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
              {hasMultiplePassengers && (
                <Checkbox
                  size="xs"
                  className={styles.sameFare}
                  label="Use the same fare for all passengers"
                  checked={sameFareForAll}
                  onChange={(event) =>
                    handleSameFareChange(event.currentTarget.checked)
                  }
                />
              )}
              {choosePerPassenger && activePassenger && (
                <PassengerFareSelector
                  passengers={passengers}
                  activePassengerId={activePassenger.id}
                  fareNames={fareNames}
                  onSelect={activatePassenger}
                />
              )}
              <CabinSelector
                cabins={flight.brandedFares}
                selectedCabinId={selectedCabin.classId}
                onSelect={setSelectedCabinId}
              />
              <FareOptions
                cabin={selectedCabin}
                selectedFareId={activeFareId ?? null}
                selectLabel={
                  choosePerPassenger && activePassenger
                    ? `Select for ${activePassenger.label}`
                    : 'Select fare'
                }
                assignedLabels={assignedLabels}
                reserveAssignmentRow={choosePerPassenger}
                onSelect={handleSelectFare}
              />
              <p className={styles.footnote}>
                Prices are per adult in US dollars for this flight. Times are
                shown in UTC; +1 indicates arrival on the next calendar day.
              </p>
            </div>
          )}
        </Collapse>
      </div>
    </article>
  )
}
