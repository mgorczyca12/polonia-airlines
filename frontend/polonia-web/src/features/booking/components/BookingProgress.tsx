import { Check } from 'lucide-react'
import styles from '../styles/BookingProgress.module.scss'

const STEPS = ['Flights', 'Passengers', 'Payment', 'Confirmation'] as const

export type BookingStep = (typeof STEPS)[number]

interface BookingProgressProps {
  currentStep: BookingStep
}

export function BookingProgress({ currentStep }: BookingProgressProps) {
  const currentIndex = STEPS.indexOf(currentStep)

  return (
    <nav className={styles.progress} aria-label="Booking progress">
      <ol className={styles.steps}>
        {STEPS.map((step, index) => {
          const state =
            index < currentIndex ? 'complete' : index === currentIndex ? 'current' : 'upcoming'

          return (
            <li
              key={step}
              className={`${styles.step} ${styles[state]}`}
              aria-current={state === 'current' ? 'step' : undefined}
            >
              <span className={styles.marker} aria-hidden="true">
                {state === 'complete' ? <Check size={13} strokeWidth={2.5} /> : index + 1}
              </span>
              <span className={styles.label}>
                {step}
                {state === 'complete' && (
                  <span className="polonia-visually-hidden"> (completed)</span>
                )}
              </span>
              {index < STEPS.length - 1 && (
                <span className={styles.connector} aria-hidden="true" />
              )}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
