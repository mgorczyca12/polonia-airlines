import { Button, TextInput } from '@mantine/core'
import { Info, UserRound } from 'lucide-react'
import type { FareSnapshot } from '#/shared/api'
import {
  CONTACT_NAME_MAX_LENGTH,
  EMAIL_MAX_LENGTH,
  latestBirthDateKey,
  NAME_MAX_LENGTH,
  useBookingFormContext,
} from '../model/booking-form'
import { PassengerBaggageAllowance } from './PassengerBaggageAllowance'
import styles from './PassengerDetailsForm.module.scss'

interface PassengerDetailsFormProps {
  /** Offer fare snapshots ordered by passenger index. */
  fares: FareSnapshot[]
  canContinue: boolean
  onContinue: () => void
}

export function PassengerDetailsForm({
  fares,
  canContinue,
  onContinue,
}: PassengerDetailsFormProps) {
  const form = useBookingFormContext()
  const maxBirthDate = latestBirthDateKey()

  return (
    <form
      className={styles.form}
      noValidate
      onSubmit={form.onSubmit(onContinue)}
    >
      {form.values.passengers.map((passenger, index) => {
        const fare = fares.at(index)
        const headingId = `traveller-${index}-heading`
        const name = `${passenger.firstName} ${passenger.lastName}`.trim()

        return (
          <section
            key={index}
            className={styles.panel}
            aria-labelledby={headingId}
          >
            <div className={styles.passengerHeading}>
              <span className={styles.passengerIcon}>
                <UserRound size={17} aria-hidden="true" />
              </span>
              <div className={styles.passengerTitle}>
                <h2 id={headingId}>{name || `Traveller ${index + 1}`}</h2>
                {fare && (
                  <span>
                    {fare.cabinClass} · {fare.brand}
                  </span>
                )}
              </div>
            </div>

            <div className={styles.fields}>
              <TextInput
                label="First name"
                autoComplete="given-name"
                required
                maxLength={NAME_MAX_LENGTH}
                {...form.getInputProps(`passengers.${index}.firstName`)}
              />
              <TextInput
                label="Last name"
                autoComplete="family-name"
                required
                maxLength={NAME_MAX_LENGTH}
                {...form.getInputProps(`passengers.${index}.lastName`)}
              />
              <TextInput
                label="Date of birth"
                type="date"
                autoComplete="bday"
                required
                max={maxBirthDate}
                {...form.getInputProps(`passengers.${index}.dateOfBirth`)}
              />
            </div>
            {fare && (
              <PassengerBaggageAllowance
                baggage={fare.baggage}
                currency={fare.currency}
              />
            )}
          </section>
        )
      })}

      <div className={styles.infoNote} role="note">
        <Info size={16} aria-hidden="true" />
        <p>
          All travellers must be aged two or older. Names and dates of birth
          must match travel documents.
        </p>
      </div>

      <section className={styles.panel} aria-labelledby="contact-heading">
        <div className={styles.sectionHeading}>
          <h2 id="contact-heading">Where can we reach you?</h2>
          <p>We use these details for this reservation only.</p>
        </div>
        <div className={styles.contactFields}>
          <TextInput
            label="Contact name"
            autoComplete="name"
            required
            maxLength={CONTACT_NAME_MAX_LENGTH}
            {...form.getInputProps('contactName')}
          />
          <TextInput
            label="Contact email"
            type="email"
            autoComplete="email"
            required
            maxLength={EMAIL_MAX_LENGTH}
            {...form.getInputProps('contactEmail')}
          />
        </div>
      </section>

      <div className={styles.actions}>
        <Button
          type="submit"
          className="polonia-action-button"
          size="md"
          disabled={!canContinue}
        >
          Review reservation
        </Button>
      </div>
    </form>
  )
}
