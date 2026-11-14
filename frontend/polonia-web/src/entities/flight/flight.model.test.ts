import { describe, expect, it } from 'vitest'
import { toFlightSummary } from './flight.model'

describe('toFlightSummary', () => {
  it('derives duration, UTC departure date and next-day arrival from timestamps', () => {
    const summary = toFlightSummary({
      id: 7,
      flightNumber: 'PA101',
      departureAirport: 'ORD',
      arrivalAirport: 'ZZZ',
      departureTime: '2026-12-01T21:30:00+00:00',
      arrivalTime: '2026-12-02T08:05:00+00:00',
    })

    expect(summary.durationMinutes).toBe(635)
    expect(summary.departureDateKey).toBe('2026-12-01')
    expect(summary.arrivalDayOffset).toBe(1)
    expect(summary.arrival.city).toBe('ZZZ')
  })

  it('uses UTC calendar days even when the timestamp carries another offset', () => {
    const summary = toFlightSummary({
      id: 8,
      flightNumber: 'PA102',
      departureAirport: 'KRK',
      arrivalAirport: 'ORD',
      departureTime: '2026-12-01T23:30:00-02:00',
      arrivalTime: '2026-12-02T09:00:00+00:00',
    })

    expect(summary.departureDateKey).toBe('2026-12-02')
    expect(summary.arrivalDayOffset).toBe(0)
  })
})
