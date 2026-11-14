import { UnstyledButton } from '@mantine/core'
import { ChevronDown, ChevronUp } from 'lucide-react'
import { useId, useState } from 'react'
import { describeCheckedBaggage, describeExtraBaggage } from '#/entities/fare'
import type { CabinFares, Fare } from '#/entities/fare'
import { formatMoney } from '#/shared/lib/formatting'
import styles from './FareBaggageComparison.module.scss'

interface FareBaggageComparisonProps {
  cabins: CabinFares[]
  selectedProductIds: ReadonlySet<string>
}

const BAGGAGE_ROWS: Array<{ label: string; describe: (fare: Fare) => string }> =
  [
    {
      label: 'Checked baggage',
      describe: (fare) => describeCheckedBaggage(fare.baggage),
    },
    {
      label: 'Extra bags',
      describe: (fare) => describeExtraBaggage(fare.baggage, fare.currency),
    },
  ]

export function FareBaggageComparison({
  cabins,
  selectedProductIds,
}: FareBaggageComparisonProps) {
  const contentId = useId()
  const [expanded, setExpanded] = useState(false)
  const fares = cabins.flatMap((cabin) => cabin.fares)

  return (
    <section className={styles.comparison} aria-label="Fare baggage comparison">
      <UnstyledButton
        className={styles.toggle}
        aria-expanded={expanded}
        aria-controls={contentId}
        onClick={() => setExpanded((current) => !current)}
      >
        <span className={styles.title}>Compare baggage allowances</span>
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
        <div
          className={styles.tableScroll}
          role="region"
          aria-label="Baggage table. Scroll horizontally to compare all fares."
          tabIndex={0}
        >
          <table className={styles.table}>
            <thead>
              <tr>
                <th scope="col" className={styles.rowHeader}>
                  Allowance
                </th>
                {fares.map((fare) => (
                  <th
                    key={fare.productId}
                    scope="col"
                    className={`${styles.fareHeader} ${selectedProductIds.has(fare.productId) ? styles.selected : ''}`}
                  >
                    <span className={styles.cabinName}>{fare.cabinClass}</span>
                    <span className={styles.fareName}>{fare.brand}</span>
                    <span className={styles.price}>
                      {formatMoney(fare.amount, fare.currency)}
                    </span>
                    {selectedProductIds.has(fare.productId) && (
                      <span className={styles.selectedLabel}>Selected</span>
                    )}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {BAGGAGE_ROWS.map(({ label, describe }) => (
                <tr key={label}>
                  <th scope="row" className={styles.rowHeader}>
                    {label}
                  </th>
                  {fares.map((fare) => (
                    <td
                      key={fare.productId}
                      className={`${styles.value} ${selectedProductIds.has(fare.productId) ? styles.selected : ''}`}
                    >
                      {describe(fare)}
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
