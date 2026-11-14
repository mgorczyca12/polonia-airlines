import { Button, Checkbox, TextInput, UnstyledButton } from '@mantine/core'
import { Apple, CreditCard, LockKeyhole, Wallet } from 'lucide-react'
import { formatUsd } from '#/shared/lib/formatting'
import type { PaymentMethod } from '../model/booking.types'
import styles from './PaymentDetailsForm.module.scss'

interface PaymentDetailsFormProps {
  total: number
  method: PaymentMethod
  termsAccepted: boolean
  onMethodChange: (method: PaymentMethod) => void
  onTermsChange: (accepted: boolean) => void
  onContinue: () => void
  onBack: () => void
}

const PAYMENT_OPTIONS: {
  value: PaymentMethod
  label: string
  icon: typeof CreditCard
}[] = [
  { value: 'card', label: 'Credit or debit card', icon: CreditCard },
  { value: 'wallet', label: 'Digital wallet', icon: Wallet },
  { value: 'mobileWallet', label: 'Mobile pay', icon: Apple },
]

export function PaymentDetailsForm({
  total,
  method,
  termsAccepted,
  onMethodChange,
  onTermsChange,
  onContinue,
  onBack,
}: PaymentDetailsFormProps) {
  return (
    <form
      className={styles.form}
      onSubmit={(event) => {
        event.preventDefault()
        onContinue()
      }}
    >
      <div className={styles.contactCard}>
        <div className={styles.contactIcon}>
          <CreditCard size={17} aria-hidden="true" />
        </div>
        <div>
          <p className={styles.contactTitle}>Passenger details are ready</p>
          <p className={styles.contactNote}>
            Review your information before continuing to the demo payment step.
          </p>
        </div>
        <button className={styles.editButton} type="button" onClick={onBack}>
          Edit details
        </button>
      </div>

      <section className={styles.panel} aria-labelledby="payment-heading">
        <div className={styles.panelHeading}>
          <h2 id="payment-heading">How would you like to pay?</h2>
          <span>
            <LockKeyhole size={14} aria-hidden="true" />
            Demo only
          </span>
        </div>

        <div
          className={styles.methods}
          role="group"
          aria-label="Payment method"
        >
          {PAYMENT_OPTIONS.map(({ value, label, icon: Icon }) => (
            <UnstyledButton
              key={value}
              className={`${styles.method} ${method === value ? styles.methodSelected : ''}`}
              aria-pressed={method === value}
              onClick={() => onMethodChange(value)}
            >
              <Icon size={16} aria-hidden="true" />
              {label}
            </UnstyledButton>
          ))}
        </div>

        {method === 'card' ? (
          <>
            <div className={styles.acceptedCards}>
              <span>Accepted for this demo</span>
              <strong>VISA</strong>
              <strong>Mastercard</strong>
              <strong>AMEX</strong>
            </div>
            <div
              className={styles.cardFields}
              aria-label="Example card details"
            >
              <TextInput
                label="Cardholder name"
                value="Example cardholder"
                readOnly
                disabled
              />
              <TextInput
                label="Card number"
                value="••••  ••••  ••••  4242"
                readOnly
                disabled
              />
              <TextInput
                label="Expiry date"
                value="MM / YY"
                readOnly
                disabled
              />
              <TextInput label="Security code" value="•••" readOnly disabled />
            </div>
            <div className={styles.billing}>
              <h3>Billing address</h3>
              <p>
                Billing details will be requested when a secure payment provider
                is connected.
              </p>
            </div>
          </>
        ) : (
          <div className={styles.providerNote} role="status">
            {method === 'wallet' ? 'Digital wallet' : 'Mobile pay'} is shown as
            a design preview. No wallet account will be opened or charged.
          </div>
        )}

        <div className={styles.demoNotice} role="note">
          <LockKeyhole size={16} aria-hidden="true" />
          <p>
            This is a demo. No payment information is collected, transmitted, or
            charged. The example card details above are read-only.
          </p>
        </div>
      </section>

      <Checkbox
        className={styles.terms}
        required
        checked={termsAccepted}
        label={`I have reviewed the passenger details and fare selection. Demo total: ${formatUsd(total)}.`}
        onChange={(event) => onTermsChange(event.currentTarget.checked)}
      />

      <div className={styles.actions}>
        <UnstyledButton className={styles.backButton} onClick={onBack}>
          ← Back to passengers
        </UnstyledButton>
        <Button type="submit" className="polonia-action-button" size="md">
          Confirm demo booking · {formatUsd(total)}
        </Button>
      </div>
    </form>
  )
}
