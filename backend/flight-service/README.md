# Flight service domain

## Dependency injection

`Application/DependencyInjection.cs` and `Infrastructure/DependencyInjection.cs`
provide the chainable `WebApplicationBuilder` extensions `AddApplication` and
`AddInfrastructure`. Both are called from `Program.cs` before the app is built.
`AddApplication` registers the search validator; `AddInfrastructure` registers
the scoped database context and flight query service. `Api/Extensions/DependencyInjection.cs`
registers controllers, authorization, OpenAPI, and centralized ProblemDetails
exception handling through `AddApi`.

No production database provider or connection has been selected. The host can
start, but flight reads require provider configuration; without it they return
a safe 500 problem response and log the configuration exception. SQLite remains
test-only. Mediator and repository registrations remain deferred.

## Flight reads and error handling

Existing routes are preserved:

- `GET /api/Flight/flight/{flightId}` accepts a positive integer service-owned ID.
  Returns a response DTO on success and 404 ProblemDetails with
  `code: flight_not_found` if absent; invalid IDs return 400.
- `GET /api/Flight/flights` accepts `DepartureAirport`, `ArrivalAirport`,
  `DepartureDate`, `Page`, and `PageSize`. Airports are exact uppercase three-letter
  IATA codes. `DepartureDate` is now a date-only `YYYY-MM-DD` UTC calendar date,
  rather than the scaffold's DateTime input. Page defaults to 1; page size defaults
  to 20 and must be between 1 and 100. Invalid inputs return field-specific 400
  ValidationProblemDetails before querying. Offset overflow is rejected.

Query interfaces return `Result<T>` over immutable application DTOs, never domain
entities. Controllers return the successful value, not the result wrapper.
Searches return an array (empty when unmatched) and use departure timestamp then
flight ID ordering before pagination. UTC filtering includes midnight at the
start of the requested date and excludes midnight of the following date.
The minimum/default date and maximum date are rejected to catch omitted dates
and prevent end-boundary overflow.

Stored timestamps are UTC; DTOs expose explicit zero-offset DateTimeOffset values.
Frontends and downstream applications handle local-time conversion. EF providers
that materialize DateTime without a Kind are interpreted as UTC, not server-local
time. This refactor does not change the domain timestamp types.

Direct queries use explicit FluentValidation at the HTTP boundary, not a DispatchR
pipeline. Single-flight ID validation uses MVC range/model-binding validation.
Unexpected exceptions propagate to the global API handler, are logged internally,
and return safe 500 ProblemDetails with a trace identifier. Request cancellation
continues through to EF without conversion into a failed result.

SQLite-backed HTTP integration tests cover response shapes, validation, missing
flights, UTC boundaries, stable pagination, untracked reads, cancellation,
OpenAPI metadata, and safe logged database failures.

## Domain collections

Flight segments, airport gates, aircraft configuration rows, and row positions
are exposed through read-only collection wrappers over initialized list backing
fields. Use `Flight.AddSegment`, `Airport.AddGate`,
`AircraftConfiguration.AddSeatMapRow`, and `SeatMapRow.AddPosition` rather than
mutating collections directly.

Addition methods reject duplicate instances and children belonging to a different
parent. Adding an unattached segment or position connects its parent navigation
and foreign key; new parent IDs remain default until persistence assigns them.
Rows and gates already receive their parent in their factories.

## Persistence

`Infrastructure/Persistence/FlightServiceDbContext` applies the entity-specific
`IEntityTypeConfiguration<T>` classes from its `Configuration` namespace.
All eleven entities are mapped, including airport gates. Collections use explicit
field access for `flightSegments`, `gates`, `seatMapRows`, and `seatMapPositions`;
domain events are not persisted.

Airport addresses are required owned values in the airport table. Aircraft state
is stored as its code and restored to canonical instances to preserve transition
guards. Row types retain combined flags through an integer conversion; operating
days are also stored as integer flags.

Owned children cascade on parent deletion. References to shared airports, aircraft
types, layouts, and cabins use restricted deletion; flight schedules also restrict
flight deletion. Unique indexes cover aircraft registration/ICAO24, aircraft type
ICAO code, airport codes, cabin code, layout type/code/version, gates per terminal,
segment sequence per flight, and position column per row.

SQLite integration tests validate model creation, graph persistence, generated-key
fixup, value conversions, deletion behavior, and segment-sequence uniqueness.
SQLite is a test-only provider. Production provider/connection registration and
migrations are not configured here. Seat-map rows currently have no explicit order
property; loading their collection does not guarantee cabin/row display order.
