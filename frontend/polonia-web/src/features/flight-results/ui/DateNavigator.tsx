import { UnstyledButton } from '@mantine/core'
import {
  addDaysToDateKey,
  formatDayMonth,
  formatLongDate,
  formatWeekday,
} from '#/shared/lib/formatting'
import styles from './DateNavigator.module.scss'

const DAYS_EACH_SIDE = 2

interface DateNavigatorProps {
  selectedDateKey: string
  /** Earliest searchable UTC date, as `YYYY-MM-DD`. */
  minDateKey: string
  onSelect: (dateKey: string) => void
}

/** Moves the search to a nearby departure date. Prices are not shown for other dates. */
export function DateNavigator({
  selectedDateKey,
  minDateKey,
  onSelect,
}: DateNavigatorProps) {
  const dates = Array.from({ length: DAYS_EACH_SIDE * 2 + 1 }, (_, index) =>
    addDaysToDateKey(selectedDateKey, index - DAYS_EACH_SIDE),
  )

  return (
    <div
      className={styles.strip}
      role="group"
      aria-label="Search another departure date"
    >
      {dates.map((dateKey) => {
        const isSelected = dateKey === selectedDateKey

        return (
          <UnstyledButton
            key={dateKey}
            className={`${styles.date} ${isSelected ? styles.selected : ''}`}
            aria-pressed={isSelected}
            aria-label={formatLongDate(dateKey)}
            disabled={dateKey < minDateKey}
            onClick={() => onSelect(dateKey)}
          >
            <span className={styles.weekday}>{formatWeekday(dateKey)}</span>
            <span className={styles.day}>{formatDayMonth(dateKey)}</span>
          </UnstyledButton>
        )
      })}
    </div>
  )
}
