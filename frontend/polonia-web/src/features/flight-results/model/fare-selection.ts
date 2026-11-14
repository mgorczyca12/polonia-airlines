import { useState } from 'react'

/** One flight with a fare product chosen per seated passenger, by passenger index. */
export interface FareSelection {
  flightId: number
  productIds: Array<string | undefined>
}

/**
 * Assigns a fare to some passengers. Choosing a different flight starts a new
 * selection, because a booking covers exactly one flight.
 */
export function assignFare(
  selection: FareSelection | null,
  flightId: number,
  passengerIndexes: number[],
  productId: string,
  passengerCount: number,
): FareSelection {
  const productIds =
    selection?.flightId === flightId
      ? [...selection.productIds]
      : Array.from<string | undefined>({ length: passengerCount })

  for (const index of passengerIndexes) {
    productIds[index] = productId
  }

  return { flightId, productIds }
}

export function selectedProductIds(
  selection: FareSelection | null,
  passengerCount: number,
): string[] | null {
  if (!selection || selection.productIds.length !== passengerCount) {
    return null
  }

  const productIds = selection.productIds.filter(
    (productId): productId is string => Boolean(productId),
  )
  return productIds.length === passengerCount ? productIds : null
}

/**
 * Fare selection scoped to one set of search results. Changing `resultsKey`
 * (for example the page) discards the previous selection without an effect.
 */
export function useFareSelection(resultsKey: string, passengerCount: number) {
  const [state, setState] = useState<{
    resultsKey: string
    selection: FareSelection | null
  }>({ resultsKey, selection: null })
  const selection = state.resultsKey === resultsKey ? state.selection : null

  const select = (
    flightId: number,
    passengerIndexes: number[],
    productId: string,
  ) => {
    setState({
      resultsKey,
      selection: assignFare(
        selection,
        flightId,
        passengerIndexes,
        productId,
        passengerCount,
      ),
    })
  }

  return {
    selection,
    select,
    completeProductIds: selectedProductIds(selection, passengerCount),
  }
}
