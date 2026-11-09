import {
  Button,
  Checkbox,
  Select,
  TextInput,
  UnstyledButton,
} from '@mantine/core'
import {
  Accessibility,
  Award,
  ChevronDown,
  ChevronUp,
  Info,
  UserRound,
} from 'lucide-react'
import { useState } from 'react'
import type { BrandedFare } from '#/features/flight-results'
import { PassengerBaggageAllowance } from './PassengerBaggageAllowance'
import type {
  BookingContactDetails,
  PassengerDetails,
} from '../model/booking.types'
import styles from '../styles/PassengerDetailsForm.module.scss'

interface PassengerDetailsFormProps {
  passengers: PassengerDetails[]
  fares: BrandedFare[]
  contact: BookingContactDetails
  onPassengerChange: <TField extends keyof PassengerDetails>(
    index: number,
    key: TField,
    value: PassengerDetails[TField],
  ) => void
  onContactChange: <TField extends keyof BookingContactDetails>(
    key: TField,
    value: BookingContactDetails[TField],
  ) => void
  specialRequests: string
  onSpecialRequestsChange: (value: string) => void
  onContinue: () => void
  onBack: () => void
}

const TITLE_OPTIONS = ['Mr', 'Ms', 'Mrs', 'Miss', 'Mx', 'Master']

export function PassengerDetailsForm({
  passengers,
  fares,
  contact,
  onPassengerChange,
  onContactChange,
  specialRequests,
  onSpecialRequestsChange,
  onContinue,
  onBack,
}: PassengerDetailsFormProps) {
  const [requestsOpen, setRequestsOpen] = useState(false)

  return (
    <form
      className={styles.form}
      onSubmit={(event) => {
        event.preventDefault()
        onContinue()
      }}
    >
      <div className={styles.memberNote}>
        <Award size={17} aria-hidden="true" />
        <span>
          Flying with us again? Add your airline membership number below to keep
          your details together.
        </span>
      </div>

      {passengers.map((passenger, index) => (
        <section
          key={passenger.id}
          className={styles.panel}
          aria-labelledby={`${passenger.id}-heading`}
        >
          <div className={styles.passengerHeading}>
            <span className={styles.passengerIcon}>
              <UserRound size={17} aria-hidden="true" />
            </span>
            <div className={styles.passengerTitle}>
              <h2 id={`${passenger.id}-heading`}>
                {passenger.firstName && passenger.lastName
                  ? `${passenger.firstName} ${passenger.lastName}`
                  : passenger.label}
              </h2>
              <span>{passenger.type}</span>
            </div>
          </div>

          <div className={styles.fields}>
            <Select
              label="Title"
              data={TITLE_OPTIONS}
              value={passenger.title}
              required
              onChange={(value) =>
                onPassengerChange(index, 'title', value ?? '')
              }
            />
            <TextInput
              label="First name"
              autoComplete="given-name"
              value={passenger.firstName}
              required
              onChange={(event) =>
                onPassengerChange(index, 'firstName', event.currentTarget.value)
              }
            />
            <TextInput
              label="Last name"
              autoComplete="family-name"
              value={passenger.lastName}
              required
              onChange={(event) =>
                onPassengerChange(index, 'lastName', event.currentTarget.value)
              }
            />
            <TextInput
              label="Date of birth"
              type="date"
              autoComplete="bday"
              value={passenger.dateOfBirth}
              required
              onChange={(event) =>
                onPassengerChange(
                  index,
                  'dateOfBirth',
                  event.currentTarget.value,
                )
              }
            />
            <TextInput
              label="Nationality"
              autoComplete="country-name"
              value={passenger.nationality}
              required
              onChange={(event) =>
                onPassengerChange(
                  index,
                  'nationality',
                  event.currentTarget.value,
                )
              }
            />
            <TextInput
              label="Polonia Club number (optional)"
              value={passenger.membershipNumber}
              onChange={(event) =>
                onPassengerChange(
                  index,
                  'membershipNumber',
                  event.currentTarget.value,
                )
              }
            />
          </div>
          <PassengerBaggageAllowance
            fare={fares[index]}
            isInfant={passenger.type === 'Infant'}
          />
        </section>
      ))}

      <div className={styles.infoNote} role="note">
        <Info size={16} aria-hidden="true" />
        <p>
          Check passport validity and entry requirements for your destination
          before travelling. Passenger names should match travel documents.
        </p>
      </div>

      <section className={styles.panel} aria-labelledby="contact-heading">
        <div className={styles.sectionHeading}>
          <h2 id="contact-heading">Where can we reach you?</h2>
          <p>
            These details stay in this demo and are not sent to an airline
            service.
          </p>
        </div>
        <div className={styles.contactFields}>
          <TextInput
            label="Email address"
            type="email"
            autoComplete="email"
            value={contact.email}
            required
            onChange={(event) =>
              onContactChange('email', event.currentTarget.value)
            }
          />
          <TextInput
            label="Mobile number"
            type="tel"
            autoComplete="tel"
            value={contact.mobile}
            required
            onChange={(event) =>
              onContactChange('mobile', event.currentTarget.value)
            }
          />
        </div>
        <Checkbox
          label="Send me flight updates by SMS as well as email."
          checked={contact.smsUpdates}
          onChange={(event) =>
            onContactChange('smsUpdates', event.currentTarget.checked)
          }
        />
      </section>

      <section className={styles.panel} aria-labelledby="requests-heading">
        <div className={styles.requestHeading}>
          <span className={styles.requestIcon}>
            <Accessibility size={20} aria-hidden="true" />
          </span>
          <div className={styles.sectionHeading}>
            <h2 id="requests-heading">Make your journey your own</h2>
            <p>Let us know about assistance or dietary preferences.</p>
          </div>
          <UnstyledButton
            className={styles.requestToggle}
            aria-expanded={requestsOpen}
            aria-controls="special-requests"
            onClick={() => setRequestsOpen((current) => !current)}
          >
            {requestsOpen ? 'Close requests' : 'Add requests'}
            {requestsOpen ? (
              <ChevronUp size={15} aria-hidden="true" />
            ) : (
              <ChevronDown size={15} aria-hidden="true" />
            )}
          </UnstyledButton>
        </div>
        {requestsOpen && (
          <TextInput
            id="special-requests"
            label="Special requests (optional)"
            placeholder="Accessibility or dietary requests"
            value={specialRequests}
            onChange={(event) =>
              onSpecialRequestsChange(event.currentTarget.value)
            }
          />
        )}
      </section>

      <div className={styles.actions}>
        <UnstyledButton className={styles.backButton} onClick={onBack}>
          ← Back to flights
        </UnstyledButton>
        <Button type="submit" className="polonia-action-button" size="md">
          Continue to payment
        </Button>
      </div>
    </form>
  )
}
