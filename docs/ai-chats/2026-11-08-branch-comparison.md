# Branch comparison

## Goal and findings

- Reviewed prior session notes and explained local domain-event dispatch versus broker integration-event delivery. No runtime code changed.
- The user requested merging the remaining Dependabot npm branch into main and pushing.
- Fetched origin with pruning. Local main matches origin/main; the initial worktree was clean.
- The remaining branch has one unique dependency-update commit. The initial common-ancestor comparison misleadingly suggested these updates were missing from main.
- Direct comparison confirmed main already has equal or newer manifest versions, including newer Mantine, router plugin, lucide-react, Sass, and Vite versions. PR #30 contains subsequent frontend updates.

## Outcome and checks

- No merge or push was performed: the older dependency branch is superseded and should not replace newer main dependencies.
- Only this summary was added. No builds or tests were run because dependency files were not changed.
- GitHub CLI is unauthenticated; remote branch inspection used git successfully.

## Branch cleanup

- At the user's request, deleted the obsolete npm Dependabot branch from origin.
- `git ls-remote --heads origin` confirmed main is the only remaining remote branch; local branch references agree.
- No changes were pushed to main. The associated PR status was not separately inspected.

## Docker and Compose setup

- User selected all three backend services, PostgreSQL, Redis, RabbitMQ, and
  Nginx in front of a Node frontend server, preserving TanStack Start SSR.
- Added a shared parameterized backend Dockerfile, frontend and Nginx
  Dockerfiles, Nginx proxy configuration, Compose, build-context exclusions,
  local environment template, and PostgreSQL database initialization SQL.
- Added srvx and a production start script; retained existing dependency
  resolutions rather than accepting incidental devtools updates.
- Documented local startup, persistent volumes, HTTP-only limitations, and
  prerequisites. User explicitly limited scope to containers, not migrations.
- Frontend local production build, all five application image builds, and
  Compose configuration validation passed. Isolated container checks returned
  HTTP 200 for home, nested about route, and client JavaScript asset.
- PostgreSQL/Redis/RabbitMQ became healthy; confirmed all three service
  databases were created. Application schemas remain unprovisioned.
- Backend runtime validation failed: MassTransit 9 requires a license.
  Added MT_LICENSE environment forwarding and documented the requirement.
  Backend workflow/readiness validation remains pending a valid license and schemas.
- Initial smoke checks also encountered a PowerShell read-only variable and
  early listener probe; subsequent diagnostics identified the license blocker.
- Validation containers and volumes were removed; no development stack was
  left running. No commits or pushes were made.
- Final Compose validation and `git diff --check` passed; confirmed bash is
  available for backend listener health checks. The frontend test command
  exited successfully with `--passWithNoTests`, but found no test files.

## MassTransit v8 compatibility investigation

- User requested a feasibility report, not a dependency change. Repository
  package versions remain 9.2.4 and the Docker work remains intact.
