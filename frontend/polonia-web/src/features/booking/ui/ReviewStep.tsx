import { Button } from '@mantine/core'
import { useBookingFormContext } from '../model/booking-form'
import styles from './ReviewStep.module.scss'

interface ReviewStepProps {
  submitting: boolean
  expired: boolean
  onEdit: () => void
  onSubmit: () => void
}

export function ReviewStep({
  submitting,
  expired,
  onEdit,
  onSubmit,
}: ReviewStepProps) {
  const { values } = useBookingFormContext()

  return (
    <section className={styles.panel} aria-labelledby="review-heading">
      <h2 id="review-heading" className={styles.title}>
        Review passengers and contact
      </h2>
      <dl className={styles.details}>
        {values.passengers.map((passenger, index) => (
          <div key={index}>
            <dt>Traveller {index + 1}</dt>
            <dd>
              {passenger.firstName} {passenger.lastName} · born{' '}
              {passenger.dateOfBirth}
            </dd>
          </div>
        ))}
        <div>
          <dt>Contact</dt>
          <dd>
            {values.contactName} · {values.contactEmail}
          </dd>
        </div>
      </dl>
      <p className={styles.note}>
        No charge will be made. Seats may be unavailable even when a fare is
        priced; your reservation is confirmed only after inventory is secured.
      </p>
      <div className={styles.actions}>
        <Button variant="default" disabled={submitting} onClick={onEdit}>
          Edit details
        </Button>
        <Button
          className="polonia-action-button"
          loading={submitting}
          disabled={expired}
          onClick={onSubmit}
        >
          Reserve without payment
        </Button>
      </div>
    </section>
  )
}
