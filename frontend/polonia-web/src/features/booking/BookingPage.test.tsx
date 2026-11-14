// @vitest-environment jsdom
import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ReactNode } from 'react'
import {
  createReservation,
  getFlight,
  getOffer,
  getReservation,
  recoverReservation,
} from '#/shared/api'
import type * as ApiModule from '#/shared/api'
import type { FareSnapshot, ReservationResponse } from '#/shared/api'
import { renderWithProviders, stubMatchMedia } from '#/shared/testing/render'
import { BookingPage } from './ui/BookingPage'

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to }: { children?: ReactNode; to?: string }) => (
    <a href={to}>{children}</a>
  ),
}))

vi.mock('#/shared/api', async () => {
  const actual = await vi.importActual<typeof ApiModule>('#/shared/api')
  return {
    ...actual,
    createReservation: vi.fn(),
    getFlight: vi.fn(),
    getOffer: vi.fn(),
    getReservation: vi.fn(),
    recoverReservation: vi.fn(),
  }
})

const OFFER_ID = '11111111-1111-1111-1111-111111111111'

const fare: FareSnapshot = {
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
}

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
  fares: [fare],
}

function renderBooking() {
  return renderWithProviders(<BookingPage offerId={OFFER_ID} />)
}

beforeEach(() => {
  vi.resetAllMocks()
  stubMatchMedia()
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
      renderBooking()

      await screen.findByText(/Contact: Saved Contact/)
      expect(screen.getByText('Saved Passenger', { exact: true })).toBeDefined()
      expect(screen.getByText('ABC123')).toBeDefined()
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

  it('keeps polling a pending reservation until inventory is confirmed', async () => {
    vi.mocked(recoverReservation).mockResolvedValue(stored)
    vi.mocked(getReservation).mockResolvedValue({
      ...stored,
      status: 'Reserved',
      workflowStatus: 'Completed',
    })
    renderBooking()

    expect(
      await screen.findByRole('heading', { name: 'Reservation status' }),
    ).toBeDefined()
    expect(
      await screen.findByRole(
        'heading',
        { name: 'Your reservation is confirmed.' },
        { timeout: 4000 },
      ),
    ).toBeDefined()
    expect(getReservation).toHaveBeenCalledWith(1, expect.any(AbortSignal))
  })
})

describe('reservation submission', () => {
  beforeEach(() => {
    vi.mocked(getOffer).mockResolvedValue({
      id: OFFER_ID,
      expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
      currency: 'USD',
      totalAmount: 125,
      fares: [fare],
    })
  })

  async function completeDetails() {
    const firstName = await screen.findByLabelText(/First name/)
    await waitFor(() =>
      expect(
        screen
          .getByRole('button', { name: 'Review reservation' })
          .hasAttribute('disabled'),
      ).toBe(false),
    )
    fireEvent.change(firstName, { target: { value: 'Ada' } })
    fireEvent.change(screen.getByLabelText(/Last name/), {
      target: { value: 'Lovelace' },
    })
    fireEvent.change(screen.getByLabelText(/Date of birth/), {
      target: { value: '1990-01-01' },
    })
    fireEvent.change(screen.getByLabelText(/Contact name/), {
      target: { value: 'Ada Lovelace' },
    })
    fireEvent.change(screen.getByLabelText(/Contact email/), {
      target: { value: 'ada@example.test' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Review reservation' }))
    return screen.findByRole('button', { name: 'Reserve without payment' })
  }

  it('adopts a reservation committed by a lost earlier response instead of creating another', async () => {
    vi.mocked(recoverReservation)
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce(stored)
    renderBooking()

    fireEvent.click(await completeDetails())

    expect(await screen.findByText('ABC123')).toBeDefined()
    expect(createReservation).not.toHaveBeenCalled()
  })

  it('submits the entered details once no committed reservation exists', async () => {
    vi.mocked(recoverReservation).mockResolvedValue(null)
    vi.mocked(createReservation).mockResolvedValue(stored)
    renderBooking()

    fireEvent.click(await completeDetails())

    await screen.findByText('ABC123')
    expect(createReservation).toHaveBeenCalledWith({
      offerId: OFFER_ID,
      contactName: 'Ada Lovelace',
      contactEmail: 'ada@example.test',
      passengers: [
        { firstName: 'Ada', lastName: 'Lovelace', dateOfBirth: '1990-01-01' },
      ],
    })
  })

  it('keeps entered details when returning from review to edit', async () => {
    vi.mocked(recoverReservation).mockResolvedValue(null)
    renderBooking()

    await completeDetails()
    fireEvent.click(screen.getByRole('button', { name: 'Edit details' }))

    expect(screen.getByLabelText<HTMLInputElement>(/First name/).value).toBe(
      'Ada',
    )
  })
})
