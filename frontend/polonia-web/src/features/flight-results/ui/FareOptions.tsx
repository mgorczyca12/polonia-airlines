import { Button } from '@mantine/core'
import { CircleCheck, Luggage } from 'lucide-react'
import { describeCheckedBaggage, describeExtraBaggage } from '#/entities/fare'
import type { CabinFares } from '#/entities/fare'
import { formatMoney } from '#/shared/lib/formatting'
import styles from './FareOptions.module.scss'

interface FareOptionsProps {
  cabin: CabinFares
  selectedProductId: string | undefined
  selectLabel: string
  /** Other passengers already assigned to each fare, by product ID. */
  assignedLabels: Record<string, string[]>
  reserveAssignmentRow: boolean
  disabled: boolean
  onSelect: (productId: string) => void
}

export function FareOptions({
  cabin,
  selectedProductId,
  selectLabel,
  assignedLabels,
  reserveAssignmentRow,
  disabled,
  onSelect,
}: FareOptionsProps) {
  return (
    <div
      className={styles.options}
      role="group"
      aria-label={`${cabin.cabinClass} fares`}
    >
      {cabin.fares.map((fare) => {
        const isSelected = fare.productId === selectedProductId
        const assignedTo = assignedLabels[fare.productId] ?? []

        return (
          <article
            key={fare.productId}
            className={`${styles.fare} ${isSelected ? styles.selected : ''}`}
          >
            <div className={styles.heading}>
              <h4 className={styles.name}>{fare.brand}</h4>
              {isSelected && (
                <CircleCheck
                  className={styles.selectedIcon}
                  size={19}
                  aria-hidden="true"
                />
              )}
            </div>

            <div>
              <p className={styles.price}>
                {formatMoney(fare.amount, fare.currency)}
              </p>
              <p className={styles.priceNote}>per traveller · this flight</p>
              {(assignedTo.length > 0 || reserveAssignmentRow) && (
                <p className={styles.assigned}>
                  {assignedTo.length > 0
                    ? `Chosen by ${assignedTo.join(', ')}`
                    : '\u00A0'}
                </p>
              )}
            </div>

            <span className={styles.divider} aria-hidden="true" />

            <ul className={styles.allowances}>
              <li className={styles.allowance}>
                <Luggage
                  className={styles.allowanceIcon}
                  size={15}
                  aria-hidden="true"
                />
                <span>{describeCheckedBaggage(fare.baggage)}</span>
              </li>
              <li className={styles.allowance}>
                <Luggage
                  className={styles.allowanceIcon}
                  size={15}
                  aria-hidden="true"
                />
                <span>{describeExtraBaggage(fare.baggage, fare.currency)}</span>
              </li>
            </ul>

            <Button
              className={styles.selectButton}
              variant={isSelected ? 'filled' : 'default'}
              fullWidth
              disabled={disabled}
              aria-pressed={isSelected}
              onClick={() => onSelect(fare.productId)}
            >
              {isSelected ? 'Selected' : selectLabel}
            </Button>
          </article>
        )
      })}
    </div>
  )
}
