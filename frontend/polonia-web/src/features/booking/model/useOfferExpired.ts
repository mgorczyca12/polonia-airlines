import { useEffect, useState } from 'react'
import { offerTimeRemaining } from '#/entities/offer'

/** Whether an offer has expired, updating when its expiry time passes. */
export function useOfferExpired(expiresAt: string | undefined): boolean {
  const [expiredAt, setExpiredAt] = useState<string | null>(null)

  useEffect(() => {
    if (!expiresAt) {
      return
    }

    const timer = setTimeout(
      () => setExpiredAt(expiresAt),
      Math.max(0, offerTimeRemaining(expiresAt)),
    )
    return () => clearTimeout(timer)
  }, [expiresAt])

  return expiresAt !== undefined && expiredAt === expiresAt
}
