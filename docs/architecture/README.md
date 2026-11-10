# System documentation

This is the current-system baseline, last reviewed on 2026-11-10.
It describes checked-in implementation, not a deployed production environment.

| Document | Contents |
| --- | --- |
| [Architecture](architecture.md) | Implementation status, service boundaries, C4 context/container/component charts, shared building blocks |
| [Flight service](services/flight-service.md) | Flight query API, validation, persistence, aircraft placeholders |
| [Booking service](services/booking-service.md) | Reservation domain, fares, baggage, payments, HTTP scaffolding |
| [Seating service](services/seating-service.md) | Cabin capacity, overbooking, seat allocation, HTTP scaffolding |
| [Frontend](frontend.md) | Routes, local data, search and booking-preview flow |
| [Development](development.md) | Local commands, verification, readability rules, documentation maintenance |
| [Observability](observability.md) | Opt-in backend OpenTelemetry, shared Collector, Honeycomb ingestion, privacy and verification |
| [Database deployments](database-deployments.md) | EF Core migration authoring and checked-in migrations; migration CI, release bundles and deployment automation are planned |

## Read this first

- The web application uses bundled JSON, not the backend APIs.
- Flight configures PostgreSQL and a RabbitMQ EF bus outbox. External connection
  settings, credentials, and schema provisioning are required; integration tests use SQLite.
- Booking exposes catalog, checkout, offer, and guest-reservation HTTP APIs.
  Its reservation saga coordinates with seating over RabbitMQ; seating has no
  public inventory-management API.
- The web application is not connected to the backend workflow, and flight is
  not part of the booking/seating reservation workflow.

[AI session records](../ai-chats/) preserve decisions and checks from individual work
sessions. They are historical context, not a substitute for the system documents above.
