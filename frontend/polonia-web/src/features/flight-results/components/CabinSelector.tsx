import { UnstyledButton } from '@mantine/core'
import { formatUsd } from '#/shared/lib/formatting'
import { getLowestCabinFare } from '../model/flight.helpers'
import type { FlightCabin } from '../model/flight.types'
import styles from '../styles/CabinSelector.module.scss'

interface CabinSelectorProps {
  cabins: FlightCabin[]
  selectedCabinId: number
  onSelect: (cabinId: number) => void
}

export function CabinSelector({ cabins, selectedCabinId, onSelect }: CabinSelectorProps) {
  return (
    <div className={styles.selector} role="group" aria-label="Cabin">
      {cabins.map((cabin) => {
        const isSelected = cabin.classId === selectedCabinId
        const lowestFare = getLowestCabinFare(cabin)

        return (
          <UnstyledButton
            key={cabin.classId}
            className={`${styles.cabin} ${isSelected ? styles.selected : ''}`}
            aria-pressed={isSelected}
            onClick={() => onSelect(cabin.classId)}
          >
            <span className={styles.name}>{cabin.className}</span>
            {lowestFare !== null && (
              <span className={styles.price}>from {formatUsd(lowestFare)}</span>
            )}
          </UnstyledButton>
        )
      })}
    </div>
  )
}
