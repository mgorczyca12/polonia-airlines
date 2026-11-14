# Copilot Instructions

## Repository map
- `backend/<name>-service/`: ASP.NET Core services (`flight`, `booking`, `seating`); `backend/shared/` holds shared building blocks.
- `backend/tests/unit/` and `backend/tests/integration/`: backend tests by service.
- `frontend/polonia-web/`: the customer web application (TanStack Start, React, Mantine) and its server-side web API layer.
- `tests/system/`: full end-to-end system tests across the web app and backend services.
- `docker/`: local and deployment container setup.
- `docs/architecture/`: current system documentation; `docs/ai-chats/`: dated session records and plans.

## Scoped instructions
These load automatically for matching files. Read the relevant file before planning or changing that area, even if no matching file is open yet.
- [Backend architecture](instructions/backend-architecture.instructions.md) (`backend/**`): DDD/Clean layering, CQRS with DispatchR commands and direct query services, `FluentResults` with ProblemDetails, aggregate encapsulation, and the service folder structure. Never put business logic in controllers or let Application depend on Infrastructure.
- [Backend style](instructions/backend-style.instructions.md) (`backend/**/*.cs`): explicit braces, block-bodied methods, readable multiline expressions, and the CI `dotnet format` checks. Do not apply it to frontend code.
- [Frontend](instructions/frontend.instructions.md) (`frontend/**`): platform strategy, thin routes, feature modules with public `index.ts` APIs, entity mappers and rules, the typed same-origin `/api/web` client with TanStack Query, and Mantine with colocated SCSS modules. Browser code never calls backend services directly.

## Cross-cutting rules
- Store timestamps in UTC and expose them with explicit offsets. Frontends own local-time conversion; calendar-date filters use UTC day boundaries.
- Treat IDs owned by another service as opaque strings. Preserve their representation without trimming or changing case, and do not parse them or add cross-service foreign keys.
- Never commit secrets. Never put tokens or personal data in URLs, logs, analytics, or browser storage.

## Working norms
- Put backend unit and integration tests in `backend/tests/`, frontend unit and component tests next to their source, and full end-to-end system tests in `tests/system/`.
- Run the checks relevant to the change. Never claim a build, test, or check passed unless it was run and passed.
- Update the related `docs/architecture/` pages when behavior, routes, contracts, or structure change.

## Session summaries
- At the end of each substantive repository session, create or update a concise summary in `docs/ai-chats/` before the final response. Skip trivial questions or sessions with no project decisions or work, and follow any explicit request not to create a summary.
- Name new files `YYYY-MM-DD-<topic>.md` using the user's current local date and a short kebab-case topic. Update the current session's same-day summary; roll over to a new dated file on each new day, linking to the previous summary rather than appending new work to it.
- Record the goal, decisions, files changed, checks and their actual results, and deferred work. Distinguish completed work from proposed or unverified work. Do not include secrets or reproduce the conversation transcript.
- Use the `session-summary` skill when available. Mention the summary file in the final response.
