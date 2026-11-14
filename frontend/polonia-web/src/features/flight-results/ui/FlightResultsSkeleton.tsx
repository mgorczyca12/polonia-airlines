import { Skeleton } from '@mantine/core'
import { LoadingRegion } from '#/shared/ui/LoadingRegion'
import styles from './FlightResultCard.module.scss'

const PLACEHOLDER_CARDS = 3

/** Placeholder shaped like collapsed result cards for the first results load. */
export function FlightResultsSkeleton() {
  return (
    <LoadingRegion label="Loading flights and fares">
      <div className={styles.skeletonList}>
        {Array.from({ length: PLACEHOLDER_CARDS }, (_, index) => (
          <div key={index} className={`${styles.card} ${styles.body}`}>
            <div className={styles.summary}>
              <div className={styles.schedule}>
                <Skeleton height={28} width="70%" radius="sm" />
                <Skeleton height={12} width="45%" mt="sm" radius="sm" />
              </div>
              <Skeleton height={28} width={96} radius="sm" />
            </div>
          </div>
        ))}
      </div>
    </LoadingRegion>
  )
}
