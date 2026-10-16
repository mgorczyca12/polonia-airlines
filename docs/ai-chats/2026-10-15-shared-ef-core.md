# Shared EF Core references - 2026-10-15

## Goal and decisions

- Add EF Core dependencies and implement opt-in shared database transaction and execution-strategy handling.
- Keep `IUnitOfWork` non-generic; its implementation takes a typed `DbContext`.
- Flight command handlers and service-level application registration were discussed as examples, not implemented in this session.
- Implemented opt-in command transaction policy rather than automatically treating every command as transactional or replay-safe.
- Plan to use MassTransit for asynchronous Kafka / Service Bus messaging and, if needed, saga coordination. Transport-specific outbox integration remains to be verified.
- Keep cross-resource orchestration and compensation explicit; database transaction eligibility does not by itself guarantee that a handler can safely be retried.

## Completed changes

- Added `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational` references to [shared.csproj](../../backend/shared/shared.csproj).
- Added central versions at `10.0.12` in [Directory.Packages.props](../../Directory.Packages.props), matching the existing SQLite version.
- Added `ITransactionalCommand<TCommand, TResponse>` and its non-generic marker. DispatchR 2.3.1 attempts to close pipeline types for every request, so the pipeline checks eligibility at runtime and resolves the unit of work only after opt-in.
- Updated transaction handling to preserve execution and rollback exceptions and avoid duplicate rollback attempts.
- Completed transaction ownership/disposal and nested-transaction guards in the user-created [EfUnitOfWork](../../backend/shared/Infrastructure/Persistance/EfUnitOfWork.cs). Added scoped `AddSharedUnitOfWork<TContext>` registration.
- Added a shared integration-test project to the solution and documented eligibility, registration, retry limitations, and multi-resource workflows in [architecture](../architecture/architecture.md).
- Preserved pre-existing untracked flight command drafts.
- Renamed all 71 backend test methods across 12 source files to `MethodName_StateUnderTest_ExpectedBehavior`, including shared transaction tests. Kept test bodies, attributes, and data unchanged; empty scaffold tests explicitly identify themselves as placeholders without assertions.
- Documented the test naming convention in [development](../architecture/development.md).
- Added `IRetryableCommand<TCommand, TResponse>` (also transactional), `ICommandExecutionStrategy`, an EF provider-backed implementation, and an execution-strategy mediator pipeline. `AddSharedUnitOfWork<TContext>` now registers both persistence services.
- Retry attempts redispatch in fresh async scopes with new handlers/contexts. Commit-attempt, rollback-failure, validation, and cancellation errors bypass provider replay and propagate explicitly; ambiguous commit outcomes need application reconciliation, not automatic replay.
- Added guards against nested command transactions/retry boundaries and documented isolated scope/input/response requirements. Preserved the user's separate flight infrastructure registration change.

## Verification

- `dotnet build backend\shared\shared.csproj --nologo`: succeeded with zero errors and one CS9113 warning for the unused `dbContext` parameter in the existing unit-of-work stub.
- During the dependency-only change, reviewed the diff; no C# files were changed at that stage.
- Subsequent implementation: `dotnet test backend\tests\integration\shared.integration.test\shared.integration.test.csproj --nologo --no-restore`: 13 tests passed. Covers real mediator routing, validation ordering, SQLite commit/rollback, scope sharing, transaction disposal/ownership, cancellation, execution failures, and rollback failures without handler retries.
- Initial test runs exposed DispatchR's generic-constraint registration limitation and a missing test-context registration; both were corrected before the passing run.
- Full-solution whitespace verification found existing formatting issues in flight command drafts plus a missing newline in the unit-of-work file. Fixed the shared file; scoped whitespace verification for Shared and its tests passed. Full-solution style verification for IDE0011/IDE0021/IDE0022 passed.
- The earlier build warning described the prior stub; subsequent shared test builds completed without that warning.
- After test renaming, `dotnet test polonia-airlines.slnx --no-restore --nologo`: 116 test cases passed across five nonempty projects. Booking/seating integration projects contain no tests. Build reported an existing unused `mediator` parameter warning in `AircraftController`.
- Naming audit: all 71 test methods match the required three-part pattern; tracked test diffs contain only method-declaration changes.
- After renaming, backend-test whitespace verification, full-solution style verification, and `git diff --check` passed. Full-solution whitespace still fails only on untouched flight command drafts.
- Execution-strategy implementation: `dotnet test backend\tests\integration\shared.integration.test\shared.integration.test.csproj --no-restore --nologo`: all 30 shared cases passed. Tests use a real EF execution strategy with deterministic transient classification over SQLite, not a Polly substitute.
- Verified transient begin/handler/save retries, fresh disposed attempt contexts and handler instances, retry exhaustion, transactional-only bypass, business/non-transient failures, pre/during-execution cancellation, nested-boundary rejection, rollback failures, and commit errors both before commit and after durable commit. The latter persisted exactly one record and did not replay.
- Final scoped Shared/test whitespace verification, full-solution selected style diagnostics, and `git diff --check` passed. Full-solution whitespace still reports only the existing flight-command draft issues. No full-suite rerun was needed for this shared-only addition; the 116-case run above preceded it.

## Deferred work

- Adopt opt-in commands and the shared registration in individual services, and configure their database providers.
- Flight command handling remains unfinished; shared transaction behavior was verified using dedicated test commands.
- Configure provider retry policies and adopt `IRetryableCommand` only for replay-safe operations. Automatic ambiguous-commit verification and durable command idempotency remain unimplemented; commit errors are deliberately not replayed.
- No MassTransit transport, outbox, inbox, saga, or blob compensation wiring was added.
