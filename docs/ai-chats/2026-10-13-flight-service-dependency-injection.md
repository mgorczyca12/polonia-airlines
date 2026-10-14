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
