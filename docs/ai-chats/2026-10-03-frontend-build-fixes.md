# Frontend Build Fixes

Date: 2026-10-03

## Goal

Build the web frontend and resolve the compile, lint, and package-manager issues uncovered during verification.

## Changes

- Repaired the pnpm lockfile metadata so the frontend's declared versions resolve consistently.
- Added ESLint as a direct development dependency, enabling the existing lint script.
- Migrated the ignored `pnpm.onlyBuiltDependencies` entries to the supported workspace `allowBuilds` configuration.
- Fixed the booking navigation's required search parameters, removed an unused form callback argument, and addressed lint errors in flight timeline, passenger dropdown, and booking route code.
- Kept the existing unrelated changes in `2026-10-03-agent-workflows-summary.md` untouched.

## Verification

- `pnpm --dir frontend/polonia-web run build` — passed, including client and server bundles.
- `pnpm --dir frontend/polonia-web exec tsc --noEmit` — passed.
- `pnpm --dir frontend/polonia-web run lint` — passed.
- Prettier check on all changed frontend source files — passed.
- `pnpm --dir frontend/polonia-web test` — no test files exist; Vitest exited with code 1.

## Deferred

- Add frontend tests; the current frontend has no Vitest test files.
