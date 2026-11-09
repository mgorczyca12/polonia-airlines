export interface PassengerDetails {
  id: string
  label: string
  type: 'Adult' | 'Child' | 'Infant'
  title: string
  firstName: string
  lastName: string
  dateOfBirth: string
  nationality: string
  membershipNumber: string
}

export interface BookingContactDetails {
  email: string
  mobile: string
  smsUpdates: boolean
}

export type PaymentMethod = 'card' | 'wallet' | 'mobileWallet'

export interface SpecialRequests {
  details: string
}
