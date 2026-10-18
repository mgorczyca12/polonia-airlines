import { UnstyledButton } from '@mantine/core'
import { formatDayMonth, formatLongDate, formatUsd } from '#/shared/lib/formatting'
import styles from '../styles/DateFareStrip.module.scss'

export interface DateFare {
  dateKey: string
  lowestFare: number | null
}

interface DateFareStripProps {
  dates: DateFare[]
  selectedDateKey: string
  onSelect: (dateKey: string) => void
}

export function DateFareStrip({ dates, selectedDateKey, onSelect }: DateFareStripProps) {
  return (
    <div className={styles.strip} role="group" aria-label="Lowest fares by departure date">
      {dates.map(({ dateKey, lowestFare }) => {
        const isSelected = dateKey === selectedDateKey
        const isAvailable = lowestFare !== null
        const accessibleLabel = isAvailable
          ? `${formatLongDate(dateKey)}, from ${formatUsd(lowestFare)}`
          : `${formatLongDate(dateKey)}, no flights`

        return (
          <UnstyledButton
            key={dateKey}
            className={`${styles.date} ${isSelected ? styles.selected : ''}`}
            aria-pressed={isSelected}
            aria-label={accessibleLabel}
            disabled={!isAvailable}
            onClick={() => onSelect(dateKey)}
          >
            <span className={styles.day}>{formatDayMonth(dateKey)}</span>
            <span className={styles.fare}>
              {isAvailable ? formatUsd(lowestFare) : 'No flights'}
            </span>
          </UnstyledButton>
        )
      })}
    </div>
  )
}
