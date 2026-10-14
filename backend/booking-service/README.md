# Booking service domain

`Reservation` owns its passengers, their fares, baggage, and recorded payment references.
It references its local booking User by integer ID and flight-service flights by
opaque string IDs, without cross-service entity navigation. An ordered itinerary contains one or more distinct
flight IDs; flight availability, connection validity, and inventory allocation
must be checked by the application layer through service integrations.
The itinerary is represented by reservation-owned `ReservationFlight` children,
each with an external `FlightId` and a one-based `SequenceNumber`. `FlightIds` is
a derived convenience projection sorted by that sequence, not a persisted navigation.
External flight IDs preserve their canonical contract representation and use
ordinal, case-sensitive comparison without parsing, trimming, or normalization.

The reservation starts as `Created`. Adding passengers and baggage is allowed only
in this state. `Reserve()` requires at least one passenger and transitions to
`Reserved`; the application calls it after securing inventory. Payment completion
is not a prerequisite enforced by this initial domain model. Created and reserved
reservations can transition to `Cancelled`; cancelled reservations cannot be
edited or reserved again. Cancellation retains the booking and payment history;
it does not itself release inventory or issue refunds.

## Fares and baggage

Fare products, cabin classes, aircraft layouts, and seat maps are **not owned by this
service**. Layouts and cabins live in flight-service and seat assignments in
seating-service; the fare catalog belongs with cabin/layout data (flight-service or a
future pricing service). Booking consumes a `FareQuote` per passenger per flight and
stores an immutable snapshot, so later catalog changes never alter booked terms.
Product codes, cabin classes, and flight IDs are opaque external references preserved
as supplied; there is no seat or layout modelling here.

- `FareQuote` (value object) is the priced input: flight ID, product code, cabin class,
  amount, currency, and a `BaggageAllowance`.
- `Reservation.AddPassenger` requires exactly one quote for **every** itinerary flight
  (no duplicates, none outside the itinerary, currency equal to the reservation
  currency). Different passengers can hold different products and cabins, and one
  passenger can fly different cabins on different flights.
- `PassengerFare` (entity) is the snapshot of that quote, owned by the passenger and
  linked to the `ReservationFlight`.
- `BaggageAllowance` is fare-controlled: included pieces, maximum weight per piece,
  maximum extra pieces, and the price per extra piece. Allowance is counted per
  passenger **per flight**.
- `Reservation.AddBaggage(passenger, flightId, ...)` charges nothing for included
  pieces, the extra-piece price for pieces beyond the allowance, and **rejects** a
  piece that exceeds the per-piece weight or `IncludedPieces + MaxExtraPieces`. The
  charge is snapshotted on `Baggage`.
- `Reservation.TotalAmount` is the sum of passenger fares plus baggage charges, and
  payments are capped against it. The reservation has a single `Currency`.

Aircraft hold capacity is a flight-level limit that belongs to flight-service
inventory. It is deferred: for now only the fare's per-passenger hard limit is
enforced, and the application layer will need to reserve cargo capacity through
service integration later. Moving excess baggage to another flight is out of scope.

Payment records contain an external
reference, amount, currency, and recording timestamp, not card details or payment
processing state. Recorded amounts must be positive, use the reservation currency,
have unique references within the reservation, and not exceed its total.

`User` holds booking contact details, not credentials. Passenger names and dates
of birth are booking-specific snapshots. Check-in, boarding, payment processing,
rebooking, retention policies, persistence mappings, and integration/domain event
delivery are deferred. Future travel status should be passenger-and-flight-specific.

## Persistence preparation

Children expose private-set parent IDs and navigations: passengers, payments,
itinerary flights, and passenger fares reference their owner; baggage references its
passenger and the passenger fare it was charged against. `BaggageAllowance` and
`FareQuote` are immutable value objects (the allowance is copied into each
`PassengerFare`; map it as an owned type). `Reservation.AddPassenger` copies each input quote into
an exclusive snapshot, so reusing a quote does not share a persisted child between
passengers. The unattached input quotes are not part of the persisted graph.
New entities have default IDs until persistence assigns them; the connected
navigations allow EF relationship fixup to propagate generated keys.

Infrastructure should explicitly map collection backing fields using field access,
ignore the derived `FlightIds` projection, `TotalAmount`, and domain events, convert status through
`ReservationStatus.From`, and map `PassengerFare` as a required dependent of both its passenger
and its itinerary flight (avoiding multiple cascade paths).
Itinerary order must use `SequenceNumber` rather than database row order. Configure
user IDs as externally supplied, appropriate precision/lengths, and unique booking
codes, payment references per reservation, and flight/sequence keys per reservation.

Command loading must include passengers, their fares and baggage, and the other children required by domain invariants.
Concurrent mutations need aggregate-level concurrency control, including child-only
payment additions; database constraints alone cannot enforce the payment sum cap.
These are requirements for the future Infrastructure layer, not implemented mappings
or verified persistence behavior.
