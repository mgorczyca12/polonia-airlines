# Flight schedules and operating days

Previous session: [2026-10-16](2026-10-16-editor-indentation.md).

## Goal and rollover
- Carry forward all uncommitted OperatingDays and flight-schedule work at the user's request. No application code was changed during this rollover.
- Earlier indentation, shared repository, domain-event/outbox, and CI newline work was already committed and pushed. The newline repair was pushed as `e25c80e`.

## Decisions and implemented local changes
- Retain `OperatingDays` as a value object with `DayOfWeek` as its public day API and preserve the existing integer storage.
- [OperatingDays](../../backend/flight-service/Domain/ValueObjects/OperatingDays.cs): replaced named singleton days and the union operator with `FromDays(IEnumerable<DayOfWeek>)`; `HasDay` accepts `DayOfWeek`. Duplicates collapse; null/empty inputs and undefined days throw. Display names stay Monday-first.
- Preserve Monday bit 0 through Sunday bit 6. `FromValue` rejects masks outside 0..127; `None` remains a materialization/default sentinel.
- [Schedule](../../backend/flight-service/Domain/Entities/Schedule.cs): added a null guard; schedules still reject empty operating days.
- [Schedule command files](../../backend/flight-service/Application/Features/ScheduleFeatures/Commands/CreateSchedule/): typed day collection, value-object conversion, and collection/enum validation.
- [Unit tests](../../backend/tests/unit/flight-service.unit.test/OperatingDaysTests.cs) cover every day, all 127 nonempty masks, equality, duplicate handling, and invalid inputs. [Command validation tests](../../backend/tests/unit/flight-service.unit.test/CreateScheduleCommandValidatorTests.cs) cover typed day inputs.
- Updated [persistence assertions](../../backend/tests/integration/flight-service.integration.test/FlightServiceDbContextTests.cs) and [flight documentation](../architecture/services/flight-service.md).

## User work carried forward
- Pending changes to [IFlightRepository](../../backend/flight-service/Application/Abstractions/IFlightRepository.cs) and [FlightRepository](../../backend/flight-service/Infrastructure/Repositories/FlightRepository.cs) add flight-number lookup.
- New [IScheduleRepository](../../backend/flight-service/Application/Abstractions/IScheduleRepository.cs) and the schedule command remain untracked.
- At rollover, the [schedule handler](../../backend/flight-service/Application/Features/ScheduleFeatures/Commands/CreateSchedule/CreateScheduleCommandHandler.cs) now injects `IScheduleRepository` and calls `Add` with the new schedule. This is newer than the last test run; the earlier missing-add note is no longer current.

## Checks and actual results
- Previous day: 20 focused unit cases and 5 persistence integration tests passed; affected service/test projects built. Solution style verification and `git diff --check` passed.
- Previous solution whitespace verification flagged only existing whitespace/final-newline issues in the untracked schedule repository interface.
- Today: inspected `git status`, diff statistics, the previous summary diff, and the current schedule handler. No builds, tests, or format checks were rerun for this documentation-only rollover.

## Remaining work
- Validate the latest handler edits; verify concrete schedule repository implementation/DI and end-to-end command persistence rather than assuming those are complete.
- Resolve the known schedule repository formatting issues before committing.
- All changes described here remain local and uncommitted. No commit or push was performed during rollover.

## Schedule naming and feature move
- Renamed the domain entity and EF configuration from `FlightSchedule` to `Schedule`, including constructors, factory, repository type, and references.
- Renamed `CreateFlightScheduleCommand`, its handler, validator, and validator tests to `CreateScheduleCommand` equivalents. Moved the feature to `Application/Features/ScheduleFeatures/Commands/CreateSchedule` and updated namespaces.
- Renamed the context set to `Schedules`; retained the explicit `FlightSchedules` database table mapping to avoid an unintended schema change. Added an assertion verifying the retained table name.
- Preserved the user's existing repository-add step and other pending edits. Updated current documentation links to the renamed files.
- Validation: 20 targeted unit tests and 8 targeted integration tests passed; affected projects built. Full solution whitespace and selected style checks passed, as did `git diff --check`. The previously flagged schedule repository whitespace was corrected while updating its entity reference.
- Remaining production repository/DI wiring is unchanged by this rename. No commit or push performed.

## Query namespace move verification
- Checked the user's move of query errors to `Queries/Errors` and validators to `Queries/Validators`.
- Reproduced compilation failures from stale imports; corrected imports in application DI, the flight controller, the EF query service, and search-validator unit tests. The moved types' namespaces were already correct.
- Verification: 16 search-validator unit cases and 19 flight-query API integration cases passed. The service and both test projects built; only the existing unread aircraft-controller mediator warning remained.
- Full solution whitespace/style verification and `git diff --check` passed. No commit or push performed.

## Commit and push
- User requested committing and pushing the accumulated schedule, operating-day, query-folder, and documentation changes on `main`.
- Reviewed the pending tracked/untracked scope and confirmed `git diff --check` passed. The passing targeted tests and formatting checks above remain the validation baseline.
- Commit/push outcome is reported in the final response after this record is saved.

## SeatMap owned by AircraftConfiguration

### Decisions and changes
- Earlier schedule/query work was committed and pushed as `dda9cff`; the worktree was clean before this implementation.
- User chose to extract passenger seat maps as child entities of the configuration aggregate, without independent lifecycle/versioning. Cargo layouts remain deferred.
- Added [SeatMap](../../backend/flight-service/Domain/Entities/SeatMap.cs) as `Entity<int>`, created automatically by `AircraftConfiguration.Create`. It has no public factory, assignable owner, or separate repository.
- Moved the read-only row collection and row-add ownership/duplicate guards from configuration to the map. `SeatMapRow.Create` now requires a map and stores `SeatMapId`.
- Updated EF mapping to a required one-to-one configuration/map relationship with a unique owner FK; map deletion cascades to rows/positions. Aircraft still restrict configuration deletion and shared cabin/type reference data is retained.
- Added the `SeatMaps` context set and mapping; updated graph loading, collection tests, persistence tests, and PostgreSQL schema-generation assertions.
- Updated [flight documentation](../architecture/services/flight-service.md) with the new ownership/API and schema transition requirements.

### Validation
- 5 collection/ownership unit tests passed.
- Final targeted persistence/infrastructure run: 11 integration cases passed, covering generated keys, round trips, editing a loaded map, duplicate-owner rejection, cascade deletion, retained reference data, and PostgreSQL schema generation.
- Affected service/test projects built; the existing unread aircraft-controller mediator warning remains.
- Full solution whitespace/style verification and `git diff --check` passed.

### Deferred and deployment notes
- No migration framework/files exist in this repository yet. Existing databases require a reviewed data migration: create one map for each configuration, backfill row map IDs, then replace the old configuration FK/column. No live PostgreSQL database migration was performed.
- Configuration remains the aggregate boundary. Passenger configuration behavior is preserved; optional maps for cargo-only configurations and standalone map workflows were not introduced.
- User subsequently requested committing and pushing the SeatMap extraction. Reviewed the pending scope and confirmed `git diff --check` passed; the 16 passing targeted tests and formatting checks above remain the validation baseline. Commit/push outcome is reported in the final response.
