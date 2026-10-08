# Flight service domain

Flight segments, airport gates, aircraft configuration rows, and row positions
are exposed through read-only collection wrappers over initialized list backing
fields. Use `Flight.AddSegment`, `Airport.AddGate`,
`AircraftConfiguration.AddSeatMapRow`, and `SeatMapRow.AddPosition` rather than
mutating collections directly.

Addition methods reject duplicate instances and children belonging to a different
parent. Adding an unattached segment or position connects its parent navigation
and foreign key; new parent IDs remain default until persistence assigns them.
Rows and gates already receive their parent in their factories.

Future EF Core configurations should explicitly select field access for
`flightSegments`, `gates`, `seatMapRows`, and `seatMapPositions`. No DbContext,
database provider, or persistence mapping is introduced by these domain conventions.
