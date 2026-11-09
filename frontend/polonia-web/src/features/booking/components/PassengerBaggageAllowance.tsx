import { Luggage } from 'lucide-react'
import type { BrandedFare } from '#/features/flight-results'
import styles from '../styles/PassengerBaggageAllowance.module.scss'

interface PassengerBaggageAllowanceProps {
  fare?: BrandedFare
  isInfant: boolean
}

export function PassengerBaggageAllowance({
  fare,
  isInfant,
}: PassengerBaggageAllowanceProps) {
  const cabinBaggage = isInfant
    ? 'Not specified for infants'
    : (fare?.benefits.cabinBaggage ?? 'Not available')
  const checkedBaggage = isInfant
    ? 'Not specified for infants'
    : (fare?.benefits.checkedBaggage ?? 'Not available')

  return (
    <div className={styles.allowance}>
      <h3>
        <Luggage size={15} aria-hidden="true" />
        Baggage allowance
      </h3>
      <dl>
        <div>
          <dt>Cabin baggage</dt>
          <dd>{cabinBaggage}</dd>
        </div>
        <div>
          <dt>Checked baggage</dt>
          <dd>{checkedBaggage}</dd>
        </div>
      </dl>
      <p>
        {isInfant
          ? 'Confirm infant baggage rules with the airline. This demo does not include them.'
          : 'Illustrative fare details only; baggage weight and dimensions are not specified.'}
      </p>
    </div>
  )
}
