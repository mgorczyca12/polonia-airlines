# Per-passenger fares and baggage allowance (2026-10-13)

Context: [booking domain and persistence preparation](./2026-10-07-domain-persistence-preparation.md).
The session started 2026-10-09 and was resumed on 2026-10-13; the design and implementation were done on 2026-10-09.

## Goal

Booking had no place for fare products or baggage rules; `Fare` was a single reservation-wide
snapshot and `Baggage` had no allowance. Requirements: fares are per passenger, depend on the cabin
(aircraft layouts vary), baggage allowance is controlled by the fare, extra bags cost more up to a
hard limit.

## Decisions

- Aircraft layouts and cabins stay in flight-service and seat assignments in seating-service. The
  fare catalog belongs with cabin/layout data (flight-service or a future pricing service), not
  booking. Booking stores only opaque product, cabin, and flight identifiers plus immutable snapshots.
- Fare products are per cabin class and brand, not per aircraft layout; layout only drives
  availability.
- Fares are per passenger **per flight** (a passenger can fly different cabins on different legs);
  baggage allowance is counted per passenger per flight.
- Hard limit for now: `IncludedPieces + MaxExtraPieces` and per-piece weight are rejected. Included
  pieces are free, extra pieces are charged the fare's extra-piece price.
- Aircraft cargo capacity and moving excess baggage to another flight are deferred (flight-service
  inventory concern).
- Assumption (user was unavailable to confirm): proceeded with implementation after the user supplied
  these requirements.

## Changes

- Added `FareQuote` and `BaggageAllowance` value objects and the `PassengerFare` entity.
- Removed the reservation-level `Fare` entity. `Reservation` now has a `Currency` and derived
  `TotalAmount` (fares plus baggage charges); payments are capped against it.
- `Reservation.AddPassenger` requires one quote per itinerary flight; `AddBaggage` now takes a
  flight ID, and `Baggage` snapshots its charge and passenger fare.
- Updated the booking README and rewrote `ReservationTests.cs` for the new model.

## Checks

- `dotnet test backend\tests\unit\booking-service.unit.test`: 41 passed, 0 failed.
- `dotnet build` of the booking integration test project: succeeded, 0 warnings.
- Persistence mappings and controllers are not implemented; nothing was run against a database.

## Deferred

- Fare catalog and cabin/layout integration (flight-service or pricing service) and the application
  layer that builds `FareQuote`s.
- Aircraft cargo capacity check and overflow to another flight.
- EF mappings (owned `BaggageAllowance`, `PassengerFare` cascade paths) and controllers.

## Flight-service persistence (2026-10-13)

- Completed the user-created `FlightServiceDbContext` by applying eleven
  entity-specific configurations from `Infrastructure/Persistence/Configuration`
  and adding the missing `AirportGates` set.
- Mapped backing-field collections, required owned airport addresses, aircraft
  state codes, row-type/operating-day flags, unique indexes, and delete behavior.
  Aircraft state restoration retains canonical instances so existing transition
  guards still work after loading.
- Added five SQLite integration tests and a centrally versioned, test-only
  `Microsoft.EntityFrameworkCore.Sqlite` dependency. Updated the flight README.
- Validation: five persistence integration tests and five flight domain unit tests
  passed; `git diff --check` passed. VS Code reported no errors in the context or
  test file. The integration build reported two existing unused-mediator warnings
  in controller stubs.
- Production database provider/connection registration and migrations remain
  deferred. Seat-map rows still lack explicit display ordering. Existing controller,
  application, and booking work was preserved.
