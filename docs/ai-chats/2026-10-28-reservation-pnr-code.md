# Reservation PNR value object

Previous session: [shared API exception handler and guest reservations](./2026-10-27-shared-api-exception-handler.md).

## Goal and completed changes

- Finished the user-started `PnrCode` value object with six-character ASCII alphanumeric validation, uppercase canonicalization, value equality, and string representation.
- Replaced time-based generation with cryptographic random generation across all 36 uppercase letters and digits.
- Changed `Reservation.Code` and its factory argument from `string` to `PnrCode`, keeping generation outside the aggregate factory.
- Updated reservation tests, added `PnrCodeTests`, and documented usage and future EF conversion requirements in the booking README.
- Preserved pre-existing guest-contact changes and unfinished controller/command work.

## Validation

- Normal booking unit-test build remains blocked by pre-existing missing request/response types and the unresolved `Range` attribute in `ReservationController`.
- All 61 booking unit tests passed using a temporary MSBuild import excluding only that unfinished controller, with output outside the repository. This does not verify the normal API build.
- Changed-file whitespace verification and solution style verification for IDE0011, IDE0021, and IDE0022 passed.
- Full-solution whitespace verification found pre-existing missing final newlines in the controller and two command files; these were left untouched. The PNR file's newline was fixed.
- `git diff --check` passed.

## Deferred and discussion

- Database PNR uniqueness, value conversion, and bounded application collision retries still need implementation alongside booking persistence.
- Discussed keeping pricing as a distinct feature within booking initially and using a booking-owned MassTransit saga for distributed reservation completion. These are recommendations, not implemented changes.

## Fare catalog and saga planning

Confirmed scope: fare catalog/pricing stays inside booking-service; "state pattern" means MassTransit saga state machines, not a shared domain-state framework. No implementation changes were requested or made during planning.

Proposed implementation order:

1. Complete booking's API/application/persistence foundation, fix the in-progress controller contract gaps, and enforce PNR uniqueness at insert with bounded collision retries rather than relying on the handler's current unbounded availability-check loop.
2. Introduce a Pricing feature with fare-product identity, cabin/brand, baggage terms, active/versioned products, and explicit flight/currency prices. Keep catalog products separate from purchased `PassengerFare` snapshots.
3. Add persisted, expiring server-priced offers identifying passenger/flight selections and exact product/price versions. Reservation creation validates ownership, expiry, and applicability and consumes the offer transactionally, with request idempotency.
4. Add durable seating capacity holds with expiry, idempotent hold/confirm/release operations, and concurrency protection. Availability reads alone cannot guarantee allocation.
5. Add booking-owned MassTransit saga state/persistence, RabbitMQ registration, durable scheduling, consumer outbox/inbox wiring, and correlation contracts. Reuse shared messaging helpers without putting business-specific saga logic in shared or MassTransit types in Domain.
6. Connect reservation creation to the saga through an atomic reservation/outbox transaction. Coordinate all itinerary holds and confirmation, track partial success, compensate failures, and handle duplicate, late, and out-of-order replies.
7. Test restart recovery, concurrency, retries, expiry races, compensation failure, and safe pending-status API responses. Payment authorization/capture integration remains a separately agreed milestone, not an implied prerequisite already supported by the model.

Open business decisions: initial price rules and fare conditions, offer and hold lifetimes, and whether first-release reservation completion includes payment or only committed inventory. No tests were run for this planning-only follow-up.

### Prior Redis decision recovered

