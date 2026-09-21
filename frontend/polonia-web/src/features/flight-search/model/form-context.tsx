import { createFormContext } from '@mantine/form'

export interface FlightSearchFormValues {
  departureAirportCode: string | null
  arrivalAirportCode: string | null
  departureDate: Date | null
  returnDate: Date | null
  tripType: 'roundTrip' | 'oneWay'
  passengers: {
    adult: number
    child: number
    infant: number
  }
}

export const [FormProvider, useFormContext] = createFormContext<FlightSearchFormValues>()