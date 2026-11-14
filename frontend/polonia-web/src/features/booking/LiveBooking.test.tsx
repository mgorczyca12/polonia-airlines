// @vitest-environment jsdom
import { MantineProvider } from '@mantine/core'
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ReactNode } from 'react'
import { LiveBooking } from './components/LiveBooking'
import {
  getFlight,
  getOffer,
  getReservation,
  recoverReservation,
} from '#/shared/api/client'
import type { ReservationResponse } from '#/shared/api/contracts'
import type * as ApiClient from '#/shared/api/client'

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to }: { children?: ReactNode; to?: string }) => (
    <a href={to}>{children}</a>
  ),
}))

vi.mock('#/shared/api/client', async () => {
  const actual = await vi.importActual<typeof ApiClient>('#/shared/api/client')
  return {
    ...actual,
    getFlight: vi.fn(),
    getOffer: vi.fn(),
    getReservation: vi.fn(),
    recoverReservation: vi.fn(),
  }
})

const stored: ReservationResponse = {
  id: 1,
  code: 'ABC123',
  status: 'Created',
  workflowStatus: 'Pending',
  workflowId: '11111111-1111-1111-1111-111111111111',
  failureCode: null,
  reservationDate: '2026-11-13T12:00:00+00:00',
  currency: 'USD',
  totalAmount: 125,
  contactName: 'Saved Contact',
  contactEmail: 'saved@example.test',
  passengers: [{ id: 1, firstName: 'Saved', lastName: 'Passenger' }],
  fares: [
    {
      passengerIndex: 0,
      flightId: '1',
      productId: '11111111-1111-1111-1111-111111111111',
      productCode: 'LIGHT',
      productVersion: 1,
      cabinId: '1',
      cabinClass: 'Economy',
      brand: 'Light',
      amount: 125,
      currency: 'USD',
      baggage: {
        includedPieces: 1,
        maxWeightPerPieceKg: 23,
        maxExtraPieces: 1,
        extraPiecePrice: 50,
      },
    },
  ],
}

beforeEach(() => {
  vi.resetAllMocks()
  vi.stubGlobal(
    'matchMedia',
    vi.fn().mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  )
  vi.mocked(getFlight).mockResolvedValue({
    id: 1,
    flightNumber: 'PA101',
    departureAirport: 'ORD',
    arrivalAirport: 'KRK',
    departureTime: '2026-12-01T12:00:00+00:00',
    arrivalTime: '2026-12-01T20:00:00+00:00',
  })
  vi.mocked(getReservation).mockResolvedValue(stored)
})

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('stored booking status', () => {
  it.each([
    { status: 'Created', workflowStatus: 'Pending', confirmed: false },
    { status: 'Created', workflowStatus: 'Completed', confirmed: false },
    { status: 'Reserved', workflowStatus: 'Completed', confirmed: true },
    { status: 'Cancelled', workflowStatus: 'Cancelled', confirmed: false },
  ])(
    'renders $status/$workflowStatus without inventing confirmation or losing stored details',
    async ({ status, workflowStatus, confirmed }) => {
      vi.mocked(recoverReservation).mockResolvedValue({
        ...stored,
        status,
        workflowStatus,
      })
      render(
        <MantineProvider>
          <LiveBooking offerId="11111111-1111-1111-1111-111111111111" />
        </MantineProvider>,
      )

      await screen.findByText(/Contact: Saved Contact/)
      expect(screen.getByText('Saved Passenger', { exact: true })).toBeDefined()
      expect(
        screen.getByRole('heading', { name: 'Reservation reference: ABC123' }),
      ).toBeDefined()
      expect(
        Boolean(
          screen.queryByRole('heading', {
            name: 'Your reservation is confirmed.',
          }),
        ),
      ).toBe(confirmed)
      expect(
        screen.queryByRole('button', { name: 'Reserve without payment' }),
      ).toBeNull()
      expect(vi.mocked(getOffer)).not.toHaveBeenCalled()
    },
  )
})
