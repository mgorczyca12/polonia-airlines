import type { BaggageSnapshot, FlightFareResponse } from '#/shared/api'
import { formatMoney } from '#/shared/lib/formatting'

/** A priced fare product for one flight. `productId` identifies the selection. */
export interface Fare {
  productId: string
  flightId: string
  cabinClass: string
  brand: string
  amount: number
  currency: string
  baggage: BaggageSnapshot
}

export interface CabinFares {
  cabinClass: string
  fares: Fare[]
  lowestAmount: number
}

export function toFare(fare: FlightFareResponse): Fare {
  return {
    productId: fare.productId,
    flightId: fare.flightId,
    cabinClass: fare.cabinClass,
    brand: fare.brand,
    amount: fare.amount,
    currency: fare.currency,
    baggage: fare.baggage,
  }
}

/** Fares priced for one flight. Flight IDs are compared in their canonical string form. */
export function faresForFlight(fares: Fare[], flightId: number): Fare[] {
  const id = String(flightId)
  return fares.filter((fare) => fare.flightId === id)
}

export function lowestAmount(fares: Fare[]): number | null {
  return fares.length > 0 ? Math.min(...fares.map((fare) => fare.amount)) : null
}

/** Groups fares by cabin, cheapest cabin first and cheapest fare first within a cabin. */
export function groupFaresByCabin(fares: Fare[]): CabinFares[] {
  const cabins = new Map<string, Fare[]>()

  for (const fare of fares) {
    cabins.set(fare.cabinClass, [...(cabins.get(fare.cabinClass) ?? []), fare])
  }

  return [...cabins.entries()]
    .map(([cabinClass, cabinFares]) => {
      const sorted = [...cabinFares].sort(
        (first, second) =>
          first.amount - second.amount || first.brand.localeCompare(second.brand),
      )

      return { cabinClass, fares: sorted, lowestAmount: sorted[0].amount }
    })
    .sort(
      (first, second) =>
        first.lowestAmount - second.lowestAmount ||
        first.cabinClass.localeCompare(second.cabinClass),
    )
}

export function describeCheckedBaggage(baggage: BaggageSnapshot): string {
  if (baggage.includedPieces === 0) {
    return 'No checked bags included'
  }

  const pieces = `${baggage.includedPieces} checked ${baggage.includedPieces === 1 ? 'bag' : 'bags'}`
  return `${pieces}, up to ${baggage.maxWeightPerPieceKg} kg each`
}

export function describeExtraBaggage(
  baggage: BaggageSnapshot,
  currency: string,
): string {
  if (baggage.maxExtraPieces === 0) {
    return 'No extra bags'
  }

  return `Up to ${baggage.maxExtraPieces} extra, ${formatMoney(baggage.extraPiecePrice, currency)} each`
}
