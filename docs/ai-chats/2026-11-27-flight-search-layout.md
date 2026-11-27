# Flight search layout

- Goal: keep departure and travellers below the airport fields on wide screens.
- Changed the flight search grid to use the existing two-row desktop layout at all desktop widths; the mobile stacked layout remains unchanged.
- Aligned the departure date and travellers fields with the airport fields by leaving the swap-button column empty on the second row.
- Changed: `frontend/polonia-web/src/features/flight-search/ui/FlightSearch.module.scss`.
- Validation: the changed stylesheet passed Prettier after formatting. Other frontend checks and browser verification were not run.
- Pre-existing change: `frontend/polonia-web/src/routeTree.gen.ts` was already modified and was not intentionally changed.

## HTTP resilience

- Added Cockatiel to the web API: backend GETs receive up to two retries with jittered exponential backoff; per-backend, per-process circuit breakers cover reads and writes. Writes are never retried, browser query retries are disabled, and the 15-second upstream timeout covers all attempts.
- Added an opt-in shared .NET HTTP-client resilience extension using the standard handler with unsafe-method retries disabled. No backend service registers it yet.
- Added focused web API and shared integration tests and updated the frontend and system architecture documentation.
- Checks: frontend tests (54 passed), typecheck, lint, build and changed-file Prettier checks passed; shared HTTP resilience tests (3 passed) and targeted C# formatting checks passed. The full frontend format check failed on 132 files, predominantly unrelated existing formatting/line-ending differences.
- Future work: select dependency-specific policies when a backend HTTP client is added; browser-to-web network failures are not automatically retried.
