const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function isUuid(value: unknown): value is string {
  return typeof value === 'string' && UUID_PATTERN.test(value)
}

/**
 * Stable reservation submission identity for an offer. Uses the GUID's canonical
 * lowercase form so differently cased offer URLs recover the same booking.
 */
export function webReservationRequestId(offerId: string): string {
  return `web-${offerId.toLowerCase()}`
}
