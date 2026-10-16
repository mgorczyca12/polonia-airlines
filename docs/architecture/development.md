# Development and maintenance

[Documentation index](README.md)

## Prerequisites and local startup

- .NET SDK 10 for backend projects.
- CI uses Node.js 26 and pnpm 12 for the web application.
- Restore dependencies before using `--no-restore` commands.

From the repository root:

```powershell
dotnet restore polonia-airlines.slnx
dotnet run --project backend\flight-service\flight-service.csproj
```

Other hosts can be run with their respective booking/seating project paths.
Use launch-profile/console URLs; no unified gateway is configured.
Flight data reads require database-provider configuration that normal startup
does not yet provide. Booking/seating startup exposes weather scaffolding only.

For the web application:

```powershell
Set-Location frontend\polonia-web
pnpm install --frozen-lockfile
pnpm dev
```

The dev script uses port 3000. The UI currently reads local JSON and does not need
the backend hosts for its sample search and booking-preview flow.

## Verification

The [CI workflow](../../.github/workflows/ci.yml) has `backend` and `frontend` jobs.
Backend checks run formatting, Release build and tests:

```powershell
dotnet format whitespace polonia-airlines.slnx --no-restore --verify-no-changes
dotnet format style polonia-airlines.slnx --diagnostics IDE0011 IDE0021 IDE0022 --no-restore --verify-no-changes
dotnet build polonia-airlines.slnx --no-restore -c Release
dotnet test polonia-airlines.slnx --no-build -c Release
```

The tests use xUnit/VSTest. Flight integration tests configure in-memory SQLite;
they are not proof of a configured production database. Booking and seating
integration projects currently contain no test source files.

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

The final option permits an empty test suite; successful CI alone does not mean
frontend behavior has test coverage.

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
