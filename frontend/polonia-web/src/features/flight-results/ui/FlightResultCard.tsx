import { Checkbox, Collapse, UnstyledButton } from '@mantine/core'
import { useId, useState } from 'react'
import type { CabinFares, Fare } from '#/entities/fare'
import type { FlightSummary } from '#/entities/flight'
import type { SeatedPassenger } from '#/features/flight-search'
import { formatMoney, formatTime } from '#/shared/lib/formatting'
import { CabinSelector } from './CabinSelector'
import { FareBaggageComparison } from './FareBaggageComparison'
import { FareOptions } from './FareOptions'
import { FlightTimeline } from './FlightTimeline'
import { PassengerFareSelector } from './PassengerFareSelector'
import styles from './FlightResultCard.module.scss'

interface FlightResultCardProps {
  flight: FlightSummary
  cabins: CabinFares[]
  preferredCabin?: string
  expanded: boolean
  passengers: SeatedPassenger[]
  /** This flight's chosen product per passenger index. */
  productIds: Array<string | undefined>
  sameFareForAll: boolean
  disabled: boolean
  onSameFareForAllChange: (checked: boolean) => void
  onExpand: () => void
  onSelectFare: (passengerIndexes: number[], productId: string) => void
}

function findFare(cabins: CabinFares[], productId: string | undefined) {
  return cabins
    .flatMap((cabin) => cabin.fares)
    .find((fare) => fare.productId === productId)
}

function initialCabin(
  cabins: CabinFares[],
  selectedFare: Fare | undefined,
  preferredCabin: string | undefined,
): string | undefined {
  return (
    selectedFare?.cabinClass ??
    cabins.find((cabin) => cabin.cabinClass === preferredCabin)?.cabinClass ??
    cabins.at(0)?.cabinClass
  )
}

export function FlightResultCard({
  flight,
  cabins,
  preferredCabin,
  expanded,
  passengers,
  productIds,
  sameFareForAll,
  disabled,
  onSameFareForAllChange,
  onExpand,
  onSelectFare,
}: FlightResultCardProps) {
  const faresId = useId()
  const hasFares = cabins.length > 0
  const hasMultiplePassengers = passengers.length > 1
  const choosePerPassenger = hasMultiplePassengers && !sameFareForAll
  const firstPendingIndex = passengers.findIndex(
    (_, index) => productIds[index] === undefined,
  )
  const [activeIndex, setActiveIndex] = useState(Math.max(firstPendingIndex, 0))
  const activeProductId = productIds[activeIndex]
  const [selectedCabin, setSelectedCabin] = useState(() =>
    initialCabin(
      cabins,
      findFare(cabins, activeProductId ?? productIds.find(Boolean)),
      preferredCabin,
    ),
  )
  const cabin =
    cabins.find((item) => item.cabinClass === selectedCabin) ?? cabins.at(0)
  const startingFare = cabins.at(0)?.fares.at(0)
  const fareNames = passengers.map(
    (_, index) => findFare(cabins, productIds[index])?.brand,
  )
  const assignedLabels: Record<string, string[]> = {}

  if (choosePerPassenger) {
    passengers.forEach((passenger, index) => {
      const productId = productIds[index]

      if (productId && index !== activeIndex) {
        assignedLabels[productId] = [
          ...(assignedLabels[productId] ?? []),
          passenger.label,
        ]
      }
    })
  }

  const activatePassenger = (index: number) => {
    const fare = findFare(cabins, productIds[index])
    setActiveIndex(index)

    if (fare) {
      setSelectedCabin(fare.cabinClass)
    }
  }

  const handleSelectFare = (productId: string) => {
    if (!choosePerPassenger) {
      onSelectFare(
        passengers.map((_, index) => index),
        productId,
      )
      return
    }

    onSelectFare([activeIndex], productId)
    const nextPending = passengers.findIndex(
      (_, index) => index !== activeIndex && productIds[index] === undefined,
    )

    if (nextPending >= 0) {
      setActiveIndex(nextPending)
    }
  }

  const handleSameFareChange = (checked: boolean) => {
    const referenceProductId = activeProductId ?? productIds.find(Boolean)
    onSameFareForAllChange(checked)

    if (checked && referenceProductId) {
      onSelectFare(
        passengers.map((_, index) => index),
        referenceProductId,
      )
    }
  }

  return (
    <article className={`${styles.card} ${expanded ? styles.expanded : ''}`}>
      <h3 className="polonia-visually-hidden">
        {flight.flightNumber}, departs {flight.departure.city}{' '}
        {formatTime(flight.departure.time)} UTC, arrives {flight.arrival.city}
      </h3>

      <div className={styles.body}>
        {!expanded && hasFares && (
          <UnstyledButton
            className={styles.cardToggle}
            aria-label={`Show fares for ${flight.flightNumber}`}
            aria-expanded={false}
            aria-controls={faresId}
            onClick={onExpand}
          />
        )}
        <div className={styles.summary}>
          <div className={styles.schedule}>
            <FlightTimeline flight={flight} />
          </div>

          {!expanded && (
            <div className={styles.startingFare}>
              {startingFare ? (
                <p className={styles.startingPrice}>
                  <span className={styles.fromLabel}>from</span>{' '}
                  {formatMoney(startingFare.amount, startingFare.currency)}
                </p>
              ) : (
                <p className={styles.unpriced}>No fares currently priced</p>
              )}
            </div>
          )}
        </div>

        <Collapse expanded={expanded && hasFares} id={faresId}>
          {cabin && (
            <div className={styles.fares}>
              {hasMultiplePassengers && (
                <Checkbox
                  size="xs"
                  className={styles.sameFare}
                  label="Use the same fare for all passengers"
                  checked={sameFareForAll}
                  disabled={disabled}
                  onChange={(event) =>
                    handleSameFareChange(event.currentTarget.checked)
                  }
                />
              )}
              {choosePerPassenger && (
                <PassengerFareSelector
                  passengers={passengers}
                  activeIndex={activeIndex}
                  fareNames={fareNames}
                  onSelect={activatePassenger}
                />
              )}
              <CabinSelector
                cabins={cabins}
                selectedCabin={cabin.cabinClass}
                onSelect={setSelectedCabin}
              />
              <FareOptions
                cabin={cabin}
                selectedProductId={activeProductId}
                selectLabel={
                  choosePerPassenger
                    ? `Select for ${passengers[activeIndex].label}`
                    : 'Select fare'
                }
                assignedLabels={assignedLabels}
                reserveAssignmentRow={choosePerPassenger}
                disabled={disabled}
                onSelect={handleSelectFare}
              />
              <FareBaggageComparison
                cabins={cabins}
                selectedProductIds={
                  new Set(productIds.filter((id): id is string => Boolean(id)))
                }
              />
              <p className={styles.footnote}>
                Current prices per traveller for this flight; children pay the
                selected fare. Your price is confirmed on the next step. Times
                are in UTC; +1 indicates arrival on the next calendar day.
              </p>
            </div>
          )}
        </Collapse>
      </div>
    </article>
  )
}
