import { Button } from '@mantine/core'
import { Check, CircleCheck } from 'lucide-react'
import { formatUsd } from '#/shared/lib/formatting'
import type { FlightCabin } from '../model/flight.types'
import styles from '../styles/FareOptions.module.scss'

interface FareOptionsProps {
  cabin: FlightCabin
  selectedFareId: number | null
  onSelect: (fareId: number) => void
}

export function FareOptions({ cabin, selectedFareId, onSelect }: FareOptionsProps) {
  return (
    <div className={styles.options} role="group" aria-label={`${cabin.className} fares`}>
      {cabin.fares.map((fare) => {
        const isSelected = fare.fareId === selectedFareId

        return (
          <article
            key={fare.fareId}
            className={`${styles.fare} ${isSelected ? styles.selected : ''}`}
          >
            <div className={styles.heading}>
              <h4 className={styles.name}>{fare.fareName}</h4>
              {isSelected && (
                <CircleCheck className={styles.selectedIcon} size={19} aria-hidden="true" />
              )}
            </div>

            <div>
              <p className={styles.price}>{formatUsd(fare.farePriceUsd)}</p>
              <p className={styles.priceNote}>per adult · this flight</p>
            </div>

            <span className={styles.divider} aria-hidden="true" />

            <ul className={styles.perks}>
              {fare.perks.map((perk) => (
                <li key={perk} className={styles.perk}>
                  <Check className={styles.perkIcon} size={15} aria-hidden="true" />
                  <span>{perk}</span>
                </li>
              ))}
            </ul>

            <Button
              className={styles.selectButton}
              variant={isSelected ? 'filled' : 'default'}
              fullWidth
              aria-pressed={isSelected}
              onClick={() => onSelect(fare.fareId)}
            >
              {isSelected ? 'Selected' : 'Select fare'}
            </Button>
          </article>
        )
      })}
    </div>
  )
}
