import { UnstyledButton } from '@mantine/core'
import type { CabinFares } from '#/entities/fare'
import { formatMoney } from '#/shared/lib/formatting'
import styles from './CabinSelector.module.scss'

interface CabinSelectorProps {
  cabins: CabinFares[]
  selectedCabin: string
  onSelect: (cabinClass: string) => void
}

export function CabinSelector({
  cabins,
  selectedCabin,
  onSelect,
}: CabinSelectorProps) {
  return (
    <div className={styles.selector} role="group" aria-label="Cabin">
      {cabins.map((cabin) => {
        const isSelected = cabin.cabinClass === selectedCabin

        return (
          <UnstyledButton
            key={cabin.cabinClass}
            className={`${styles.cabin} ${isSelected ? styles.selected : ''}`}
            aria-pressed={isSelected}
            onClick={() => onSelect(cabin.cabinClass)}
          >
            <span className={styles.name}>{cabin.cabinClass}</span>
            <span className={styles.price}>
              from {formatMoney(cabin.lowestAmount, cabin.fares[0].currency)}
            </span>
          </UnstyledButton>
        )
      })}
    </div>
  )
}
