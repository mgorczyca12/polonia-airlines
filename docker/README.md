# Local Docker stack

The root Compose file builds the three .NET 10 services, the TanStack Start
Node server, and an unprivileged Nginx reverse proxy. PostgreSQL 18, Redis 8,
and RabbitMQ 4 use persistent named volumes. Published ports bind to localhost
only. This is local development configuration, not a production deployment.

## Prerequisites

- Docker Desktop running Linux containers.
- Copy [`.env.example`](../.env.example) to `.env` at the repository root and
  supply PostgreSQL and RabbitMQ passwords. Keep `.env` out of source control.
  Use connection-string-safe passwords (for example random letters and digits);
  the PostgreSQL password is interpolated into an Npgsql connection string.
- Backends use Apache-2.0 licensed MassTransit 8.5.12; no MassTransit license
  key is required. Compose disables anonymous MassTransit usage telemetry.
- Apply the checked-in initial EF migrations before using backend workflows.
  The hosts deliberately do not create or migrate schemas, and Compose has no
  migration jobs yet. Follow the [migration commands](../docs/architecture/database-deployments.md#current-local-migration-commands)
  with local connections to each database. Migrations include saga/inbox/outbox
  tables, uniqueness constraints, and seating's inventory mutation guard.
  See the [booking](../backend/booking-service/README.md) and
  [seating](../backend/seating-service/README.md) provisioning requirements.

## Starting

Run commands from the repository root:

```powershell
Copy-Item .env.example .env
# Edit .env before continuing.
docker compose config --quiet
docker compose up -d --wait postgres redis rabbitmq
```

PostgreSQL creates `polonia_flight`, `polonia_booking`, and `polonia_seating`
on the first initialization of its volume. These databases are empty: apply
reviewed schemas and provision trusted flight/cabin/seat/catalog data separately.
The initialization script does not run again against an existing volume.

After schema provisioning:

```powershell
docker compose up -d --build --wait
docker compose ps
docker compose logs -f booking-service seating-service
```

To run only the current sample-data frontend (no schemas needed):

```powershell
docker compose up -d --build --wait polonia-web nginx
```

The shared backend Dockerfile accepts `SERVICE` as a build argument and preserves
the root central NuGet package policy. All application runtime images run as
non-root users. Frontend production startup uses srvx to serve client assets and
forward dynamic requests to the built TanStack Start fetch handler.

## Endpoints

| Component | Default local endpoint |
| --- | --- |
| Web through Nginx | http://localhost:8080 |
| Flight API | http://localhost:5101 |
| Booking API | http://localhost:5102 |
| Seating liveness | http://localhost:5103/health |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |
| RabbitMQ AMQP | localhost:5672 |
| RabbitMQ management | http://localhost:15672 |

Override host ports in `.env` if they conflict with existing local services.
Container-to-container connections use service names and standard internal ports.
Nginx forwards the frontend's routes and assets; no SPA/static conversion is made.
The web app still uses sample data and is not newly wired to backend APIs.

Backend checks verify only that the HTTP listener is reachable, not schema
readiness or successful business workflows. Nginx's `/health` checks Nginx only;
the Node server has a separate page-response health check. Backends run in
Production, so development OpenAPI endpoints are not exposed.

Local endpoints use HTTP, with no TLS termination added. Do not use real guest
tokens, deployed credentials, or sensitive customer data in this setup. Production
needs HTTPS, proper proxy trust configuration, private infrastructure, separate
database roles, secret management, and reviewed deployment migrations.

## Stopping

```powershell
docker compose down
```

This retains data volumes. `docker compose down --volumes` permanently removes
this stack's databases, broker state, and Redis data; use it only when deliberately
discarding this local stack. Changing passwords in `.env` does not update accounts
already stored in PostgreSQL or RabbitMQ volumes.
