export interface FlightResponse {
  id: number
  flightNumber: string
  departureTime: string
  arrivalTime: string
  departureAirport: string
  arrivalAirport: string
}

export interface BaggageSnapshot {
  includedPieces: number
  maxWeightPerPieceKg: number
  maxExtraPieces: number
  extraPiecePrice: number
}

export interface FlightFareResponse {
  flightId: string
  productId: string
  productCode: string
  productVersion: number
  cabinId: string
  cabinClass: string
  brand: string
  amount: number
  currency: string
  baggage: BaggageSnapshot
}

export interface FareSnapshot extends FlightFareResponse {
  passengerIndex: number
}

export interface OfferResponse {
  id: string
  expiresAt: string
  currency: string
  totalAmount: number
  fares: FareSnapshot[]
}

export interface ReservationResponse {
  id: number
  code: string
  status: string
  reservationDate: string
  currency: string
  totalAmount: number
  fares: FareSnapshot[]
  workflowId: string
  workflowStatus: string
  failureCode: string | null
  contactName: string
  contactEmail: string
  passengers: Array<{ id: number; firstName: string; lastName: string }>
}

export interface PassengerRequest {
  firstName: string
  lastName: string
  dateOfBirth: string
}

export interface ReservationRequest {
  offerId: string
  contactName: string
  contactEmail: string
  passengers: PassengerRequest[]
}
