# Booking service

## Running and provisioning

Configure `ConnectionStrings:BookingDatabase` (PostgreSQL) and `RabbitMq:Host`,
`RabbitMq:User`, `RabbitMq:Pass` (also optional `Port`, `VHost`, `UseSsl`), validated on
start as in flight-service. Never put deployed credentials in source control.

Like flight-service, the host neither creates nor migrates databases, and there are no
bootstrap flags or startup seeding. No migrations are checked in: generate and review an
initial migration (booking, saga, inbox and outbox tables) with EF Core tooling before
deploying. The `Reservations.Code` PNR unique index (`UX_Reservation_Pnr`) and the
request-idempotency indexes must be part of the deployed schema.

### Catalog administration

No authentication is configured, so there are deliberately **no HTTP catalog mutation
or inventory administration endpoints**. Trusted code can dispatch `PublishFareProductCommand`,
`SetFlightPriceCommand`, and `RetireFareProductCommand` through DispatchR; they are
transactional commands with validators, and failures are typed `BookingError` results.
Published product terms are immutable: change cabin class, brand, or baggage by
publishing a new `(Code, Version)` and retire old versions explicitly. Prices are keyed
by flight ID, product ID and uppercase currency, with no default price or
client-supplied amount. Physical `CabinId` belongs to each flight price, not the global
product: two flights can price the same Economy product into different layout cabins, and
a physical ID is never inferred from a cabin-class name. Inventory capacity and cabin
existence remain flight/seating-service responsibilities.

## Code organization

Follows flight-service: `Api` (controllers per resource, `BookingResultExtensions`),
`Application` (`Features/<Name>Features/Commands|Queries`, repository abstractions,
`Contracts/Requests|Responses`, typed `BookingError`), `Domain` (aggregates and value
objects such as `PnrCode`, `GuestToken`, `OfferedFare`) and `Infrastructure`
(`Persistence/Configuration|Queries`, `Repositories`, `Integrations/Sagas`).
Commands go through the shared validation/transaction pipelines; reads use query
services validated at the API boundary. `CreateReservationCommand` atomically consumes
the offer, saves the reservation and publishes `ReservationRequested` via the outbox.

## Guest checkout API

All routes start with `/api/booking`. Bearer-like guest tokens must be kept secret,
sent only over HTTPS, and never put in URLs/logs. Responses disable caching.

1. `POST /checkouts` returns a random 256-bit `guestToken`; only its SHA-256 hash is stored.
2. `GET /catalog` lists active product versions and baggage terms.
3. `POST /offers`, with `X-Guest-Token`, accepts `currency`, `passengerCount` (1–9),
   and `selections`: `{ "flightId": "...", "productId": "guid" }`.
   Omit `passengerIndex` to select the product for everyone, or supply zero-based
   passenger indexes for different products/cabins. Every passenger must have exactly
   one selection for each of at most eight distinct flights. Prices are read on the
   server and persisted as exact per-passenger/per-flight snapshots, including product
   version, physical cabin ID, cabin class, brand, currency, and baggage. Offers expire exactly 15 minutes
   after creation using UTC.
4. `GET /offers/{id}` requires the same guest token. Unknown/wrong-owner offers return
   404, expired offers return 409.
5. `POST /reservations`, with the same header, accepts `offerId`, durable `requestId`,
   `contactName`, `contactEmail`, and ordered `passengers` (`firstName`, `lastName`,
   `dateOfBirth`). It accepts no authoritative account ID, price, timestamp, or status.
   The passenger order matches the offer indexes. Returns **201** with `status: Created`
   and `workflowStatus: Pending`, not a promise of inventory confirmation.
6. `GET /reservations/{id}` requires the same secret guest token, even with a known
   PNR or reservation ID. It returns reservation status plus `workflowId`,
   `workflowStatus`, and safe `failureCode` from the persisted workflow, or `Pending`
   until the saga starts. Unauthorized/wrong-owner lookups return indistinguishable 404s.
   The response's `guestAccessToken` is the same checkout secret, not a second credential.

Offer consumption, reservation insertion, persisted idempotency, and the
`ReservationRequested` MassTransit bus-outbox message commit in one local transaction.
Database-generated integer passenger IDs are saved before publishing; messages carry
their canonical string representations and preserve opaque flight/cabin IDs exactly.
The inventory deadline is UTC now plus ten minutes, separate from offer expiry.
Reusing a guest-scoped request ID with an identical payload returns the existing
reservation even after offer expiry; changed payloads conflict (409). A consumed offer
cannot create another reservation. A database unique PNR index and at most three
insert attempts handle collisions without an availability-check loop.

