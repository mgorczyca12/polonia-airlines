import { parseLiveSearchParams } from '#/features/flight-search'
import type { FlightSearchParams } from '#/features/flight-search'

const MAX_PAGE = 1000

/** Flight list URL state: the live search plus the result page (omitted for page 1). */
export type FlightListSearch = FlightSearchParams & {
  unsupported?: boolean
  page?: number
}

function readPage(value: unknown): number | undefined {
  return typeof value === 'number' &&
    Number.isInteger(value) &&
    value > 1 &&
    value <= MAX_PAGE
    ? value
    : undefined
}

export function parseFlightListSearch(
  search: Record<string, unknown>,
): FlightListSearch {
  const page = readPage(search.page)
  return { ...parseLiveSearchParams(search), ...(page ? { page } : {}) }
}
