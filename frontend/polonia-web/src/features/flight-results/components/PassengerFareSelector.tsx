import { UnstyledButton } from '@mantine/core'
import { CircleCheck } from 'lucide-react'
import type { SeatedPassenger } from '#/features/flight-search'
import styles from '../styles/PassengerFareSelector.module.scss'

interface PassengerFareSelectorProps {
  passengers: SeatedPassenger[]
  activePassengerId: string
  fareNames: Record<string, string | undefined>
  onSelect: (passengerId: string) => void
}

export function PassengerFareSelector({
  passengers,
  activePassengerId,
  fareNames,
  onSelect,
}: PassengerFareSelectorProps) {
  return (
    <div
      className={styles.selector}
      role="group"
      aria-label="Choose a fare for"
    >
      {passengers.map((passenger) => {
        const isActive = passenger.id === activePassengerId
        const fareName = fareNames[passenger.id]

        return (
          <UnstyledButton
            key={passenger.id}
            className={`${styles.passenger} ${isActive ? styles.active : ''}`}
            aria-pressed={isActive}
            onClick={() => onSelect(passenger.id)}
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
