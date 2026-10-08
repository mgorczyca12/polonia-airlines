# Booking service domain

`Reservation` owns its passengers, their baggage, and recorded payment references.
It references the booking user and flight-service flights by integer ID, without
cross-service entity navigation. An ordered itinerary contains one or more distinct
flight IDs; flight availability, connection validity, and inventory allocation
must be checked by the application layer through service integrations.
The itinerary is represented by reservation-owned `ReservationFlight` children,
each with an external `FlightId` and a one-based `SequenceNumber`. `FlightIds` is
a derived convenience projection sorted by that sequence, not a persisted navigation.

The reservation starts as `Created`. Adding passengers and baggage is allowed only
in this state. `Reserve()` requires at least one passenger and transitions to
`Reserved`; the application calls it after securing inventory. Payment completion
is not a prerequisite enforced by this initial domain model. Created and reserved
reservations can transition to `Cancelled`; cancelled reservations cannot be
edited or reserved again. Cancellation retains the booking and payment history;
it does not itself release inventory or issue refunds.

`Fare` is an immutable quote for the **entire reservation**, including all passengers
and itinerary flights, not a per-passenger or per-flight price. Pricing and baggage
allowances are outside this initial model. Payment records contain an external
reference, amount, currency, and recording timestamp, not card details or payment
processing state. Recorded amounts must be positive, use the fare currency, have
unique references within the reservation, and not exceed its quoted total.

`User` holds booking contact details, not credentials. Passenger names and dates
of birth are booking-specific snapshots. Check-in, boarding, payment processing,
rebooking, retention policies, persistence mappings, and integration/domain event
delivery are deferred. Future travel status should be passenger-and-flight-specific.

## Persistence preparation

Children expose private-set parent IDs and navigations: passengers, payments,
itinerary flights, and the fare snapshot reference their reservation; baggage
references its passenger. `Reservation.Create` copies the input fare quote into
an exclusive snapshot, so reusing a quote does not share a persisted child between
reservations. The unattached input quote itself is not part of the persisted graph.
New entities have default IDs until persistence assigns them; the connected
navigations allow EF relationship fixup to propagate generated keys.

Infrastructure should explicitly map collection backing fields using field access,
ignore the derived `FlightIds` projection and domain events, convert status through
`ReservationStatus.From`, and configure the fare as a required one-to-one dependent.
Itinerary order must use `SequenceNumber` rather than database row order. Configure
user IDs as externally supplied, appropriate precision/lengths, and unique booking
codes, payment references per reservation, and flight/sequence keys per reservation.

Command loading must include the fare and children required by domain invariants.
Concurrent mutations need aggregate-level concurrency control, including child-only
payment additions; database constraints alone cannot enforce the payment sum cap.
These are requirements for the future Infrastructure layer, not implemented mappings
or verified persistence behavior.
