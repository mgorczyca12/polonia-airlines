# Committing shared command pipelines - 2026-10-16

Previous work: [shared EF Core and command pipelines](2026-10-15-shared-ef-core.md).

## Goal and scope

- Commit all current changes and push the current `main` branch to `origin`.
- Include shared EF dependencies, opt-in transaction/retry pipelines, fresh attempt scopes, tests and naming changes, documentation, flight infrastructure registration, and the user's unfinished CreateFlight command scaffolding.
- Preserve current implementation and drafts; no additional runtime behavior changes.

## Verification

- `dotnet test polonia-airlines.slnx --no-restore --nologo`: 133 cases passed across five nonempty projects. Booking/seating integration projects contain no tests.
- Existing unused `mediator` warning in `AircraftController` remains.
- `git diff --check`: passed before staging.
- Fetched `origin`; local `main` and `origin/main` had no divergence before the commit.
- Previous formatting checks are recorded in the linked summary; known flight-command draft formatting issues remain, and its handler still throws `NotImplementedException`.
- The staged whitespace check caught trailing whitespace in the previously untracked handler draft. Removed that whitespace only; implementation is unchanged.

## Publication

- Commit and push are the next operations; their actual outcome will be reported after verification.
- Provider configuration, flight command completion, and MassTransit transport/outbox/saga integration remain deferred.
