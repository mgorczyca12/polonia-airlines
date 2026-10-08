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
