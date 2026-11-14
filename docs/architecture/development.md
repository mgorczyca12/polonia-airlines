# Development and maintenance

[Documentation index](README.md)

## Prerequisites and local startup

- .NET SDK 10 for backend projects.
- CI uses Node.js 26 and pnpm 12 for the web application.
- Restore dependencies before using `--no-restore` commands.
- For flight startup: provision PostgreSQL and RabbitMQ, and create the database
  schema including `InboxState`, `OutboxMessage`, and `OutboxState`.
  Initial migrations are checked in for all three services; apply them using the
  [local EF migration commands](database-deployments.md#current-local-migration-commands)
  before starting the hosts. Do not treat the table mappings as a
  deployed schema. The host does not automatically migrate or create databases.
- Existing flight databases also need the configuration-owned seat-map schema,
  optional flight aircraft FK, and backfilled unique row numbers described in the
  [flight-service schema notes](services/flight-service.md#aircraft-configuration-and-seat-map-ownership).
  `EnsureCreated` cannot upgrade an existing database.
- Flight configuration-management and aircraft-assignment APIs have no access
  control yet. Keep them restricted to trusted development until production
  authentication and authorization are implemented.

Configure the following through environment variables, user secrets, or your
deployment secret provider (do not commit credentials):

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__FlightDatabase` | PostgreSQL connection string |
| `RabbitMq__Host` | Broker hostname; defaults to `localhost` |
| `RabbitMq__Port` | Optional AMQP port; defaults to `5672` |
| `RabbitMq__VHost` | Virtual host; defaults to `/` |
| `RabbitMq__User` | Broker user; checked-in local default is `polonia` |
| `RabbitMq__Pass` | Required broker password; no checked-in default |
| `RabbitMq__UseSsl` | Enable TLS when required by the broker |

RabbitMQ credentials are validated at startup. Outbox delivery runs as a hosted
service and needs the configured database schema and broker to be available.

From the repository root:

```powershell
dotnet restore polonia-airlines.slnx
dotnet run --project backend\flight-service\flight-service.csproj
```

Other hosts can be run with their respective booking/seating project paths.
Use launch-profile/console URLs. Compose supplies Nginx for the web application;
its TanStack Start API layer calls private backend services server-side.
Flight data reads use the configured PostgreSQL database.
Booking exposes the guest checkout API; seating consumes inventory workflow
messages and exposes a liveness-only `/health` endpoint.

For the web application:

```powershell
Set-Location frontend\polonia-web
pnpm install --frozen-lockfile
$env:WEB_SESSION_PASSWORD = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
$env:WEB_ORIGIN = 'http://localhost:3000'
$env:FLIGHT_API_URL = 'http://localhost:5114'
$env:BOOKING_API_URL = 'http://localhost:5180'
pnpm dev
```

The API URLs above match the `http` launch profiles; use the actual service URLs
when choosing other profiles. The web server does not disable TLS validation or
follow upstream redirects. For HTTPS backend profiles, trust/configure the
development certificate instead of bypassing validation.

The web flow now requires the backend hosts, initialized schemas and coherent
business data. Provision actual flight rows, booking products/USD flight prices
and seating capacity/seat membership using the owning services' canonical IDs.
Flight/cabin references in prices and seating must match exactly. There is no
automatic business seeding, live aircraft-to-inventory synchronization or public
administration API added by this integration. See the
[booking](../../backend/booking-service/README.md) and
[seating](../../backend/seating-service/README.md) provisioning prerequisites.

`WEB_SESSION_PASSWORD` must contain at least 32 random characters; all web
instances must share a stable externally managed value. Generating a new one
invalidates existing guest sessions. Never expose it as a `VITE_` variable.
`WEB_ORIGIN` is the browser's exact origin (no trailing slash) and guards mutations;
HTTPS enables Secure cookies. HTTP is allowed only on localhost/127.0.0.1.
The remaining API URLs are server-only configuration, not browser URLs.

Compose obtains the password and browser origin from
[`.env.example`](../../.env.example), and supplies internal service URLs automatically.
Adjust `WEB_ORIGIN` if changing `WEB_PORT` or putting TLS in front of Nginx.
Existing Nginx proxying handles `/api/web/*` through the web server; no direct
backend mutation proxy or browser CORS policy is required.

## Verification

Backend telemetry is opt-in and disabled during normal tests. See
[observability](observability.md) for the shared Collector overlay, local ingest-key
configuration and privacy limits. Honeycomb MCP authentication does not replace
the ingest key; never embed it in code or application/frontend settings.

For the agreed schema-update workflow, see [EF Core database deployments](database-deployments.md)
and the [migration authoring skill](../../.github/skills/ef-core-migrations/SKILL.md).
Initial migrations and design-time tooling are implemented. Dedicated migration-matrix CI, bundles,
and deployment jobs are planned, not yet implemented.

The [CI workflow](../../.github/workflows/ci.yml) has `backend` and `frontend` jobs.
Backend checks run formatting, Release build and tests:

```powershell
dotnet format whitespace polonia-airlines.slnx --no-restore --verify-no-changes
dotnet format style polonia-airlines.slnx --diagnostics IDE0011 IDE0021 IDE0022 --no-restore --verify-no-changes
dotnet build polonia-airlines.slnx --no-restore -c Release
dotnet test polonia-airlines.slnx --no-build -c Release
```

The tests use xUnit/VSTest. Flight API integration tests configure in-memory SQLite
and disable only MassTransit hosted services to avoid external dependencies.
Shared outbox tests persist actual EF outbox records using an unstarted in-memory
bus and verify commit/rollback behavior; neither suite verifies live
PostgreSQL-to-RabbitMQ delivery. Booking also has checkout and in-memory saga tests.
Seating's enabled `ExternalInventory` tests use real PostgreSQL/Redis.
Booking's [ExternalWorkflow tests](../../backend/tests/integration/booking-service.integration.test/ReservationBrokerWorkflowTests.cs)
start both production hosts in-process with real PostgreSQL, Redis, and RabbitMQ;
only connection settings, logging, and bounded bus startup/shutdown waits are overridden.
They apply the checked-in migrations to two fresh databases, provision a catalog
and itinerary capacity before starting hosts, and exercise checkout, offer,
reservation creation and authenticated polling through HTTP. Two passengers on
two flights either complete with four durable allocations and Redis cleanup, or
fail capacity acquisition and compensate to Cancelled with no allocations/holds.
Both scenarios verify saga/inbox persistence, exact opaque references, and
idempotent reservation replay. They do not simulate process crashes or broker outages.

The [recovery tests](../../backend/tests/integration/booking-service.integration.test/ReservationRecoveryTests.cs)
use the same isolated infrastructure but launch actual booking/seating `dotnet`
child processes from the built service assemblies. They forcibly terminate
specific processes without graceful shutdown and verify:

- An unacknowledged Hold is requeued/redelivered after seating dies, and the
  persisted booking saga resumes after booking also restarts.
- A one-shot PostgreSQL serialization failure rolls back confirmation, production
  retries succeed, and booking recovers a queued outcome after dying before
  confirmation commits. Duplicate confirmations with reused and distinct message
  IDs leave exactly four allocations.
- Losing only the fixture's Redis hold key while seating restarts invalidates the
  hold and cancels the itinerary without reconstructing validity from SQL.

SQL locks and fixture-only triggers make interruption boundaries deterministic.
Recovery checks include drained queues with no error/skipped messages. All 17
booking integration tests passed locally on 2026-11-08. These cases cover specific
failure boundaries, not full PostgreSQL/Redis/RabbitMQ outages, broker restarts,
every crash window, or production failover. CI includes them through the existing
`ExternalWorkflow` category; hosted execution remains unverified.

The first hosted recovery run exposed delayed RabbitMQ management metrics:
queues may appear before `consumers` is reported. Readiness now waits for both
metrics rather than throwing on a missing field, with six deterministic regression
cases. Live reruns also exposed a concurrent duplicate-outcome inbox collision;
booking now retries that specific constraint plus transaction-concurrency errors,
not arbitrary uniqueness failures. All 23 booking integration tests passed
locally after these fixes; a hosted run of the fixes is still pending.

Backend CI provisions disposable PostgreSQL 18, Redis 8, and RabbitMQ 4 management
service containers and supplies all external-test settings. For local runs, use
**dedicated test infrastructure**, never deployed/shared databases or broker accounts:

| Environment variable | Value/requirement |
| --- | --- |
| `SEATING_TEST_POSTGRES` | PostgreSQL connection string; user needs `CREATE SCHEMA` for inventory tests and `CREATEDB` for workflow tests |
| `SEATING_TEST_REDIS` | Redis connection string, e.g. `localhost:6379` |
| `WORKFLOW_TEST_RABBITMQ_HOST` | AMQP hostname, e.g. `localhost` |
| `WORKFLOW_TEST_RABBITMQ_PORT` | AMQP port, e.g. `5672` |
| `WORKFLOW_TEST_RABBITMQ_USER` | Dedicated broker user able to create/delete virtual hosts and grant permissions |
| `WORKFLOW_TEST_RABBITMQ_PASSWORD` | Test broker password |
| `WORKFLOW_TEST_RABBITMQ_MANAGEMENT` | Management API base URL with trailing slash, e.g. `http://localhost:15672/` |
| `MASSTRANSIT_USAGE_TELEMETRY` | Set to `false` for test execution |

Workflow fixtures isolate each case in two randomly named databases, one broker
virtual host and one Redis key, then stop both hosts and delete those resources.
Recovery fixtures also terminate/dispose their exact child processes; they never
kill unrelated services or clear shared Redis keys.
Missing settings fail rather than skip or replace production transport/storage.

```powershell
dotnet test backend\tests\integration\booking-service.integration.test --filter Category=ExternalWorkflow
dotnet test backend\tests\integration\seating-service.integration.test --filter Category=ExternalInventory
```

Local unfiltered solution runs require all external settings above. To run without
external infrastructure, explicitly use
`--filter "Category!=ExternalInventory&Category!=ExternalWorkflow"`.

Backend test methods use `MethodName_StateUnderTest_ExpectedBehavior`, for example
`GetFlight_MissingFlight_ReturnsNotFoundWithIdentifiableProblemDetails`.
Use the operation under test, the relevant input or state, and the observable
outcome. Existing tests covering several operations name those operations together;
empty scaffold tests explicitly identify themselves as placeholders without assertions.

Frontend CI runs, from the web folder:

```powershell
pnpm install --frozen-lockfile
pnpm build
pnpm test --passWithNoTests
```

The final option permits an empty suite, but focused guest-session/API-client tests
now exist. Browser success, cancellation and refresh checks used an isolated mock
API fixture during frontend integration; a real browser-to-PostgreSQL/Redis/RabbitMQ
run remains pending. On 2026-11-13, the Docker-backed `ExternalInventory` and
`ExternalWorkflow` selection passed all ten tests: five inventory cases and five
booking broker-workflow/crash-recovery cases. They used disposable containers,
which were removed after execution, and did not exercise the web UI or
flight-service. Unit/mocked browser checks must not be described as proof of live
inventory allocation.

The proposed full-system Playwright suite is now set up in
[tests/system](../../tests/system/README.md), separately from backend integration
and frontend component tests. It provisions an isolated Compose stack and covers
browser booking success and insufficient capacity. It has **not yet been run**;
automatic CI execution is deliberately deferred until its first validated run.

## Backend readability convention

[backend/.editorconfig](../../backend/.editorconfig) applies only to backend C#.
[Scoped agent instructions](../../.github/instructions/backend-style.instructions.md)
also cover services, shared code and backend tests.

- Explicit control-flow braces and block-bodied methods/constructors.
- Concise simple auto-properties and short computed properties.
- Multiline long signatures, calls, query chains and complex conditions.
- Blank lines between validation, construction, state changes and return values.
- Preserve behavior and avoid abstractions or comments added only for formatting.

Omit `--verify-no-changes` from formatter commands to apply automatic fixes.
CI enforces supported whitespace and selected style diagnostics; naming, sensible
wrapping and logical grouping still require review. Frontend style is unchanged.

## Documentation ownership

The [documentation agent](../../.github/agents/documentation-writer.agent.md)
maintains this index, architecture, service and frontend references.
The [docs updater workflow](../../.github/workflows/docs-updater.md) runs after
non-Dependabot merges or on manual dispatch. Its safe outputs are currently staged
(preview mode), so do not assume a scheduled agent will publish changes.

Keep Mermaid charts inline with the relevant document. Distinguish C4 levels,
test-only storage, local UI flow and intended architecture from deployed behavior.
Do not add cross-service arrows until the corresponding integration exists.
Keep dated session summaries under [ai-chats](../ai-chats/), separate from authoritative
system documentation.
