export interface FlightAirport {
  airportId: number
  airportName: string
  airportCode: string
}

export interface FlightSegment {
  segmentIndex: number
  flightNumber: string
  aircraftType: string
  departureAirportCode: string
  arrivalAirportCode: string
  departureDateTimeUtc: string
  arrivalDateTimeUtc: string
  availableCabins: string[]
}

export interface FlightLayover {
  airportCode: string
  airportName: string
  durationMinutes: number
}

export interface BrandedFare {
  fareId: number
  fareName: string
  farePriceUsd: number
  isMixedCabin: boolean
  segmentCabinOverlays: Record<string, string>
  perks: string[]
}

export interface FlightCabin {
  classId: number
  className: string
  fares: BrandedFare[]
}

export interface Flight {
  flightNumber: string
  isNonStop: boolean
  totalDurationMinutes: number
  basePriceUsd: number
  departureAirport: FlightAirport
  arrivalAirport: FlightAirport
  layovers: FlightLayover[]
  segments: FlightSegment[]
  brandedFares: FlightCabin[]
}