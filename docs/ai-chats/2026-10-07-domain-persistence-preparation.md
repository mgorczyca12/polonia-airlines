# Domain persistence preparation and daily summary rollover

Previous context: [2026-10-06 summary](./2026-10-06-dependabot-docs-checkout.md).

## Goal and scope

- Reviewed booking EF Core compatibility and explained seating inventory, holds,
  and assignments (currently scaffolded). Recommended direct domain mapping
  rather than separate database models.
- User requested daily summary rollover and consistent read-only collections
  across services. User explicitly limited implementation to the Domain layer:
  no Infrastructure, DbContext, provider, migrations, or persistence mappings yet.
- Moved the October 7 review out of the October 6 summary. Updated the session-summary
  skill and repository instructions to roll over using the user's local date.

## Changes

- Booking children now have explicit private-set parent IDs and navigations.
  Reservation owns ordered ReservationFlight children with FlightId and
  SequenceNumber; FlightIds remains a sorted convenience projection.
- Reservation copies a supplied fare quote into an exclusive child snapshot.
  ReservationStatus.From reconstructs only known statuses and throws otherwise.
- Flight, Airport, AircraftConfiguration, and SeatMapRow collections now use
  initialized list backing fields and read-only wrappers. Addition methods reject
  duplicate instances and foreign parents; segments and positions get connected
  parent navigations. Airport.AddGate replaces direct collection mutation.
- Added booking relationship/status tests and flight collection tests, plus a
  flight-test project reference. Updated booking README and added flight README.
- Existing seating-service SeatInventory edits are unrelated and untouched.

## Persistence recommendations still deferred

- Map collection backing fields explicitly; ignore domain events and derived FlightIds.
  Configure status conversion, exclusive fare relationship, database-generated child
  keys, and externally supplied User IDs.
- Enforce unique booking codes, per-reservation payment references, itinerary flight
  and sequence keys, and appropriate scalar precision/lengths.
- Load the graph required by aggregate invariants. Protect concurrent mutations,
  including child-only payment additions, at aggregate level.
- Verify insertion, generated-key propagation, updates, ordering, and conversion
  with relational round-trip tests when Infrastructure is implemented.

## Checks and delivery

- Filtered VSTest runs passed: 29 booking ReservationTests and 4 flight CollectionTests.
  Both services and test projects built successfully. Initial parallel builds hit
  a shared-project output lock; reran sequentially after correcting a misplaced
  flight helper method. Editor test discovery was unavailable, so CLI tests were used.
- Scoped `git diff --check` passed for the changes in this task. The unscoped check
  flagged trailing whitespace in the user's unrelated SeatInventory edit, left intact.
  Checked editor diagnostics reported no errors. No EF persistence behavior has
  been verified; tests cover domain behavior and object graph relationships only.
- At the user's subsequent request, committed the domain, tests, documentation,
  and rollover policy as `eb6c046` and pushed to origin/main. Verified matching
  local and remote-tracking HEADs; seating-service edits remain unstaged and
  uncommitted. This delivery note is recorded in a follow-up documentation commit.

## Seating inventory capacity properties

- Added OverbookingAllowance (default zero), HeldSeats, derived BookingLimit,
  AvailableToBook, and actual OverbookedSeats. Replaced writable AvailableSeats
  with the derived availability and private-set state. FlightId is now an integer
  matching flight-service IDs. Added a validated factory and private ORM constructor.
- Allocation operations and analytics remain deferred; this change defines capacity
  state only. Added seating README and focused capacity tests with a service project
  reference.
- Editor diagnostics reported no errors in SeatInventory. The already-started CLI
  validation completed successfully (8 tests passed and whitespace check passed)
  before follow-up; user requested skipping tests, so no further runs were started.
- Changes remain local and uncommitted.

## Future cabin-specific inventory

- Discussed cabin-specific overbooking allowances. Deferred implementation while
  recording that capacity, holds, reservations, and allowance must eventually be
  tracked together per flight/cabin; splitting allowance alone is insufficient.
- Recommend physical cabin identity from the aircraft configuration, distinct from
  fare products. Policy/analytics supplies cabin-specific limits; cross-cabin upgrades
  or substitution need explicit rules rather than treating capacity as interchangeable.
