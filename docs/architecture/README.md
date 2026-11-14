# System documentation

This is the current-system baseline, last updated on 2026-11-13.
It describes checked-in implementation, not a deployed production environment.

| Document | Contents |
| --- | --- |
| [Architecture](architecture.md) | Implementation status, service boundaries, C4 context/container/component charts, shared building blocks |
| [Flight service](services/flight-service.md) | Flight query API, validation, persistence, aircraft placeholders |
| [Booking service](services/booking-service.md) | Fare discovery, offers, guest reservations and completion workflow |
| [Seating service](services/seating-service.md) | Cabin capacity, overbooking, seat allocation, HTTP scaffolding |
| [Frontend](frontend.md) | Backend-connected one-way search, guest sessions, review and reservation status |
| [Development](development.md) | Local commands, verification, readability rules, documentation maintenance |
| [Observability](observability.md) | Opt-in backend OpenTelemetry, shared Collector, Honeycomb ingestion, privacy and verification |
| [Database deployments](database-deployments.md) | EF Core migration authoring and checked-in migrations; migration CI, release bundles and deployment automation are planned |

## Read this first

- The web application calls flight and booking through its server-only API layer.
  Only airport reference labels remain bundled. Payment is deferred.
- Flight configures PostgreSQL and a RabbitMQ EF bus outbox. External connection
  settings, credentials, and schema provisioning are required; integration tests use SQLite.
- Booking exposes catalog, checkout, offer, and guest-reservation HTTP APIs.
  Its reservation saga coordinates with seating over RabbitMQ; seating has no
  public inventory-management API.
- Flight is not part of the booking/seating reservation workflow. Matching flight
  prices and seating inventories must be provisioned through trusted tooling.
  Browser integration was checked against an isolated mock API; full live browser
  workflow verification remains pending.

[AI session records](../ai-chats/) preserve decisions and checks from individual work
sessions. They are historical context, not a substitute for the system documents above.
