---
name: ef-core-migrations
description: 'Create and review service-owned EF Core PostgreSQL migrations. Use when adding a migration, checking model/snapshot drift, generating migration SQL or bundles, or planning a database upgrade. Does not authorize applying changes to deployed databases.'
argument-hint: 'Service, migration name, and intended model change'
---

# EF Core migration workflow

Follow the [database deployment design](../../../docs/architecture/database-deployments.md).
This skill guides authoring and validation; it does not implement CI/CD or grant
permission to change deployed databases.

## Scope and safety

- Identify the service, intended model change, and migration name. Ask when
  ownership, data preservation, or destructive changes are ambiguous.
- Read the worktree diff and preserve existing edits. Read backend style
  instructions before changing C#.
- Never automatically apply migrations to a deployed/shared database, edit applied
  migration history, delete data, or roll back with `Down()`.
- Do not automatically commit, push, or start unrelated infrastructure.
- Do not assume tooling, factories, or initial migrations exist just because this
  skill does. Implement prerequisites only within the approved task scope.

## 1. Inspect the service

| Service project | Context |
| --- | --- |
| backend\flight-service\flight-service.csproj | FlightServiceDbContext |
| backend\booking-service\booking-service.csproj | BookingDbContext |
| backend\seating-service\seating-service.csproj | SeatingDbContext |

Read the selected context, changed entities/configurations, existing migration
history and snapshot, service README, root central package policy, and local tool
manifest. Determine the last migration included in the target release.

For an existing database without migration history, stop and obtain a reviewed
baseline/schema-comparison plan. An empty database and an existing production
database are not interchangeable.

## 2. Check design-time prerequisites

- Use the repository-pinned `dotnet-ef`, aligned with EF Core, and matching
  centrally managed `Microsoft.EntityFrameworkCore.Design` development references.
- Use a design-time factory that constructs the selected Npgsql context without
  starting the application or its messaging/infrastructure services.
- Current factories require the service's `ConnectionStrings__...Database`
  environment variable. Restore the root `dotnet-tools.json` with `dotnet tool restore`;
  factories do not automatically load `.env`, appsettings, or user secrets.
- Restore only when required by dependency/tool changes or missing dependencies.
  Do not use `--no-build` until the relevant model changes have been built.
- Use an isolated local connection supplied through the approved configuration
  mechanism. Never hardcode or print secrets.

## 3. Generate, do not hand-author the snapshot

From the repository root in PowerShell, substitute the selected service, context,
and meaningful migration name:

```powershell
$project = 'backend\<service>\<service>.csproj'
$context = '<DbContext>'
dotnet ef migrations add <MigrationName> --project $project --startup-project $project --context $context --output-dir Infrastructure\Persistence\Migrations
dotnet ef migrations script <PreviousMigration> <MigrationName> --project $project --startup-project $project --context $context --output <SessionSqlPath>
```

Use `0` as the previous migration only for the initial migration. Save inspection
SQL outside tracked source unless it is an explicitly requested release deliverable.
Use EF tooling to generate migrations/snapshots; make necessary deliberate edits
to migration operations for renames, backfills, and safe ordering.

## 4. Review correctness and operational risk

- Confirm only the intended service/model changed.
- Inspect `Up`, `Down`, metadata, snapshot, and PostgreSQL SQL.
- Check renamed/dropped columns, nullability, defaults, data types, UTC timestamps,
  indexes, uniqueness, FK ownership, seed operations, and data preservation.
- Verify MassTransit inbox/outbox and booking saga mappings when affected.
- Initial seating schema must include its configured inventory mutation guard.
- Preserve opaque external IDs and avoid cross-service FKs.
- Check expand/contract compatibility with old application instances and consumers.
- Surface destructive operations, long locks/table rewrites, nontransactional SQL,
  or large backfills explicitly. Obtain a decision before implementing ambiguous
  data-loss or deployment behavior.

## 5. Validate against PostgreSQL

Build the selected service, then check:

```powershell
dotnet ef migrations has-pending-model-changes --project $project --startup-project $project --context $context
```

With permission to use an explicitly disposable, isolated PostgreSQL database:

- Apply all migrations to an empty database and verify constraints/seed rows.
- Apply again and verify no pending migrations.
- Test the previous release schema plus representative data upgrading to the new
  schema. For the first migration, verify empty-to-initial schema.
- Run the smallest relevant integration tests, using repository test tooling.
- Do not replace migration validation with `EnsureCreated` or SQLite-only tests.

If PostgreSQL is unavailable, report that live migration validation is blocked;
compilation or generated SQL inspection is not a successful migration test.
Clean only the specific temporary resources created for this validation.

## 6. Bundles when requested

Build reviewed migrations into a bundle for the deployment runner's runtime and
architecture; do not infer a production target. For example, Linux x64:

```powershell
dotnet ef migrations bundle --project $project --startup-project $project --context $context --self-contained --target-runtime linux-x64 --output <BundlePath>
```

Do not embed credentials. Verify necessary runtime configuration accompanies the
artifact; test it in the matching environment. Deployment jobs apply bundles
with schema credentials, never application startup.

## 7. Report

List the service/context, migration and baseline, changed files, SQL/data risks,
checks actually executed, and deployment prerequisites. Distinguish reviewed
artifacts from applied database changes. Update the session summary using the
repository's session-summary workflow.
