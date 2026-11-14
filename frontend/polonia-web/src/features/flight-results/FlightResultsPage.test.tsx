// @vitest-environment jsdom
import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ReactNode } from 'react'
import { createOffer, getFares, searchFlights } from '#/shared/api'
import type * as ApiModule from '#/shared/api'
import type { FlightFareResponse } from '#/shared/api'
import { renderWithProviders, stubMatchMedia } from '#/shared/testing/render'
import { FlightResultsPage } from './ui/FlightResultsPage'

const navigate = vi.fn()

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to }: { children?: ReactNode; to?: string }) => (
    <a href={to}>{children}</a>
  ),
  useNavigate: () => navigate,
}))

vi.mock('#/shared/api', async () => {
  const actual = await vi.importActual<typeof ApiModule>('#/shared/api')
  return {
    ...actual,
    createOffer: vi.fn(),
    getFares: vi.fn(),
    searchFlights: vi.fn(),
  }
})

const OFFER_ID = '22222222-2222-2222-2222-222222222222'

function fare(productId: string, brand: string, amount: number) {
  return {
    flightId: '7',
    productId,
    productCode: brand.toUpperCase(),
    productVersion: 1,
    cabinId: '1',
    cabinClass: 'Economy',
    brand,
    amount,
    currency: 'USD',
    baggage: {
      includedPieces: 1,
      maxWeightPerPieceKg: 23,
      maxExtraPieces: 1,
      extraPiecePrice: 50,
    },
  } satisfies FlightFareResponse
}

const search = { from: 'ORD', to: 'KRK', departure: '2026-12-01', adults: 2 }

function renderPage() {
  return renderWithProviders(
    <FlightResultsPage search={search} onSearchChange={vi.fn()} />,
  )
}

beforeEach(() => {
  vi.resetAllMocks()
  stubMatchMedia()
  vi.mocked(searchFlights).mockResolvedValue([
    {
      id: 7,
      flightNumber: 'PA101',
      departureAirport: 'ORD',
      arrivalAirport: 'KRK',
      departureTime: '2026-12-01T21:30:00+00:00',
      arrivalTime: '2026-12-02T08:05:00+00:00',
    },
  ])
  vi.mocked(getFares).mockResolvedValue([
    fare('11111111-1111-1111-1111-111111111111', 'Light', 420),
    fare('33333333-3333-3333-3333-333333333333', 'Flex', 610),
  ])
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('flight results', () => {
  it('prices a fare for every traveller before creating an offer and opening it', async () => {
    vi.mocked(createOffer).mockResolvedValue({
      id: OFFER_ID,
      expiresAt: '2026-12-01T00:15:00+00:00',
      currency: 'USD',
      totalAmount: 840,
      fares: [],
    })
    renderPage()

    expect(await screen.findByText('PA101')).toBeDefined()
    const continueButton = screen.getByRole('button', {
      name: 'Review price and add passengers',
    })
    expect(continueButton.hasAttribute('disabled')).toBe(true)

    fireEvent.click(screen.getAllByRole('button', { name: 'Select fare' })[0])

    expect(screen.getByText('$840.00')).toBeDefined()
    fireEvent.click(continueButton)

    await waitFor(() =>
      expect(navigate).toHaveBeenCalledWith({
        to: '/booking',
        search: { offer: OFFER_ID },
      }),
    )
    expect(createOffer).toHaveBeenCalledWith(7, [
      '11111111-1111-1111-1111-111111111111',
      '11111111-1111-1111-1111-111111111111',
    ])
  })

  it('shows an explicit empty state rather than sample flights', async () => {
    vi.mocked(searchFlights).mockResolvedValue([])
    renderPage()

    expect(
      await screen.findByText(/No scheduled flights match this search/),
    ).toBeDefined()
    expect(getFares).not.toHaveBeenCalled()
  })

  it('reports a failed search with a retry instead of an empty result', async () => {
    vi.mocked(searchFlights).mockRejectedValue(new Error('Backend unavailable'))
    renderPage()

    expect(await screen.findByText('Backend unavailable')).toBeDefined()
    expect(screen.getByRole('button', { name: 'Retry search' })).toBeDefined()
    expect(screen.queryByText(/No scheduled flights/)).toBeNull()
  })
})
