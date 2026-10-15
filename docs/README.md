# System documentation

This is the initial current-system baseline, reviewed on 2026-10-14.
It describes checked-in implementation, not a deployed production environment.

| Document | Contents |
| --- | --- |
| [Architecture](architecture.md) | Implementation status, service boundaries, C4 context/container/component charts, shared building blocks |
| [Flight service](services/flight-service.md) | Flight query API, validation, persistence, aircraft placeholders |
| [Booking service](services/booking-service.md) | Reservation domain, fares, baggage, payments, HTTP scaffolding |
| [Seating service](services/seating-service.md) | Cabin capacity, overbooking, seat allocation, HTTP scaffolding |
| [Frontend](frontend.md) | Routes, local data, search and booking-preview flow |
| [Development](development.md) | Local commands, verification, readability rules, documentation maintenance |

## Read this first

- The web application uses bundled JSON, not the backend APIs.
- Flight queries have API and EF Core implementations, but the normal host does
  not configure a database provider. Integration tests supply SQLite.
- Booking and seating have domain behavior but no wired business HTTP endpoints.
- No implemented cross-service messaging or request flow currently connects these services.

[AI session records](ai-chats/) preserve decisions and checks from individual work
sessions. They are historical context, not a substitute for the system documents above.
