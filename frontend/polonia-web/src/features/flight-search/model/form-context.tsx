import { createFormContext } from '@mantine/form'
import type { CabinPreference, TravellerCounts, TripType } from './search-params'

/** Date fields hold Mantine's `YYYY-MM-DD` date strings. */
export interface FlightSearchFormValues {
  departureAirportCode: string | null
  arrivalAirportCode: string | null
  departureDate: string | null
  returnDate: string | null
  tripType: TripType
  passengers: TravellerCounts
  cabin: CabinPreference
}

export const [FormProvider, useFormContext] =
  createFormContext<FlightSearchFormValues>()