- Current flight-wide inventory is a temporary simplification. Revisit cabin buckets
  before implementing cabin-specific allocation, even if all allowances default to zero.

## Hold storage discussion

- Redis can coordinate short-lived seat holds, but frontend freshness does not
  prevent races: backend acquisition must be atomic and owner/token-aware.
- Distinguished temporary Redis holds from durable confirmed allocations. Do not
  persist a second authoritative HeldSeats counter asynchronously without a
  consistency strategy; TTL expiration alone does not decrement a separate counter.
- Recommended choosing one authoritative allocation path before implementation:
  database-backed expiring holds with Redis/read notifications, or Redis-authoritative
  holds with explicit cross-store confirmation, recovery, and durability semantics.
- HeldSeats remains a useful domain concept, but its source and persistence mapping
  are pending. Physical-seat exclusivity and per-cabin booking capacity are separate
  constraints. No implementation or tests performed.

## Hold storage decision

- User chose Redis for temporary seat holds and the database for confirmed
  allocations only. This supersedes the earlier database-backed hold recommendation.
- Temporary holds will carry an owner/token and expiry, with atomic acquisition
  and owner-checked release. HeldSeats should reflect active capacity-consuming
  Redis holds, not a separately persisted database counter.
- Confirmation still requires hold ownership validation, an idempotent database
  write, and database enforcement of unique physical seat assignment per flight.
  Redis cleanup occurs after successful confirmation; stale holds must not make
  confirmed seats available. Detailed recovery and capacity coordination are deferred.
- Decision recorded only; no Redis/Infrastructure implementation or tests added.

## Seat selection and overbooking clarification

- Advance seat selection is distinct from eventual assignment; fare rules can
  govern selection timing/fees without requiring an assignment to confirm a booking.
- Redis remains the choice for temporary physical-seat holds. Durable confirmed
  cabin allocations must exist independently of optional physical-seat assignments,
  with atomic database enforcement of booking limits.
- Mandatory immediate physical assignment caps sales at physical capacity; true
  overbooking requires accepting some confirmed bookings without assignments.
- Blocked map seats do not create physical capacity; upgrades move capacity between
  cabins but cannot resolve total-aircraft overselling when all passengers show up.
- Suggested optional/deferred selection as the initial model, with fare entitlements
  later. No new domain or Infrastructure changes made for this discussion.

## External identifier convention

- Added repository convention: other-service references are opaque strings;
  local identities and internal foreign keys retain native types. Boundary mapping
  preserves canonical representations; non-blank validation and ordinal comparison
  do not silently trim, parse, or normalize references.
- Converted booking itinerary FlightId values, factory input, and FlightIds projection
  to strings. Local UserId and parent relationship IDs remain integers.
- Kept user's flight/cabin string references and confirmed-capacity-only seating
  model; tightened blank-ID validation and initialized FlightId for ORM materialization.
- Updated both service READMEs and test sources, including representation preservation
  and case-sensitive duplicate detection. String-array theory data uses MemberData
  after the initial InlineData approach failed compilation.
- Both affected service/test projects build with zero warnings/errors; editor checks
  found no domain errors and git diff whitespace check passed. Tests were not executed,
  honoring the earlier skip request.
- Preserved the user's deletion of SeatHold. No Infrastructure or contracts currently
  exist to map; database uniqueness, Redis holds, and allocation logic remain deferred.
  Changes are local and uncommitted.

## Confirmed seating allocations

- User selected SeatAssignment as a confirmed passenger/cabin allocation with an
  optional physical seat, rather than a separate capacity-allocation entity.
- Added inventory ownership with local parent ID/navigation and opaque external
  ReservationId, PassengerId, and optional SeatId. Mutations go through inventory
  methods; its assignment collection is read-only.
- Confirmation/release maintain ReservedSeats and enforce booking limits.
  Seat selection/clearing leaves booking count unchanged. Guards reject duplicate
  passengers/seats, excess physical assignments, and foreign allocation instances.
- Added focused test sources and updated seating README with loading requirements,
  layout/Redis boundary checks, and pending concurrency/database uniqueness work.
- Seating service and test project build with zero warnings/errors; whitespace
  check passed and editor diagnostics found no errors. Tests were not executed,
  honoring the user's skip request. No Infrastructure, Redis, commit, or push added.
