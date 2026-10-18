import { PlaneLanding } from 'lucide-react'
import styles from '../styles/ReturnFlightNotice.module.scss'

interface ReturnFlightNoticeProps {
  routeLabel: string
  returnDateLabel?: string
}

export function ReturnFlightNotice({ routeLabel, returnDateLabel }: ReturnFlightNoticeProps) {
  return (
    <section className={styles.notice} aria-labelledby="return-heading">
      <div className={styles.heading}>
        <h2 id="return-heading" className={styles.title}>
          <PlaneLanding className={styles.icon} size={20} aria-hidden="true" />
          Return · {routeLabel}
        </h2>
        {returnDateLabel && <p className={styles.date}>{returnDateLabel}</p>}
      </div>
      <p className={styles.text}>
        Return flights aren't available in the current schedule. You can continue with
        your outbound flight.
      </p>
    </section>
  )
}
