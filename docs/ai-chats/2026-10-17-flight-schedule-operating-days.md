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

## CargoLayout reminder
- SeatMap extraction was committed and pushed as `f26c7ec`; the worktree was clean before this addition.
- Added [CargoLayout](../../backend/flight-service/Domain/Entities/CargoLayout.cs) as an unimplemented `Entity<int>` with a private constructor and a TODO covering configuration ownership, main/lower decks, container positions, bulk holds, and capacity restrictions. Flight load plans remain a separate future concern.
- No configuration navigation, DbSet, EF mapping, or runtime behavior was added.
- Flight-service build and full solution whitespace/style checks passed; the existing unread aircraft-controller mediator warning remains. `git diff --check` passed. Tests were not run for this behavior-free placeholder.
- Placeholder changes remain local and uncommitted.

## Configuration management and flight seat-map query

### Implemented
- Added [ConfigurationFeatures](../../backend/flight-service/Application/Features/ConfigurationFeatures/) with transactional create-configuration, active-state, set-row and remove-row commands, validators, typed errors and direct layout-query contracts. Added tracked configuration/aircraft repositories and the missing schedule repository.
- Wired shared DispatchR validation/transaction pipelines and required repositories into flight DI. Added narrow FluentValidation exception handling returning 400 validation problems; unexpected failures still surface as safe 500s.
- Added [ConfigurationController](../../backend/flight-service/Api/Controllers/ConfigurationController.cs) for configuration creation/read/activation and row editing/removal. Creation returns the generated ID and Location, flushing through Unit of Work inside the pipeline-owned transaction.
- Added nullable aircraft assignment to Flight, guarded assignment/reassignment commands and HTTP PUT, and GET `/api/Flight/flight/{flightId}/seat-map`. Missing flight returns 404; unassigned flight returns 409 `flight_aircraft_unassigned`. Inactive aircraft/configurations and type mismatches reject assignment.
- [ConfigurationQueries](../../backend/flight-service/Infrastructure/Persistence/Queries/ConfigurationQueries.cs) reads the assigned aircraft/configuration/map in one no-tracking query and maps application DTOs for UI consumption. Includes registration/configuration metadata, cabin information, row numbers and seat/non-seat positions. Rows sort by number; positions by aisle group and ordinal column. No booking or seat-availability data is implied.
- Added unique numbered rows, row/position editing/removal, duplicate-column guards and overflow-safe default numbering. PUT replaces a row's position list while retaining matching column IDs; deleting rows cascades positions.
- Updated [HTTP examples](../../backend/flight-service/flight-service.http) and architecture/development/service documentation. Preserved the prior CargoLayout placeholder without wiring it into persistence.

### Verification
- Flight-service build passed; only the existing unread aircraft-controller mediator warning remains.
- All 43 flight integration cases passed, including 13 new configuration/API cases covering real HTTP/mediator/transaction persistence, validation, generated identity/Location, row reconciliation/deletion, assignment/reassignment, missing resources, inactive/type-mismatch conflicts, empty maps, no tracking, cancellation and OpenAPI. PostgreSQL schema-generation assertions verify the new row index and assignment FK.
- 29 selected domain/validator unit cases passed, including 4 new seat-map editing/assignment invariant cases.
- Full solution whitespace and selected style verification passed; `git diff --check` passed. No dependencies were installed or restored.

### Limits and deployment follow-up
- No migration was created/applied. Existing databases need nullable `Flights.AircraftId` plus restricted-delete FK/index and a reviewed `SeatMapRows.Number` backfill before enforcing the unique `(SeatMapId, Number)` index, in addition to the prior map extraction schema.
- Shared configuration edits are live across referencing aircraft/flights, without immutable publishing/snapshots. Existing assignments remain readable after deactivation. Schedule-overlap/capacity checks, seat inventory reconciliation, and optimistic concurrency remain deferred; racing unique writes can still surface as safe 500s.
- New management endpoints have no authentication/authorization and must stay restricted to trusted development. SQLite integration tests and PostgreSQL schema generation do not verify live PostgreSQL/RabbitMQ delivery.
- All changes remain local and uncommitted; no push was performed.

## Observability direction and commit request
- User plans OpenTelemetry with Honeycomb for system observability, BigQuery for business analytics, and Grafana for dashboards. No telemetry or analytics implementation was added. Proposed separation: OTLP telemetry via a collector versus durable business events via the outbox and analytics ingestion; Honeycomb/Grafana integration capabilities still need verification.
- User requested committing and pushing the configuration/seat-map APIs, aircraft assignment, tests, documentation and CargoLayout placeholder directly to `main`.
- Reviewed the pending scope; `git diff --check` passed. The previously recorded 43 passing integration cases, 29 selected unit cases and formatting checks remain the validation baseline; tests were not rerun solely for committing.
- Commit and push outcome will be reported after execution.
  - Outcome: committed and pushed `3be751c` to `main`.

## Figma connection and frontend redesign (search and results)

### Decisions
- Connected the official Figma MCP server; it exposed five booking-flow frames (search, results, passengers, payment, confirmation) created from an Emirates-inspired prompt with placeholder "Aurel Airways" branding.
- User chose: keep **Polonia Airlines** naming with the design's layout, typography and colors; neutral image placeholders (Polonia photos to be supplied later); payment as a clearly labelled demo form later; this pass covers **search and results only**.
- Screens stay driven by bundled sample data. No invented fares, taxes, return flights or support claims; unavailable services are shown disabled.

### Implemented
- Design tokens (red `#A51D35`, gold `#AC8A51`, warm neutrals) with light/dark values, Mantine theme, and self-hosted Lora/Inter (`@fontsource`). Adding fonts also re-resolved `latest`-tagged TanStack packages to patch versions in the lockfile.
- New header/footer/shell, booking progress stepper, search recap, reusable booking layout and journey summary sidebar.
- Home: hero placeholder, search panel (tabs, trip type, airport swap, date range, travellers and cabin picker), service highlights, data-driven destination card.
- Results: UTC date filtering with ±2-day fare strip and nearest-date fallback, sorting, recommended flight banner, cabin tabs, fare cards, flight details, return-unavailable notice, price summary and continue to `/booking`.
- Shared, validated search parameters across `/` and `/flightlist` (including travellers and cabin) so "Modify search" prefills the form.
- Fixed the pre-existing search crash: Mantine 9 date pickers return strings, so `toISOString()` failed once a date was chosen.
- Updated [frontend documentation](../architecture/frontend.md).

### Checks
- `tsc --noEmit`, `pnpm lint`, `pnpm build` and `pnpm test --passWithNoTests` (no frontend tests exist) passed. Imported the search-param parser directly to keep the main chunk at 397 KB (baseline 357 KB) without the size warning.
- Production-preview checks in Edge (Playwright) at 1440 px and 390 px, light and dark: layouts rendered, fare selection updated the summary, and continue navigated to `/booking`. The scripted run was stopped before the search-form and empty-date checks finished; those interactions were not fully verified.
- Dev mode logged a hydration mismatch in TanStack devtools `data-tsd-source` attributes on the root document; devtools console piping re-logged it between browser and server in a growing loop, flooding the log and crashing the integrated browser.
- Fix in `vite.config.ts`: disabled devtools console piping and excluded `__root.tsx` from source injection. A headless Edge check on the dev server then loaded home and results with zero console errors, and fare selection enabled Continue. Type-check, lint, build and `git diff --check` passed again.

### Deferred
- Passenger details, payment (demo) and confirmation redesigns; replace photo placeholders; frontend tests.
- User requested committing and pushing the search/results redesign directly to `main`; outcome reported in the final response.
