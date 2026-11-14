import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { offerKeys } from '#/entities/offer'
import { createOffer } from '#/shared/api'

interface OfferRequest {
  flightId: number
  productIds: string[]
}

/** Creates the authoritative priced offer, then opens it for passenger details. */
export function useContinueToBooking() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: ({ flightId, productIds }: OfferRequest) =>
      createOffer(flightId, productIds),
    onSuccess: async (offer) => {
      queryClient.setQueryData(offerKeys.detail(offer.id), offer)
      await navigate({ to: '/booking', search: { offer: offer.id } })
    },
  })
}
