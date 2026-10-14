# Flight service domain

## Dependency injection

`Application/DependencyInjection.cs` and `Infrastructure/DependencyInjection.cs`
provide the chainable `WebApplicationBuilder` extensions `AddApplication` and
`AddInfrastructure`. Both are called from `Program.cs` before the app is built.
These are preliminary entry points and do not register services yet. Add
application registrations (such as mediator handlers and validators) to
`AddApplication`, and persistence, repository, and query-service registrations to
`AddInfrastructure` when their implementations and configuration are ready.

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
