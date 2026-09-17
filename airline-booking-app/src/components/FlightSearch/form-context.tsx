import { createFormContext } from '@mantine/form';
export const [FormProvider, useFormContext, useForm] = createFormContext<FlightSearchFormValues>();

export interface FlightSearchFormValues {
  departureAirportCode: string | null,
  arrivalAirportCode: string | null,
  departureDate: Date | null;
  returnDate: Date | null;
  tripType: string | null;
  passengers: { 
    adult: number; 
    child: number; 
    infant: number; 
  }; 
}