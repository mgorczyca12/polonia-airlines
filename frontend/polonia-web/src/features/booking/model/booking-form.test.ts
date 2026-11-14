import { describe, expect, it } from 'vitest'
import { validateBirthDate, validateEmail } from './booking-form'

const NOW = new Date('2026-11-14T12:00:00Z')

describe('booking form validation', () => {
  it('accepts travellers aged two or older and rejects younger ones like the server', () => {
    expect(validateBirthDate('2024-11-14', NOW)).toBeNull()
    expect(validateBirthDate('2024-11-15', NOW)).toBe(
      'Bookings for children under two are not available yet',
    )
  })

  it('rejects dates that are not real calendar dates', () => {
    expect(validateBirthDate('1990-02-30', NOW)).toBe(
      'Enter a valid date of birth',
    )
    expect(validateBirthDate('', NOW)).toBe('Enter a valid date of birth')
  })

  it('requires a plausible email address within the length limit', () => {
    expect(validateEmail('guest@example.test')).toBeNull()
    expect(validateEmail('guest@example')).toBe('Enter a valid email address')
    expect(validateEmail(`${'a'.repeat(250)}@x.io`)).toBe(
      'Use 254 characters or fewer',
    )
  })
})
