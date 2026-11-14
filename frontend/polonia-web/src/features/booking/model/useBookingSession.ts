import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { flightQuery } from '#/entities/flight'
import { offerKeys, offerQuery } from '#/entities/offer'
import {
  reservationByRequestQuery,
  reservationKeys,
} from '#/entities/reservation'
import { createReservation, recoverReservation } from '#/shared/api'
import type { ReservationResponse } from '#/shared/api'
import type { BookingFormValues } from './booking-form'

export class NoCommittedReservationError extends Error {
  constructor() {
    super(
      'No committed reservation was found yet. You may retry with the original passenger and contact details; do not start another booking if a submission is still in flight.',
    )
  }
}

/**
 * Loads a booking for an offer. A reservation already committed for the offer
 * always wins over the offer, so a lost response never leads to a second booking.
 */
export function useBookingSession(offerId: string) {
  const queryClient = useQueryClient()
  const [submittedId, setSubmittedId] = useState<number | null>(null)
  const recovery = useQuery(reservationByRequestQuery(offerId))
  const reservationId = submittedId ?? recovery.data?.id ?? null
  const offer = useQuery({
    ...offerQuery(offerId),
    enabled: recovery.isSuccess && reservationId === null,
  })

  const adoptReservation = (reservation: ReservationResponse) => {
    queryClient.setQueryData(
      reservationKeys.detail(reservation.id),
      reservation,
    )
    setSubmittedId(reservation.id)
  }

  const submit = useMutation({
    mutationFn: async (details: BookingFormValues) =>
      (await recoverReservation(offerId)) ??
      (await createReservation({ offerId, ...details })),
    onMutate: (): void => {
      checkSubmission.reset()
    },
    onSuccess: adoptReservation,
  })

  const checkSubmission = useMutation({
    mutationFn: async () => {
      const existing = await recoverReservation(offerId)

      if (!existing) {
        throw new NoCommittedReservationError()
      }

      return existing
    },
    onMutate: (): void => {
      submit.reset()
    },
    onSuccess: adoptReservation,
  })

  const reload = () => {
    submit.reset()
    checkSubmission.reset()
    void queryClient.invalidateQueries({
      queryKey: reservationByRequestQuery(offerId).queryKey,
    })
    void queryClient.invalidateQueries({ queryKey: offerKeys.detail(offerId) })
  }

  return {
    reservationId,
    recovered: recovery.data
      ? { reservation: recovery.data, updatedAt: recovery.dataUpdatedAt }
      : undefined,
    offer: offer.data,
    isLoading:
      recovery.isPending || (reservationId === null && offer.isPending),
    loadError: recovery.error ?? offer.error,
    submit,
    checkSubmission,
    reload,
  }
}

/** Itinerary details for a booking; failures do not affect reservation state. */
export function useBookingFlight(flightId: string | undefined) {
  return useQuery({
    ...flightQuery(flightId ?? ''),
    enabled: flightId !== undefined,
  })
}
