import { UnstyledButton } from '@mantine/core'
import { ChevronDown, ChevronUp } from 'lucide-react'
import { useId, useState } from 'react'
import { formatUsd } from '#/shared/lib/formatting'
import type { FareBenefits, FlightCabin } from '../model/flight.types'
import styles from '../styles/FareBenefitsComparison.module.scss'

interface FareBenefitsComparisonProps {
  cabins: FlightCabin[]
  selectedFareIds: ReadonlySet<number>
}

const BENEFIT_ROWS: { label: string; key: keyof FareBenefits }[] = [
  { label: 'Checked baggage', key: 'checkedBaggage' },
  { label: 'Cabin baggage', key: 'cabinBaggage' },
  { label: 'Meals and entertainment', key: 'mealsAndEntertainment' },
  { label: 'Seat and lounge benefits', key: 'seatAndLoungeBenefits' },
  { label: 'Change fee', key: 'changeFee' },
  { label: 'Refund policy', key: 'refundPolicy' },
]

export function FareBenefitsComparison({
  cabins,
  selectedFareIds,
}: FareBenefitsComparisonProps) {
  const contentId = useId()
  const [expanded, setExpanded] = useState(false)
  const fares = cabins.flatMap((cabin) =>
    cabin.fares.map((fare) => ({ cabinName: cabin.className, fare })),
  )

  return (
    <section
      className={styles.comparison}
      aria-label="Fare benefits comparison"
    >
      <UnstyledButton
        className={styles.toggle}
        aria-expanded={expanded}
        aria-controls={contentId}
        onClick={() => setExpanded((current) => !current)}
      >
        <span className={styles.title}>Compare fare benefits</span>
        <span className={styles.toggleLabel}>
          {expanded ? 'Hide comparison' : 'Show comparison'}
          {expanded ? (
            <ChevronUp size={16} aria-hidden="true" />
          ) : (
            <ChevronDown size={16} aria-hidden="true" />
          )}
        </span>
      </UnstyledButton>

      <div id={contentId} hidden={!expanded}>
        <p className={styles.note}>
          Sample benefits only. Prices are per adult for this flight; final fare
          rules are not available in this demo.
        </p>
        <div
          className={styles.tableScroll}
          role="region"
          aria-label="Fare benefits table. Scroll horizontally to compare all fares."
          tabIndex={0}
        >
          <table
            className={styles.table}
            style={{ minWidth: `${(fares.length + 1) * 10}rem` }}
          >
            <thead>
              <tr>
                <th scope="col" className={styles.rowHeader}>
                  Benefit
                </th>
                {fares.map(({ cabinName, fare }) => {
                  const isSelected = selectedFareIds.has(fare.fareId)

                  return (
                    <th
                      key={fare.fareId}
                      scope="col"
                      className={`${styles.fareHeader} ${isSelected ? styles.selected : ''}`}
                    >
                      <span className={styles.cabinName}>{cabinName}</span>
                      <span className={styles.fareName}>{fare.fareName}</span>
                      <span className={styles.price}>
                        {formatUsd(fare.farePriceUsd)}
                      </span>
                      <span className={styles.priceNote}>
                        per adult · this flight
                      </span>
                      {isSelected && (
                        <span className={styles.selectedLabel}>Selected</span>
                      )}
                    </th>
                  )
                })}
              </tr>
            </thead>
            <tbody>
              {BENEFIT_ROWS.map(({ label, key }) => (
                <tr key={key}>
                  <th scope="row" className={styles.rowHeader}>
                    {label}
                  </th>
                  {fares.map(({ fare }) => (
                    <td
                      key={fare.fareId}
                      className={`${styles.value} ${selectedFareIds.has(fare.fareId) ? styles.selected : ''}`}
                    >
                      {fare.benefits[key]}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  )
}
