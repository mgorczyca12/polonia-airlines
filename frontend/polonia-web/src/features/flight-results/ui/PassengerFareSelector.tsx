import { UnstyledButton } from '@mantine/core'
import { CircleCheck } from 'lucide-react'
import type { SeatedPassenger } from '#/features/flight-search'
import styles from './PassengerFareSelector.module.scss'

interface PassengerFareSelectorProps {
  passengers: SeatedPassenger[]
  activeIndex: number
  /** Chosen fare name per passenger index. */
  fareNames: Array<string | undefined>
  onSelect: (passengerIndex: number) => void
}

export function PassengerFareSelector({
  passengers,
  activeIndex,
  fareNames,
  onSelect,
}: PassengerFareSelectorProps) {
  return (
    <div
      className={styles.selector}
      role="group"
      aria-label="Choose a fare for"
    >
      {passengers.map((passenger, index) => {
        const isActive = index === activeIndex
        const fareName = fareNames[index]

        return (
          <UnstyledButton
            key={passenger.id}
            className={`${styles.passenger} ${isActive ? styles.active : ''}`}
            aria-pressed={isActive}
            onClick={() => onSelect(index)}
          >
            <span className={styles.name}>
              {passenger.label}
              {fareName && <CircleCheck size={14} aria-hidden="true" />}
            </span>
            <span className={styles.fare}>{fareName ?? 'Choose a fare'}</span>
          </UnstyledButton>
        )
      })}
    </div>
  )
}
