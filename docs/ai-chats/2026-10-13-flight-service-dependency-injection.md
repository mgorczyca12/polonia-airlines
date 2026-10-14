# Flight service dependency injection - 2026-10-13

## Goal and decisions

- Add preliminary Application and Infrastructure dependency-injection entry points.
- Use per-layer static `DependencyInjection` classes with chainable
  `WebApplicationBuilder` extensions.
- Keep both extensions empty for now; do not select a database provider or
  register unfinished services.

## Completed changes

- Added [Application/DependencyInjection.cs](../../backend/flight-service/Application/DependencyInjection.cs)
  with `AddApplication`.
- Added [Infrastructure/DependencyInjection.cs](../../backend/flight-service/Infrastructure/DependencyInjection.cs)
  with `AddInfrastructure`.
- Wired both extensions into [Program.cs](../../backend/flight-service/Program.cs)
  before building the app, preserving existing endpoint configuration.
- Documented scope and future registration ownership in
  [README.md](../../backend/flight-service/README.md).
- Left pre-existing untracked controllers, application abstractions, and query
  scaffolds untouched.

## Validation

- `dotnet build backend\flight-service\flight-service.csproj --no-restore` passed:
  zero errors and three existing unused-parameter warnings in controller/query
  scaffolds.
- VS Code diagnostics reported no errors in the two new files or `Program.cs`.
- `git diff --check` passed.

## Deferred

- Concrete mediator, validator, persistence, repository, and query registrations
  remain pending their implementations and configuration.
- No runtime or endpoint tests were run; no endpoints were added or changed.

## Current snapshot commit

- At the user's request, include all current tracked and untracked work in one
  commit on `main`, targeting `origin/main`.
- The snapshot also includes flight/aircraft/airport controller scaffolds,
  repository abstractions, the flight-query interface and unfinished implementation,
  and an empty booking-service reservation-controller file.
- User changes to `Program.cs` remove the weather endpoint and partial `Program`
  declaration, and add authorization middleware and controller mapping.
- Rebuilt the current flight-service snapshot successfully with zero errors and
  the same three unused-parameter warnings.
- Runtime startup and endpoint behavior remain unverified; controller,
  authorization, mediator, and persistence registrations are still incomplete.

## Implemented query results and API error handling

This work supersedes the preliminary/deferred query and API wiring above.

### Decisions

- Keep reads on direct query services; commands retain DispatchR handlers and
  validation pipelines. Share outcome semantics, not mediator plumbing.
- Return FluentResults over immutable application DTOs. Typed not-found errors
  describe application outcomes; controllers own HTTP translation.
- Validate search requests explicitly with FluentValidation before querying.
  Positive integer route IDs use MVC binding/range validation.
- The user confirmed UTC storage and frontend/downstream local-time conversion.
  Search uses UTC calendar-day boundaries; response timestamps have zero offsets.
- The user selected exact uppercase IATA codes, page 1 and page size 20 defaults,
  and a maximum page size of 100.
- Production database-provider selection remains deferred by user choice.
  Register the context/query service and verify with test-only SQLite.

### Changes

- Updated the query interface and implementation to return response DTO results,
  perform no-tracking projections, discover missing flights during the read, and
  return successful empty arrays. Pagination sorts by departure time then ID.
- Added search/response contracts, search validator, and `FlightNotFoundError`.
- Refactored the flight controller to return values, 400 validation problems, or
  404 problems with `flight_not_found`; removed its unused mediator dependency.
- Added API registration and global exception handling: full internal error
  logging, safe 500 problem responses, trace IDs, and explicit JSON problem
  writing when content negotiation has no compatible problem writer.
- Registered validators and Infrastructure reads; restored partial `Program`
  for HTTP integration testing and completed flight-endpoint startup wiring.
- Kept route paths and collection response shape. Intentional input changes:
  single-flight IDs now bind as integers; `DepartureDate` is date-only
  `YYYY-MM-DD`, replacing the scaffold's DateTime parameter.
- Updated project guidelines in
  [copilot-instructions.md](../../.github/copilot-instructions.md), service
  documentation, and HTTP request examples.
- Added
  [validator tests](../../backend/tests/unit/flight-service.unit.test/SearchFlightsRequestValidatorTests.cs)
  and
  [HTTP/query integration tests](../../backend/tests/integration/flight-service.integration.test/FlightQueryApiTests.cs).

### Checks and actual results

- VS Code's test tool did not discover the new files; used the existing xUnit
  VSTest projects through `dotnet test`, without new tooling or dependencies.
- Filtered solution run passed 16 validator tests and 17 HTTP/query tests.
- Final focused integration run passed all 19 HTTP/query tests after adding
  content-negotiation cases: **35 new test cases passed in total**.
- Verified DTO-only JSON, 404 error codes, field-specific 400 responses before
  database access, UTC day boundaries, stable pagination, actual 20/100 result
  limits, untracked reads, cancellation propagation, and OpenAPI response metadata.
- Verified a real SQLite failure is logged by the API handler and returns a safe
  500 even in Development and with JSON/plain-text/HTML Accept headers.
- Verified the host starts without a production provider and reads fail safely.
- Builds performed by the test commands succeeded with one remaining pre-existing
  unused mediator warning in the aircraft controller. VS Code diagnostics found
  no errors in changed service/test files.
- `git diff --check` passed before the final documentation update.

### Remaining work

- Configure a production provider/connection and migrations before deployment.
- Aircraft controller/mediator and command/repository scaffolds remain unchanged;
  this refactor implements the flight read endpoints only.
- Existing domain DateTime types remain unchanged; read projections interpret
  provider-materialized timestamps as UTC under the confirmed storage contract.
- No full-suite test run, commit, or push was performed for this refactor.
