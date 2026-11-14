import { Luggage } from 'lucide-react'
import { describeCheckedBaggage, describeExtraBaggage } from '#/entities/fare'
import type { BaggageSnapshot } from '#/shared/api'
import styles from './PassengerBaggageAllowance.module.scss'

interface PassengerBaggageAllowanceProps {
  baggage: BaggageSnapshot
  currency: string
}

export function PassengerBaggageAllowance({
  baggage,
  currency,
}: PassengerBaggageAllowanceProps) {
  return (
    <div className={styles.allowance}>
      <h3>
        <Luggage size={15} aria-hidden="true" />
        Baggage allowance
      </h3>
      <dl>
        <div>
          <dt>Checked baggage</dt>
          <dd>{describeCheckedBaggage(baggage)}</dd>
        </div>
        <div>
          <dt>Extra bags</dt>
          <dd>{describeExtraBaggage(baggage, currency)}</dd>
        </div>
      </dl>
    </div>
  )
}
