import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'

interface LoadingRegionProps {
  /** Accessible description of what is loading, e.g. "Loading flights". */
  label: string
  /** Placeholder shown once loading lasts longer than `delayMs`. */
  children: ReactNode
  delayMs?: number
}

/**
 * Busy region for a section's first load. The placeholder appears only after a
 * short delay so fast responses do not flash a skeleton.
 */
export function LoadingRegion({
  label,
  children,
  delayMs = 200,
}: LoadingRegionProps) {
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => setVisible(true), delayMs)
    return () => clearTimeout(timer)
  }, [delayMs])

  return (
    <div role="status" aria-busy="true" aria-label={label}>
      {visible && children}
    </div>
  )
}