Reviewed [the October 7 hold-storage decision](./2026-10-07-domain-persistence-preparation.md#hold-storage-decision): the user explicitly chose Redis for temporary holds and the database for confirmed allocations, superseding the earlier database-hold recommendation. The subsequent clarification specifically retains Redis for temporary physical-seat holds and separates durable cabin allocations from optional seat assignments.

That remains the agreed direction; today's SQL-hold recommendation was not an accepted change. Saga planning must preserve Redis owner/token-aware atomic acquisition and release, and specify the still-deferred cross-store confirmation/recovery protocol. Whether to hold unassigned cabin capacity temporarily in Redis also needs explicit design rather than assuming physical-seat holds cover it. No Redis implementation or tests were added.

## Reservation workflow implementation

The user approved implementation with these first-release rules:

- Pricing stays in booking; fare products include cabin/brand, baggage terms, active/versioned products, and explicit flight/currency prices.
- Offers expire after 15 minutes. Redis holds both unassigned cabin capacity and optional physical seats for 10 minutes.
- Reservations complete after inventory confirmation; payment, change/refund rules, and advance-seat-selection fees are deferred.
- After checking the environment, the user explicitly deferred tests. No test executions are authorized for this implementation follow-up; added test source and build checks must be reported separately from executed tests.

PostgreSQL, Redis, and RabbitMQ were not listening locally, and Docker Desktop's engine was not running; no shared infrastructure was started.

Implemented in code:

- Booking catalog and flight-specific physical-cabin pricing, immutable server-priced offers, guest capability-scoped access, atomic offer consumption/reservation creation/outbox publishing, durable request idempotency, and bounded PNR collision handling.
- EF-persisted booking saga with hold/confirm/release messages, a durable deadline dispatcher, compensation, intervention escalation, retained terminal records, and workflow status queries.
- Seating's Redis hold participant with SQL-confirmed allocations, provisioned seat membership, explicit global SQL serialization, and durable release fencing so late requests cannot resurrect cancelled work.
- Post-commit confirmed-inventory cleanup runs through a separate seating consumer. It validates the durable outcome and removes only the owner's Redis token; retries and eventual expiry conservatively retain capacity if cleanup fails.
- Service setup/bootstrap documentation, architecture diagrams, and automated test sources. Payment integration, authenticated administration, dynamic provisioning, and production schema upgrades remain outside this milestone.

Final verification:

- `dotnet restore polonia-airlines.slnx`: passed.
- `dotnet build polonia-airlines.slnx --no-restore`: passed, including all test projects; final incremental build reported 0 warnings and 0 errors. An earlier build reported the existing unread aircraft-controller mediator warning.
- Full solution whitespace verification and required IDE0011/IDE0021/IDE0022 style verification: passed.
- `git diff --check`: passed.
- No tests were executed in this implementation follow-up, per user instruction. Test sources cover offers, capability access, idempotency, saga transitions/deadline persistence, and inventory cleanup/recovery, but behavioral and live crash-recovery validation remain pending.

Operational limits: configure and provision PostgreSQL/Redis/RabbitMQ and trusted flight/cabin/seat data before use. Explicit empty-database bootstrap is documented; reviewed schema upgrades are still required for production evolution. Seating deliberately uses a global SQL mutation guard for correctness, so throughput optimization is deferred. The web application still uses local sample data.

## Reservation workflow refactor

Follow-up to the commit above, after review against the architecture guidelines and flight-service layout:

- Replaced booking's `BookingService`/`IBookingService`/`CatalogAdministration` god classes with feature command handlers (catalog, checkout/offers, reservations), repository abstractions, query services validated at the API boundary, and a typed `BookingError` with `BookingErrorKind`.
- Split the catch-all controller into catalog/checkout/offer/reservation controllers with a `BookingResultExtensions` ProblemDetails mapping; `Program.cs` now chains `AddApi/AddApplication/AddInfrastructure`.
- Domain: one entity per file on shared `AggregateRoot`/`Entity` bases; added `GuestToken` and `OfferedFare`; `FareQuote` now requires cabin ID, version and brand (no optional defaults).
- Persistence: per-entity `IEntityTypeConfiguration` classes plus a typed JSON converter for offer fares; repositories in `Infrastructure/Repositories`; sagas moved to `Infrastructure/Integrations/Sagas`.
- Seating was restructured in parallel by the same pattern (command handlers, `InventoryOperation` aggregate, repositories, thin consumers, Redis options); see its README. Its protocol was preserved; the operation state column is now a string and empty correlation IDs on confirm/release are validation failures.
- Removed bootstrap paths per the owner's decision: booking `--schema-script`, `ProvisionDevelopmentDatabase`, catalog seeding, and seating `--initialize-database`. Schema creation awaits migrations, as in flight-service.
- Split reservation contracts in shared into one file per message.

Verification (runnable tests only, as agreed): booking unit 85, seating unit 52, flight unit 46, booking integration 12 (SQLite/in-memory), flight integration 43, shared integration 43 all passed. A stale `Assert.Same` on the copied baggage allowance, left by the prior commit, was corrected. Whitespace and IDE0011/IDE0021/IDE0022 checks and `git diff --check` pass. Live PostgreSQL/Redis/RabbitMQ seating tests were compiled but not run (no Docker engine). Not run against real infrastructure: DispatchR/MassTransit wiring in the production host.

Behaviour notes: PNR allocation now pre-checks availability (bounded attempts) inside the command; a race is still stopped by the unique index but would surface as an error rather than a retry. Not committed.


## Commit and push

The user requested committing and pushing the accumulated local work, including
the shared exception handler, guest reservations/PNR, catalog/offers, inventory
participant, saga, tests, and documentation. Before committing, `git diff --check`
passed and `main` matched `origin/main` after fetch. Tests remain deferred.
