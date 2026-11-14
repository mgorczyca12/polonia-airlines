import type { FareSnapshot } from '#/shared/api'
import {
  BookingFormProvider,
  createBookingFormOptions,
  useBookingForm,
} from '../model/booking-form'
import type { BookingFormValues } from '../model/booking-form'
import { PassengerDetailsForm } from './PassengerDetailsForm'
import { ReviewStep } from './ReviewStep'

export type BookingFlowStep = 'Passengers' | 'Review'

interface BookingFlowProps {
  /** Offer fare snapshots ordered by passenger index. */
  fares: FareSnapshot[]
  step: BookingFlowStep
  canReview: boolean
  expired: boolean
  submitting: boolean
  onStepChange: (step: BookingFlowStep) => void
  onSubmit: (values: BookingFormValues) => void
}

/**
 * Passenger and contact entry followed by review. Draft details stay in memory
 * only; they are never written to the URL or browser storage.
 */
export function BookingFlow({
  fares,
  step,
  canReview,
  expired,
  submitting,
  onStepChange,
  onSubmit,
}: BookingFlowProps) {
  const form = useBookingForm(createBookingFormOptions(fares.length))

  return (
    <BookingFormProvider form={form}>
      {step === 'Passengers' ? (
        <PassengerDetailsForm
          fares={fares}
          canContinue={canReview}
          onContinue={() => onStepChange('Review')}
        />
      ) : (
        <ReviewStep
          submitting={submitting}
          expired={expired}
          onEdit={() => onStepChange('Passengers')}
          onSubmit={() => onSubmit(form.getValues())}
        />
      )}
    </BookingFormProvider>
  )
}
