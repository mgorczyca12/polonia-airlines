import { ActionIcon, Button } from '@mantine/core'
import {
  Armchair,
  CalendarPlus,
  Check,
  CircleCheck,
  ContactRound,
  Copy,
  Download,
  Headset,
  Plane,
  PlaneLanding,
  PlaneTakeoff,
  TicketCheck,
  UserRound,
} from 'lucide-react'
import { useState } from 'react'
import {
  formatDuration,
  formatShortDate,
  formatTime,
  formatUsd,
  toDateKey,
} from '#/shared/lib/formatting'
import {
  getCityName,
  getFlightNumbersLabel,
  getFlightTimes,
  getStopsLabel,
} from '#/features/flight-results'
import type { BrandedFare, Flight } from '#/features/flight-results'
import type {
  BookingContactDetails,
  PassengerDetails,
} from '../model/booking.types'
import { PassengerBaggageAllowance } from './PassengerBaggageAllowance'
import styles from './BookingConfirmation.module.scss'

interface BookingConfirmationProps {
  flight: Flight
  fares: BrandedFare[]
  passengers: PassengerDetails[]
  contact: BookingContactDetails
  specialRequests: string
  onSearchAgain: () => void
}

function BookingItinerary({ flight }: { flight: Flight }) {
  const times = getFlightTimes(flight)

  if (!times) {
    return (
      <p className={styles.muted}>
        Flight times are unavailable for this demo itinerary.
      </p>
    )
  }

  const departureCode = flight.departureAirport.airportCode
  const arrivalCode = flight.arrivalAirport.airportCode

  return (
    <section className={styles.panel} aria-labelledby="itinerary-heading">
      <div className={styles.panelHeading}>
        <h2 id="itinerary-heading">Your flight itinerary</h2>
        <CalendarPlus size={17} aria-hidden="true" />
      </div>
      <div className={styles.leg}>
        <div className={styles.legHeading}>
          <span>
            <PlaneTakeoff size={16} aria-hidden="true" />
            Outbound · {formatShortDate(toDateKey(times.departure))}
          </span>
          <span className={styles.status}>
            Demo itinerary <Check size={13} aria-hidden="true" />
          </span>
        </div>
        <div className={styles.route}>
          <div className={styles.endpoint}>
            <p>
              {formatTime(times.departure)} <strong>{departureCode}</strong>
            </p>
            <span>{flight.departureAirport.airportName}</span>
          </div>
          <div className={styles.journey}>
            <span>{formatDuration(flight.totalDurationMinutes)}</span>
            <span className={styles.routeLine}>
              <i />
              <Plane size={15} aria-hidden="true" />
              <i />
            </span>
            <span>{getStopsLabel(flight)}</span>
          </div>
          <div className={styles.endpoint}>
            <p>
              {formatTime(times.arrival)}
              {times.arrivalDayOffset > 0 && (
                <sup> +{times.arrivalDayOffset}</sup>
              )}{' '}
              <strong>{arrivalCode}</strong>
            </p>
            <span>{flight.arrivalAirport.airportName}</span>
          </div>
        </div>
        <p className={styles.flightDetail}>
          {getFlightNumbersLabel(flight)} ·{' '}
          {flight.segments.map((segment) => segment.aircraftType).join(' · ')}
        </p>
      </div>
      <p className={styles.demoNote}>
        Return-flight selection is not part of this booking demo.
      </p>
    </section>
  )
}

function downloadItinerary(
  flight: Flight,
  passengers: PassengerDetails[],
  bookingReference: string,
) {
  const times = getFlightTimes(flight)
  const lines = [
    'POLONIA AIRLINES — DEMO ITINERARY',
    `Reference: ${bookingReference}`,
    `Flight: ${getFlightNumbersLabel(flight)}`,
    `Route: ${flight.departureAirport.airportCode} to ${flight.arrivalAirport.airportCode}`,
    times
      ? `Departure: ${formatShortDate(toDateKey(times.departure))} ${formatTime(times.departure)} UTC`
      : 'Departure: unavailable',
    times
      ? `Arrival: ${formatShortDate(toDateKey(times.arrival))} ${formatTime(times.arrival)} UTC`
      : 'Arrival: unavailable',
    'Passengers:',
    ...passengers.map(
      (passenger) =>
        `- ${passenger.firstName} ${passenger.lastName} (${passenger.type})`,
    ),
    '',
    'This is a demo itinerary, not a ticket or confirmed reservation.',
  ]
  const file = new Blob([lines.join('\n')], { type: 'text/plain' })
  const url = URL.createObjectURL(file)
  const link = document.createElement('a')
  link.href = url
  link.download = `${bookingReference.toLowerCase()}.txt`
  document.body.append(link)
  link.click()
  link.remove()
  window.setTimeout(() => URL.revokeObjectURL(url), 0)
}