- Verified [NuGet metadata for MassTransit.EntityFrameworkCore 8.5.12](https://api.nuget.org/v3/registration5-semver1/masstransit.entityframeworkcore/8.5.12.json):
  Apache-2.0 license, explicit net10.0 target, and EF Core Relational >= 10.0.0.
  It is the latest stable v8 version in the NuGet version index at inspection.
- Reviewed broker publishing/consumers, EF bus and consumer outboxes, pessimistic
  PostgreSQL saga persistence, state-machine activities, retries, and test harness
  usage. No v9-only application APIs were identified.
- In an isolated HEAD source copy, changed only the three MassTransit package
  versions to 8.5.12. Full Release solution build passed: zero errors and one
  existing unread aircraft-controller mediator warning. No C# edits were needed.
- Targeted v8 tests passed: domain-event/outbox 7, flight infrastructure 3,
  booking checkout and saga 9 (19 total). Tests used SQLite/in-memory transport;
  production RabbitMQ/PostgreSQL saga persistence and failure recovery were not
  executed. Usage telemetry was disabled for the compatibility test run.
- Proposed migration: align all three central MassTransit packages to 8.5.12,
  remove the v9 license requirement from Compose/template/docs, and validate
  real broker/storage behavior. No .NET or EF Core downgrade appears necessary.
- Compatibility does not imply identical v9 fixes or behavior; review release
  differences and persistent outbox/saga schemas before deployment.

## MassTransit v8 migration completed

- At the user's request, switched MassTransit, MassTransit.EntityFrameworkCore,
  and MassTransit.RabbitMQ to 8.5.12 in Directory.Packages.props. No C# changes
  or .NET/EF Core downgrade were needed.
- Removed MT_LICENSE from Compose and the environment template; updated Docker
  documentation. Compose explicitly disables MassTransit usage telemetry.
- Dependabot ignores major updates for MassTransit and MassTransit.* so future
  minor/patch maintenance does not reintroduce the commercial major version.
- Actual repository Release build passed (one existing CS9113 warning);
  all 19 targeted messaging/infrastructure tests passed again.
- Compose validation and all three rebuilt backend images passed. An isolated
  PostgreSQL/Redis/RabbitMQ stack started all three v8 backends without a license;
  listener checks passed and seating /health returned HTTP 200.
- As expected with empty databases, outbox workers reported missing OutboxState
  tables. Schema provisioning and real workflow/recovery testing remain pending;
  listener health is not business readiness.
- Removed all isolated validation containers and volumes. No commit or push.

## Commit and push follow-up

- User requested committing and pushing this session's Docker/Compose setup,
  frontend production server adapter, MassTransit v8 migration, Dependabot guard,
  and related documentation to main.
- Before committing, fetched origin and confirmed main matched origin/main;
  `git diff --check` passed. Earlier validation results remain as recorded above.

## Current Dependabot PR auto-merge diagnosis

- Inspected live open PRs #33 (gh-aw setup 0.91.9 to 1.0.1) and #36
  (srvx 0.11.22 to 1.0.8). Both backend and frontend CI checks passed.
- #33's automerge job was skipped: GitHub Actions dependency updates are
  intentionally excluded by the workflow's job condition.
- #36's automerge job ran successfully, but its merge step was skipped:
  only semver minor/patch updates are eligible; srvx is a major update.
  Successful workflow status does not mean auto-merge was requested.
- No workflow settings or PRs were changed. GitHub CLI remains unauthenticated;
  live evidence came from GitHub MCP check-run and job-step reads.
- User manually merged #33 and #36. Fetched origin and fast-forwarded local
  main to a2e961a, preserving this uncommitted summary update. No post-merge
  builds or tests were run; earlier Docker validation predates the srvx upgrade.

## EF Core database deployment planning

- User wants EF Core-based database updates automated across services through CI/CD.
- Confirmed current CI builds/tests only; no migrations, design-time context
  factories, or database deployment workflow are present.
- Recommended proposal, not implemented or approved: service-owned migrations
  under Infrastructure/Persistence/Migrations, design-time factories, pinned
  EF tooling, PostgreSQL migration checks in PR CI, and per-service migration
  bundles built with each release and executed by protected deployment jobs.
- Generate/review migrations during development, not automatically against
  production. Deployment applies the reviewed migrations using dedicated DDL
  credentials; application replicas do not migrate on startup.
- Serialize deployment per environment, use backward-compatible expand/contract
  changes across applications and consumers, fail deployment on migration errors,
  and avoid automatic Down-based rollback. Local Compose can use one-shot
  migration containers with the same artifacts.
- Production target/network access, environment approval policy, and handling
  any existing databases without migration history remain undecided.
- No implementation, schema changes, or validation commands for this proposal.

## Migration documentation and skill

- User approved the proposed EF Core deployment approach and requested recording
  it in authoritative docs plus a repository-shared migration skill.
- Added docs/architecture/database-deployments.md and linked it from the
  documentation index and development guide. Distinguishes the agreed target
  design from still-missing tooling, migrations, CI and deployment implementation.
- Added .github/skills/ef-core-migrations/SKILL.md for service-specific generation,
  SQL review, disposable PostgreSQL validation and optional bundle creation.
  The skill explicitly does not authorize deployed database updates.
- No schemas, tooling dependencies, application code, or workflows were changed.
- Skill frontmatter and new-document local links verified; `git diff --check`
  passed. No build/tests needed for these documentation-only changes. Not committed.

## Initial migrations completed

- User authorized creating all initial migrations using the skill and committing/
  pushing after successful validation. The live skill registry did not discover
  the newly created skill, so its SKILL.md was read directly and followed.
- Added root dotnet-tools.json pinning dotnet-ef 10.0.14, matching central
  Microsoft.EntityFrameworkCore.Design development references, and a design-time
  factory for each service. Factories require the service's connection-string
  environment variable and do not start HTTP/messaging hosts.
- Generated InitialCreate, designer metadata, and model snapshots for flight,
  booking, and seating using EF tooling. No business model changes were made.
- All three migrations applied to fresh isolated PostgreSQL 18 databases.
  Reapplication was a no-op, each history contained exactly one migration,
  inbox/outbox tables existed, and seating's mutation guard Id=1 was seeded.
- Model-drift checks passed for all three contexts. Reviewed additive SQL,
  booking PNR/request uniqueness, saga persistence, seating allocation uniqueness/
  filtered seat index and ordinal collations; domain event collections are not mapped.
- Release solution build passed with the existing unread mediator warning.
  45 targeted persistence/outbox/saga/inventory regression tests passed.
  Required whitespace/style verification and git diff checks passed.
- Early validation commands had PowerShell syntax/native quoting and EF flag
  mistakes; corrected commands passed. No application migration failure occurred.
- Updated service/Docker/architecture setup docs to reference the supplied initial
  migrations. Bundles, migration CI/CD, and Compose migration jobs remain deferred.
- Disposable PostgreSQL containers/volumes were removed. Initial migrations are
  for empty databases; no deployed/shared database was modified or baselined.
- Fetched origin and confirmed main matched origin/main before the requested commit.

## Seating external inventory tests

- User requested enabling and running the existing Redis/PostgreSQL inventory
  tests, not the full booking/broker workflow milestone.
- Removed all five permanent Fact skips. Missing configuration still fails with
  setup instructions rather than silently skipping.
- Backend CI now provisions PostgreSQL 18 and Redis 8 service containers and
  supplies the test environment variables; existing unfiltered tests include them.
- All five ExternalInventory tests passed against isolated local containers:
  zero failures and zero skips. Coverage includes concurrent last-capacity
  acquisition, SQL allocation survival after Redis hold loss, itinerary atomicity,
  owner-safe cleanup, expiry/release fencing, and acquire retry after SQL rollback.
- Updated seating/development docs with configuration and explicit local exclusion.
  Tests still use generated schema SQL and direct handlers; this is not broker
  delivery or migration testing. No production code was changed.
- Removed the validation containers and their anonymous volumes. Hosted GitHub
  Actions execution and full broker workflow validation remain pending.
- All 52 seating unit tests passed. Required whitespace/style verification and
  `git diff --check` passed. Changes are local; no commit or push was requested.

## Booking/seating real broker workflow tests

- User requested step 2: exercise booking API -> RabbitMQ -> seating -> booking
  saga with real infrastructure. Added `ReservationBrokerWorkflowTests` under
  the booking integration project, with an aliased seating project reference to
  keep the services' global Program types separate.
- Both actual ASP.NET Core hosts run in-process through TestServer. Production
  DispatchR, PostgreSQL repositories, Redis, RabbitMQ, EF saga persistence and
  inbox/outbox registrations remain intact. Tests override connection settings,
  logging and bounded bus startup/shutdown waits, not transports or hosted services.
- Each case creates two isolated databases, applies the checked-in migrations,
  seeds trusted catalog/inventory before host startup, and creates a private
  broker virtual host plus a unique Redis key.
- Two passengers on two flights complete as Reserved/Completed with four durable
  allocations, generated passenger references and eventual Redis cleanup.
  Insufficient capacity on one leg compensates to Cancelled with
  `inventory_unavailable`, zero allocations/counts and no leftover hold.
  Both cases assert durable saga/inbox state, opaque flight references and
  idempotent HTTP reservation replay.
- Backend CI now adds RabbitMQ 4 management and supplies all workflow-test
  settings alongside PostgreSQL 18 and Redis 8. Updated booking/seating READMEs,
  development setup/exclusion instructions and migration-coverage status.
- Both new broker cases passed repeatedly locally. Final Release booking
  integration run: 14 passed, 0 failed, 0 skipped. Required whitespace/style
  verification and `git diff --check` passed. No production code change needed.
- Verified fixture teardown: no workflow databases, only RabbitMQ's default
  virtual host, and zero Redis keys. Removed all three validation containers
  and anonymous volumes.
- Hosted GitHub CI execution, process crash/restart and outage/redelivery fault
  injection remain unverified/deferred. Migration deployment jobs and the
  dedicated three-service schema matrix remain separate work.
- User subsequently authorized committing and pushing steps 1 and 2 to main.
  Pre-commit fetch confirmed local main matched origin/main; `git diff --check`
  passed. The commit includes the verified tests, CI infrastructure and related docs.

## Process crash/retry recovery and provisioning design

- Steps 1 and 2 were committed/pushed as `26b450a`; main matched origin/main
  and the worktree was clean before this follow-up.
- User selected implementing step 3 first, then designing step 4.
- Added `ReservationRecoveryTests` and `WorkflowServiceProcess`, and extended
  the existing broker fixture for shared isolated resources, child-process
  startup/termination, queue inspection and duplicate-message publishing.
  Production code, dependencies, schema migrations and CI configuration did
  not change; CI already runs the `ExternalWorkflow` category.
- Actual service processes are force-killed without graceful shutdown. Tests
  verify requeued/redelivered unacknowledged Hold processing after seating dies,
  restoration of booking's persisted saga after booking also restarts, and
  eventual completion with exactly four allocations.
- A fixture-only PostgreSQL trigger causes one serialization failure during
  confirmation. Production retry preserves Held state/Redis validity while
  SQL allocations roll back. Booking is killed before confirmation commits,
  then restarts to consume the persisted broker outcome. Duplicate confirmations
  with reused/distinct message IDs do not allocate twice.
- Losing only the fixture's Redis hold key during seating restart causes
  `hold_expired_or_lost` cancellation with no remaining allocations/holds.
  No hold validity is reconstructed from SQL.
- Final Release booking integration run: 17 passed, 0 failed, 0 skipped.
  Required whitespace/style verification and `git diff --check` passed.
  Recovery queues drained without error/skipped messages. Verified no fixture
  databases, no non-default broker virtual hosts, zero Redis keys and no child
  services left running; removed validation containers/anonymous volumes.
- Updated booking/seating READMEs and development coverage/setup documentation.
  Full infrastructure outages, broker restart, all other crash windows and
  hosted GitHub CI execution remain unverified.
- User selected step 4's design: event-driven initial snapshot from flight-service,
  supporting initial creation and identical replay only, with layout/capacity
  changes rejected. Recorded this selected but unimplemented direction in the
  seating README. Trusted API callers/broker permissions, canonical seat IDs,
  snapshot revisions and existing-flight backfill need resolution during implementation.
- User subsequently authorized committing and pushing step 3 changes and step 4
  design notes to main. Pre-commit fetch confirmed main matched origin/main;
  `git diff --check` passed. Step 4 implementation remains deferred.

## CI failure investigation and fixes

- Inspected [CI run 42525404175](https://github.com/mgorczyca12/polonia-airlines/actions/runs/42525404175)
  for `2d09282`: frontend, restore, formatting and build passed; backend tests
  failed in the Redis-loss recovery case. The preceding `26b450a` CI run passed.
  GitHub CLI was unauthenticated; retrieved runs/jobs/logs through GitHub MCP.
- Root cause: newly visible RabbitMQ queues can lack the asynchronously reported
  `consumers` metric. Readiness used GetProperty and threw KeyNotFoundException
  instead of continuing its bounded wait. Fixed it with TryGetProperty and added
  six non-external regression cases for missing queues/metrics, inactive consumers
  and fully ready endpoints.
- Live validation revealed another intermittent failure: concurrent duplicate
  confirmation outcomes collided on booking's inbox unique constraint
  `AK_InboxState_MessageId_ConsumerId` and faulted because booking had no consumer
  retry policy. Added five two-second retries for that specific constraint,
  EF concurrency conflicts and PostgreSQL serialization/deadlock errors outside
  the consumer outbox. Other unique constraints are not retried.
- Enhanced recovery failure-queue diagnostics to identify faulted queue/message
  errors instead of reporting only a nonzero count. No schema/dependency changes.
- Six readiness cases passed; targeted duplicate-confirmation/retry plus readiness
  run passed all seven cases. Final Release booking integration run: 23 passed,
  0 failed, 0 skipped. Required whitespace/style verification passed.
- Verified fixture cleanup: no workflow databases/non-default vhosts or Redis
  keys. Removed validation containers and anonymous volumes. Updated booking
  README and development docs. User subsequently authorized committing and
  pushing these fixes to main. Pre-commit fetch confirmed main matched origin/main;
  `git diff --check` passed. Hosted validation of these fixes remains pending.
