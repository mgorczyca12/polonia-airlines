import { createFormContext } from '@mantine/form'
import type { PassengerRequest } from '#/shared/api'
import { isDateKey, toDateKey } from '#/shared/lib/formatting'

export const NAME_MAX_LENGTH = 100
export const CONTACT_NAME_MAX_LENGTH = 200
export const EMAIL_MAX_LENGTH = 254
const MINIMUM_PASSENGER_AGE_YEARS = 2
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export interface BookingFormValues {
  passengers: PassengerRequest[]
  contactName: string
  contactEmail: string
}

export const [BookingFormProvider, useBookingFormContext, useBookingForm] =
  createFormContext<BookingFormValues>()

/** Latest birth date accepted for a seated passenger, as `YYYY-MM-DD` (UTC). */
export function latestBirthDateKey(now = new Date()): string {
  return toDateKey(
    new Date(
      Date.UTC(
        now.getUTCFullYear() - MINIMUM_PASSENGER_AGE_YEARS,
        now.getUTCMonth(),
        now.getUTCDate(),
      ),
    ),
  )
}

function requiredText(label: string, maxLength: number) {
  return (value: string) => {
    if (!value.trim()) {
      return `Enter ${label}`
    }

    return value.length > maxLength
      ? `Use ${maxLength} characters or fewer`
      : null
  }
}

/** Mirrors the web API's rules: a real calendar date for a traveller aged two or older. */
export function validateBirthDate(value: string, now = new Date()) {
  if (!isDateKey(value)) {
    return 'Enter a valid date of birth'
  }

  return value > latestBirthDateKey(now)
    ? 'Bookings for children under two are not available yet'
    : null
}

export function validateEmail(value: string) {
  if (!EMAIL_PATTERN.test(value)) {
    return 'Enter a valid email address'
  }

  return value.length > EMAIL_MAX_LENGTH
    ? `Use ${EMAIL_MAX_LENGTH} characters or fewer`
    : null
}

export function createBookingFormOptions(passengerCount: number) {
  return {
    mode: 'controlled' as const,
    initialValues: {
      passengers: Array.from({ length: passengerCount }, () => ({
        firstName: '',
        lastName: '',
        dateOfBirth: '',
      })),
      contactName: '',
      contactEmail: '',
    },
    validate: {
      passengers: {
        firstName: requiredText('a first name', NAME_MAX_LENGTH),
        lastName: requiredText('a last name', NAME_MAX_LENGTH),
        dateOfBirth: (value: string) => validateBirthDate(value),
      },
      contactName: requiredText('a contact name', CONTACT_NAME_MAX_LENGTH),
      contactEmail: validateEmail,
    },
  }
}