export function BookingConfirmation({
  flight,
  fares,
  passengers,
  contact,
  specialRequests,
  onSearchAgain,
}: BookingConfirmationProps) {
  const [copied, setCopied] = useState(false)
  const [copyError, setCopyError] = useState(false)
  const bookingReference = `DEMO-${flight.flightNumber}-${fares.map((fare) => fare.fareId).join('')}`
  const total = fares.reduce((sum, fare) => sum + fare.farePriceUsd, 0)
  const leadName = passengers.find(
    (passenger) => passenger.firstName && passenger.lastName,
  )
  const destination = flight.arrivalAirport.airportCode
  const hasDubaiImage = destination === 'DXB'
  const times = getFlightTimes(flight)

  const copyReference = async () => {
    try {
      await navigator.clipboard.writeText(bookingReference)
      setCopied(true)
      setCopyError(false)
    } catch {
      setCopied(false)
      setCopyError(true)
    }
  }

  return (
    <div className={styles.confirmation}>
      <div className={styles.heading}>
        <span className={styles.successIcon}>
          <CircleCheck size={24} aria-hidden="true" />
        </span>
        <div className={styles.headingCopy}>
          <p className={styles.eyebrow}>Your demo booking is ready</p>
          <h1>You’re going to {getCityName(destination)}.</h1>
          <p>
            Thank you{leadName ? `, ${leadName.firstName}` : ''}. Your sample
            itinerary is ready.
            {contact.email ? ` Contact email: ${contact.email}.` : ''} No email
            or reservation was created.
          </p>
        </div>
        <div className={styles.reference}>
          <span>Demo reference</span>
          <strong>{bookingReference}</strong>
          <ActionIcon
            variant="subtle"
            color="gray"
            aria-label={copied ? 'Reference copied' : 'Copy demo reference'}
            onClick={() => void copyReference()}
          >
            <Copy size={16} aria-hidden="true" />
          </ActionIcon>
          {copied && (
            <span className={styles.copiedMessage} role="status">
              Copied
            </span>
          )}
          {copyError && (
            <span className={styles.copyError} role="status">
              Could not copy reference
            </span>
          )}
        </div>
      </div>

      <div
        className={`${styles.destinationHero} ${hasDubaiImage ? styles.dubaiHero : ''}`}
      >
        {hasDubaiImage && (
          <img
            className={styles.destinationImage}
            src="/images/dubai-destination.png"
            alt=""
          />
        )}
        <div className={styles.heroCopy}>
          <span>
            {times
              ? formatShortDate(toDateKey(times.departure))
              : 'Your upcoming journey'}
          </span>
          <h2>A new city is waiting.</h2>
          <p>Your journey with Polonia Airlines starts here.</p>
        </div>
      </div>

      <div className={styles.columns}>
        <div className={styles.main}>
          <BookingItinerary flight={flight} />

          <section
            className={styles.panel}
            aria-labelledby="travellers-heading"
          >
            <div className={styles.panelHeading}>
              <h2 id="travellers-heading">Your travellers</h2>
              <ContactRound size={17} aria-hidden="true" />
            </div>
            <ul className={styles.passengerList}>
              {passengers.map((passenger, index) => (
                <li key={passenger.id}>
                  <UserRound size={15} aria-hidden="true" />
                  <div className={styles.passengerDetails}>
                    <span>
                      {passenger.firstName} {passenger.lastName}
                    </span>
                    <span className={styles.passengerMeta}>
                      {passenger.type}
                      {fares[index] ? ` · ${fares[index].fareName}` : ''}
                    </span>
                    <PassengerBaggageAllowance
                      fare={fares[index]}
                      isInfant={passenger.type === 'Infant'}
                    />
                  </div>
                </li>
              ))}
            </ul>
            {specialRequests && (
              <p className={styles.specialRequests}>
                Special requests: {specialRequests}
              </p>
            )}
          </section>

          <section className={styles.beforeYouGo}>
            <h2>A few things before you go.</h2>
            <div className={styles.tips}>
              <article className={styles.tip}>
                <Armchair size={18} aria-hidden="true" />
                <h3>Make yourself at home</h3>
                <p>
                  Review your fare details and seat options before departure.
                </p>
              </article>
              <article className={styles.tip}>
                <ContactRound size={18} aria-hidden="true" />
                <h3>Get travel-ready</h3>
                <p>Check that traveller names match their travel documents.</p>
              </article>
              <article className={styles.tip}>
                <TicketCheck size={18} aria-hidden="true" />
                <h3>Keep your itinerary close</h3>
                <p>Save a copy of this demo itinerary for your records.</p>
              </article>
            </div>
          </section>
        </div>

        <aside className={styles.sidebar}>
          <section className={styles.summary} aria-labelledby="summary-heading">
            <h2 id="summary-heading">Your journey</h2>
            <p className={styles.summaryRoute}>
              {flight.departureAirport.airportCode} →{' '}
              {flight.arrivalAirport.airportCode}
            </p>
            <p className={styles.summaryFlight}>
              {flight.flightNumber} · {passengers.length}{' '}
              {passengers.length === 1 ? 'traveller' : 'travellers'}
            </p>
            <div className={styles.summaryItems}>
              {fares.map((fare, index) => (
                <p key={`${fare.fareId}-${index}`}>
                  <span>
                    Traveller {index + 1} · {fare.fareName}
                  </span>
                  <span>{formatUsd(fare.farePriceUsd)}</span>
                </p>
              ))}
            </div>
            <div className={styles.total}>
              <span>Demo total · USD</span>
              <strong>{formatUsd(total)}</strong>
            </div>
            <p className={styles.paymentNotice}>
              <Check size={14} aria-hidden="true" />
              No payment was taken
            </p>
          </section>

          <Button
            className="polonia-action-button"
            rightSection={<Download size={16} aria-hidden="true" />}
            onClick={() =>
              downloadItinerary(flight, passengers, bookingReference)
            }
          >
            Download demo itinerary
          </Button>
          <Button
            variant="default"
            leftSection={<PlaneLanding size={16} aria-hidden="true" />}
            onClick={onSearchAgain}
          >
            Search another flight
          </Button>

          <section className={styles.help}>
            <Headset size={18} aria-hidden="true" />
            <div>
              <h2>A little help along the way?</h2>
              <p>
                This sample booking is not connected to reservations support.
              </p>
            </div>
          </section>
        </aside>
      </div>
    </div>
  )
}
