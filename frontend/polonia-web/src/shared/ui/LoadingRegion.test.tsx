// @vitest-environment jsdom
import { act, cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LoadingRegion } from './LoadingRegion'

afterEach(() => {
  cleanup()
  vi.useRealTimers()
})

describe('LoadingRegion', () => {
  it('announces loading immediately but shows the placeholder only after the delay', () => {
    vi.useFakeTimers()
    render(
      <LoadingRegion label="Loading flights" delayMs={200}>
        <p>Skeleton</p>
      </LoadingRegion>,
    )

    expect(
      screen.getByRole('status', { name: 'Loading flights' }),
    ).toBeDefined()
    expect(screen.queryByText('Skeleton')).toBeNull()

    act(() => vi.advanceTimersByTime(200))

    expect(screen.getByText('Skeleton')).toBeDefined()
  })
})