Reads return application `FluentResults` with typed stable error codes, translated to
ProblemDetails. Validation failures return 400, missing resources 404, expected
checkout conflicts 409, exhausted PNR allocation 503; unexpected errors propagate to
central safe exception handling. Queries do not use mediator validation pipelines;
read inputs are explicitly validated at the HTTP boundary.

The saga progresses through `AwaitingCapacity`, `ConfirmingAllocations`,
`Compensating`, `RequiresIntervention`, `Completed`, or `Cancelled`; terminal workflow
rows are retained. The API does not process payments. Broker/infrastructure delivery
is eventually consistent: poll secure reservation lookup rather than treating 201 as
an allocated seat or confirmed booking.

## Domain

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

`Reservation.Code` is a `PnrCode` value object. The application supplies it to
`Reservation.Create`, using `PnrCode.Generate()` for a cryptographically random
six-character uppercase alphanumeric candidate. `PnrCode.From(...)` restores an
existing code, accepting ASCII letters and digits only and canonicalizing letters
to uppercase; whitespace is rejected rather than trimmed. Generation does not
guarantee uniqueness: persistence must enforce a unique code constraint, with
bounded collision retries coordinated by the application. Those persistence and
retry mechanisms are implemented by `BookingDbContext` and the local checkout transaction.

The reservation starts as `Created`. Adding passengers and baggage is allowed only
in this state. `Reserve()` requires at least one passenger and transitions to
`Reserved`; the application calls it after securing inventory. Payment completion
is not a prerequisite enforced by this initial domain model. Created and reserved
reservations can transition to `Cancelled`; cancelled reservations cannot be
edited or reserved again. Cancellation retains the booking and payment history;
it does not itself release inventory or issue refunds.

## Fares and baggage

Booking owns the bounded versioned commercial fare catalog and explicit flight prices.
Aircraft layouts and physical cabin definitions live in flight-service; seat assignments
live in seating-service. Catalog cabin IDs are opaque references, not cross-service
navigation or foreign keys stored on the flight-price mapping. Booking constructs a `FareQuote` per passenger per flight and
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
rebooking and retention policies are deferred. Persistence and durable integration
message delivery are implemented. Future travel status should be passenger-and-flight-specific.

## Persistence

Children expose private-set parent IDs and navigations: passengers, payments,
itinerary flights, and passenger fares reference their owner; baggage references its
passenger and the passenger fare it was charged against. `BaggageAllowance` and
`FareQuote` are immutable value objects (the allowance is copied into each
`PassengerFare`; map it as an owned type). `Reservation.AddPassenger` copies each input quote into
an exclusive snapshot, so reusing a quote does not share a persisted child between
passengers. The unattached input quotes are not part of the persisted graph.
New entities have default IDs until persistence assigns them; the connected
navigations allow EF relationship fixup to propagate generated keys.

Infrastructure explicitly maps collection backing fields using field access,
ignore the derived `FlightIds` projection, `TotalAmount`, and domain events, convert status through
`ReservationStatus.From`, and map `PassengerFare` as a required dependent of both its passenger
and its itinerary flight (avoiding multiple cascade paths).
Itinerary order must use `SequenceNumber` rather than database row order. Configure
user IDs as externally supplied, appropriate precision/lengths, and unique booking
codes, payment references per reservation, and flight/sequence keys per reservation.
Map `Reservation.Code` to its six-character `Value` with a value converter that
restores it through `PnrCode.From`.

Command loading must include passengers, their fares and baggage, and the other children required by domain invariants.
Concurrent mutations need aggregate-level concurrency control, including child-only
payment additions; database constraints alone cannot enforce the payment sum cap.
No payment-write endpoint is exposed; aggregate-wide concurrency for future payment
or baggage mutation APIs remains required before enabling those operations.

## Validation status

`BookingCheckoutTests` contains SQLite integration coverage through actual DispatchR
handlers, booking services, EF mappings, and a real MassTransit bus outbox with an
in-memory transport registration. It checks pricing/version snapshots, UTC expiry,
guest isolation, idempotency, and generated passenger IDs in persisted messages.
Test execution is deferred at the user's request; PostgreSQL/RabbitMQ/Redis live
integration validation is also deferred. Source/build validation is distinct from
runtime behavior verification.
