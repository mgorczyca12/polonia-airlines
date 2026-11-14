# Frontend refactor plan

Date: 2026-11-14. Previous context:
[frontend/backend integration](2026-11-13-frontend-backend-integration-plan.md).

## Goal

Review [polonia-web](../../frontend/polonia-web/) for organization, readability
and applicable frontend patterns. The review was first delivered as a report and
plan; the user then asked to implement it, which is recorded under
[Implementation](#implementation).

## Decisions

- **Adapt** the orphaned styled/Figma UI into the live screens instead of deleting it.
- Adopt **TanStack Query** for server state, which adds `@tanstack/react-query`.
- Include a tooling cleanup and a restructure of `web-api.server.ts` into a
  route table.
- Keep the [integration scope](../architecture/frontend.md): one-way trips,
  USD, eight seated travellers, no infants, no payment, and no invented fare,
  aircraft, segment or perk claims.
- Use section-level skeletons for first loads only. Refetching, polling and
  submission keep existing content visible (see [Loading states](#loading-states)).

## Findings (baseline: `tsc`, ESLint and 18 Vitest tests passed)

1. **Orphaned UI.** No route reaches `features/flight-results` (10 components),
   the booking components `BookingSummary`, `BookingConfirmation`,
   `JourneySummary`, `PassengerBaggageAllowance`, `PassengerDetailsForm` and
   `PaymentDetailsForm`, `booking.types.ts`, or `data/ord-krk-flights.json`.
   They depend on a mock `Flight` model with segments, aircraft, layovers,
   perks, benefits and numeric fare IDs.
2. **God components.** [flightlist.tsx](../../frontend/polonia-web/src/routes/flightlist.tsx)
   (~250 lines) and [LiveBooking.tsx](../../frontend/polonia-web/src/features/booking/components/LiveBooking.tsx)
   (496 lines, 17 `useState` calls, three hand-written fetch/poll effects and
   five-level nested ternaries) mix fetching, workflow state and markup.
3. **Manual server state.** Each screen hand-codes `useEffect`,
   `AbortController`, retry counters and loading/error flags.
4. **Mixed `shared/api` responsibilities.** It combines the HTTP client,
   contracts/guards, domain rules (`isConfirmed`, `isTerminal`) and formatting.
   `presentation.ts` duplicates `shared/lib/formatting.ts` (`formatMoney` and
   `formatUtc` versus `formatUsd` and its helpers).
5. **Leaky feature boundaries.** Routes deep-import feature internals.
   `LiveBooking` imports its own `#/features/booking` barrel while
   `import/no-cycle` is off. The UUID regex is duplicated in `routes/booking.tsx`
   and the server layer.
6. **Tooling leftovers.** The Tailwind Vite plugin is registered, but no
   stylesheet imports Tailwind. The starter `/about` page uses Tailwind classes
   that do not render. `@tabler/icons-react` and the `@/*` alias are unused,
   and the README is starter boilerplate.
7. **Server layer.** [web-api.server.ts](../../frontend/polonia-web/src/shared/api/web-api.server.ts)
   routes requests through one long `if`/`else` chain.

## Target structure

```text
src/
  app/          providers (Mantine, QueryClient), shell, layouts, theme
  routes/       thin: validateSearch, loader/prefetch, compose feature pages
  features/
    flight-search/   search form, recap, search-param parsing
    flight-results/  results page, cards, fare selection (live data)
    booking/         passengers, review, reservation status/confirmation
      api/        queries.ts, mutations.ts (query keys + options)
      model/      view models, mappers, reducers, hooks
      ui/         components with colocated *.module.scss
      index.ts    public API (only import surface for routes/other features)
  entities/     airport, flight, fare, reservation: types, mappers, rules
  shared/
    api/        http client, contracts, guards (future `api-client` package)
    lib/        formatting (single source), ids/validation helpers
    ui/         reusable primitives (future `ui` package)
  server/       web-api route table, handlers, session, upstream client
```

This mirrors the planned `api-client` / `domain` / `ui` frontend packages, so a
later extraction is a move rather than a rewrite.

## Patterns to apply

- **Server state with TanStack Query.** Add a `QueryClient` provider in the root
  route, use query-key factories such as `flightKeys.search(params)` and
  `fareKeys.forFlights(ids)`, and use `queryOptions` reused by route loaders via
  `ensureQueryData` and `@tanstack/react-router-ssr-query`, which is already
  installed. Fetch fares as a dependent query. Create offers and reservations
  with `useMutation`. Poll reservations with a `refetchInterval` function that
  stops at terminal states and backs off up to the existing 40-attempt cap.
  Disable automatic retries for mutations because idempotent recovery stays
  explicit through `recoverReservation`.
- **Adapters and view models.** Add `entities/flight` and `entities/fare`
  mappers from `FlightResponse` and `FlightFareResponse[]` to UI models:
  group fares into cabins by `cabinClass`, use `brand` as the fare name, use
  the UUID `productId` as the fare ID, and use baggage as the only benefit.
  Components never consume raw DTOs.
- **Discriminated unions instead of nested ternaries.** Use
  `deriveReservationView(reservation)` to return
  `{ kind: 'confirmed' | 'pending' | 'compensating' | 'intervention' | 'cancelled' | 'inconsistent' }`,
  then render it with a lookup table of copy and colors. Model the booking
  workflow (`passengers` → `review` → `submitted`) with `useReducer`.
- **Custom hooks for workflows.** Encapsulate concerns in hooks such as
  `useOfferExpiry`, `useBookingSession(offerId)` for recovery or offer loading,
  `useReservationStatus` and `useFareSelection`.
- **Forms.** Move passenger and contact entry to `@mantine/form`, matching the
  existing `FlightSearch`, with validators shared with the server's age and
  length rules.
- **Container and presentational split.** Each page has one container that calls
  hooks. Leaf components receive props only, which makes them easy to test and
  reusable on future platforms.
- **Enforced boundaries.** Routes and other features import only `index.ts`.
  Enable `import/no-cycle` and add `no-restricted-imports` rules for deep
  `features/*/*` imports. Colocate `Component.tsx` with
  `Component.module.scss` in place of separate `styles/` folders.

## Loading states

Skeletons are for first loads of main content areas, not for every async
component.

- **When to show a skeleton.** Use one for a section's first load when its
  layout is predictable. Show it only after about 150–300 ms so fast responses
  don't flash a placeholder.
- **One boundary per section.** Use one Suspense boundary or `isPending` check
  per page section, not per component.
- **Skeleton components.** Build them from Mantine `Skeleton`, sized to match
  the real component, and store each next to its component (for example,
  `FlightResultCard.Skeleton`).
- **Refetching and polling.** Keep the current data visible and show a subtle
  "updating" indicator. Use `placeholderData: keepPreviousData` when paging.
- **Mutations.** Use the button's loading state, not a skeleton.
- **Preloaded data.** Bundled data such as airport labels, and data already
  fetched by a route loader, needs no skeleton.
- **Separate states.** Keep loading, empty and error states distinct. A skeleton
  must never stand in for "no results" or a failure.
- **Accessibility.** Mark the loading region `aria-busy` and give it one
  accessible label, such as "Loading flights".

| Area | Loading state |
| --- | --- |
| `/flightlist` first load | 3–4 result-card skeletons |
| `/flightlist` page change | Previous results remain visible but dimmed |
| Booking first load | Itinerary and fare-summary skeletons |
| Reservation status polling | Content remains visible with an "updating" badge |
| Offer or reservation submission | Button loading state |
| Itinerary read failure | Inline alert, as today |

## Adapting the mock UI

| Mock component | Plan |
| --- | --- |
| `FlightResults`, `FlightResultCard`, `FlightTimeline`, `CabinSelector`, `FareOptions`, `PassengerFareSelector` | Reuse with live view models. Show departure/arrival UTC times and duration from timestamps. Remove the aircraft label, stops/layovers and "recommended/shortest" claim unless backed by data. |
| `FlightDetails` (segments) | Drop it or reduce it to the flight number and airports. Do not synthesize a nonstop segment. |
| `FareBenefitsComparison` | Reduce it to baggage terms from the fare response. Remove meal, lounge, change and refund rows. |
| `DateFareStrip` | Use it for date navigation only, without adjacent-day prices, or omit it. |
| `PassengerDetailsForm` | Reuse its layout. Remove title, nationality, membership, phone, SMS and special-request fields. |
| `JourneySummary`, `BookingSummary`, `PassengerBaggageAllowance` | Feed them with offer or reservation fare snapshots and live totals. |
| `BookingConfirmation` | Reuse its layout for the reservation status/PNR view, driven by `deriveReservationView`. |
| `PaymentDetailsForm` | Replace it with the review and "Reserve without payment" step. Keep the component out of the live flow. |
| `ReturnFlightNotice`, mock `flight.repository`, `booking.types`, `ord-krk-flights.json` | Delete once replaced. |

Remove unused return and infant branches from `search-params.ts` after the
results UI no longer needs them.

## Server layer (`web-api.server.ts`)

- Move it to `src/server/`. Split it into `config.ts`, `session.ts`, the
  `upstream.ts` client, `validation.ts` for offers, passengers and JSON bodies,
  and `routes.ts`.
- Define routes as an explicit allowlist of `{ method, pattern, auth: 'none' | 'guest', handler }`.
  The dispatcher resolves the route, enforces exact-Origin checks for mutations,
  loads the session only for guest routes, strips `guestAccessToken` and maps
  errors. Keep a 404 default.
- Share `UUID` and ID patterns with the client in `shared/lib`.
- Preserve every existing behavior. The current
  [server tests](../../frontend/polonia-web/src/shared/api/web-api.server.test.ts)
  must pass unchanged as the regression gate.

## Tooling cleanup

- Remove `tailwindcss`, `@tailwindcss/vite`, `@tailwindcss/typography` and
  `@tabler/icons-react`. Remove the `@/*` path alias. Rewrite `/about` with
  Mantine and SCSS, or remove it.
- Replace the README with project-specific setup, scripts, structure, and
  environment variables.
- Pin `latest` TanStack dependencies to explicit ranges for reproducible
  installs, then add a `typecheck` script (`tsc --noEmit`).
- Gate TanStack devtools to development in `__root.tsx`.

## Proposed sequence (each step should leave tests green)

1. Tooling cleanup, ESLint boundary rules and the `typecheck` script.
2. Split `shared/api` into the client, contracts and entity rules. Unify
   formatting.
3. Add the `QueryClient` provider, query and mutation options, and tests for
   mappers and `deriveReservationView`.
4. Rebuild `/flightlist` using the adapted results components, fare-selection
   hook and result-card skeletons.
5. Rebuild booking with the reducer, hooks, adapted forms, summaries,
   confirmation and loading states. Split the `LiveBooking` test into hook and component tests.
6. Delete remaining mock code and data. Run the server-layer route-table refactor.
7. Update [frontend architecture](../architecture/frontend.md) and capture
   desktop and 390-pixel browser checks.

## Frontend agent instructions

No frontend instructions existed; `copilot-instructions.md` covered only platform
strategy. Added:

- [frontend.instructions.md](../../.github/instructions/frontend.instructions.md)
  (`applyTo: frontend/**`). It covers layers and dependency direction, feature
  public APIs, thin routes, the `/api/web` boundary, TanStack Query, entity
  mappers, component and state patterns, loading states, styling and
  accessibility, product scope, TypeScript and formatting, tests, and checks.
  It also tells agents to apply these rules to new and changed code without
  incidental large migrations.
- A short "Frontend Architecture" section in
  [copilot-instructions.md](../../.github/copilot-instructions.md) linking to it.
- A fix to the stale "no frontend test exists yet" note in the
  [test-writer agent](../../.github/agents/test-writer.agent.md).

## Agent instruction refactor

Done before the frontend refactor, at the user's request.

- Slimmed [copilot-instructions.md](../../.github/copilot-instructions.md) from
  about 105 to about 35 lines. It now has a repository map, an index of scoped
  instruction files with their key rules, cross-cutting rules (UTC, opaque
  cross-service IDs, secrets and personal data), working norms (test
  locations, honest checks, doc updates) and session summaries.
- Moved the backend architecture, CQRS and query-error rules, domain
  encapsulation, cross-service identifiers, service structure, and patterns
  sections into the new
  [backend-architecture.instructions.md](../../.github/instructions/backend-architecture.instructions.md)
  (`applyTo: backend/**`). They are unchanged except for fixed typos.
  [backend-style.instructions.md](../../.github/instructions/backend-style.instructions.md)
  stays formatting-only.
- Moved "Frontend Platform Strategy" into
  [frontend.instructions.md](../../.github/instructions/frontend.instructions.md).
- Decision: do not hardcode current product scope in instructions, because it
  will expand quickly. The root file does not mention scope. The frontend file
  points to [frontend architecture](../architecture/frontend.md) for currently
  supported features, and its hardcoded scope and missing-field examples were
  removed.
- Pointed the [code-reviewer](../../.github/agents/code-reviewer.agent.md) and
  [dependency-fixer-tier2](../../.github/agents/dependency-fixer-tier2.agent.md)
  agents at the scoped instruction files. Updated the
  [architecture doc](../architecture/architecture.md) layering link.
- Workflows import agent files with `runtime-import`, so no lock-file recompile
  was needed.
- Checks: a script compared every bullet in the old root file with the new
  files. The only differences were the two backend-readability bullets, which
  `backend-style.instructions.md` and its index line cover, and the CQRS bullet
  whose typos were fixed. `git diff --check` **passed**.

## Implementation

Done on branch `refactor/frontend-architecture`, one commit per plan step, so
it can go through a pull request (`main` requires PRs). The plan was followed
with these decisions and deviations:

- **Tooling:** Tailwind, Tabler icons, the `@/` alias and the broken `/about`
  starter route were removed; TanStack `latest` ranges were pinned; a
  `typecheck` script was added and devtools now load in development only.
- **Boundaries:** ESLint `no-restricted-imports` enforces feature public APIs
  and downward layer imports. `import/no-cycle` had been silently inert because
  the TanStack config sets no resolver and import-x parses only `.js` by
  default. `eslint-plugin-import-x` was added as a direct dev dependency to
  configure its resolver and TypeScript parsing; a temporary cycle probe
  confirmed detection for relative and `#/` imports.
- **Query options in entities:** query-key factories and `queryOptions` live in
  `entities/<name>/<name>.queries.ts` (not in feature `api/` folders) so all
  features share cache keys. Queries run in the browser; route loaders do not
  prefetch because the `/api/web` client uses relative URLs and the guest
  cookie.
- **Flight list:** thin route plus page container, `useFlightResults`,
  `useFareSelection` and `useContinueToBooking`. The page number is in the URL.
  The adapted card shows UTC times, airports, duration and flight number; fares
  show baggage terms. Sorting, recommendations, aircraft, stops, segment
  details and adjacent-day prices were dropped, and the date strip only
  navigates.
- **Booking:** `LiveBooking` was replaced by `BookingPage` with
  `useBookingSession` (recovery first, idempotent submit, explicit status
  check), `useReservationStatus` (TanStack Query polling with the same backoff
  and 40-poll cap) and `useOfferExpired`. Forms use `@mantine/form` with
  validation mirroring the web API. The mock confirmation layout became
  `ReservationStatus`, driven by `deriveReservationView`. Destination hero,
  travel tips, itinerary download and the payment form were deleted rather than
  kept unused.
- **Server:** the web API moved to `src/server/web-api/` with an explicit route
  table. One intentional behavior change: unknown routes now return 404 even
  without a guest cookie (previously 401).
- **Return/infant search parameters** were kept, because the product scope is
  expected to grow.
- **CI and docs:** one-time Prettier pass; CI now runs typecheck, lint, format,
  build and test without `--passWithNoTests`. Updated the polonia-web README,
  [frontend architecture](../architecture/frontend.md),
  [development](../architecture/development.md) and the
  [frontend instructions](../../.github/instructions/frontend.instructions.md).

## Checks and results

- Baseline before changes: `tsc`, ESLint and 18 Vitest tests **passed**;
  Prettier **failed** on 26 pre-existing files.
- After every step: `tsc`, ESLint, Vitest and `pnpm build` **passed**. Final:
  typecheck, lint, format, build all **passed**; tests **47 passed** in 11 files,
  including the unchanged server security tests plus a new unknown-route case.
- The client bundle contains no session-password, guest-cookie, guest-header or
  backend-URL strings.
- Browser smoke test against a throwaway mock backend (session folder, not
  committed): search, skeleton, fares, per-traveller pricing, offer creation,
  form validation, review, submission, pending state and confirmation by
  polling **passed**. Confirmation survived a reload; no horizontal overflow at
  390 px; the guest cookie was not readable from JavaScript.
- **Not run:** the `tests/system` suite against real services, and a visual
  comparison with the Figma designs.

## Deferred and open items

- Visual review of the adapted screens against Figma.
- Run the system tests once the live stack is available.
- The [integration-tests agent](../../.github/agents/integration-tests.agent.md)
  still places UI tests in `frontend/polonia-web/e2e`, while system tests live in
  `tests/system`. Changing it also requires updating the test-writer workflow's
  `push_to_pull_request_branch.allowed_files` and recompiling the lock file.
- Open a pull request from `refactor/frontend-architecture`.

## Pre-implementation checks

- Baseline only: `pnpm exec tsc --noEmit` **passed**, `pnpm exec eslint src`
  **passed**, and `pnpm test` **passed** (3 files, 18 tests).
- `pnpm exec prettier --check src` **failed** on 26 pre-existing files. The new
  instructions ask agents to format only the files they change.
- No application code was changed and no build, browser or system test was run.

## Pre-implementation deferred items (superseded)

- All refactor steps above were **proposed, not implemented** at this point;
  see [Implementation](#implementation) for what was done.
- Visual adaptation must be checked against the Figma designs. Some mock
  styling assumes fields that do not exist, such as aircraft and layovers.
- Decide whether `/about` stays (removed during implementation).
- The [integration-tests agent](../../.github/agents/integration-tests.agent.md)
  still places UI tests in `frontend/polonia-web/e2e`, while system tests live in
  `tests/system`. Changing it also requires updating the test-writer workflow's
  `push_to_pull_request_branch.allowed_files` and recompiling the lock file.
- Optional follow-ups:
  - A one-time Prettier pass, in its own commit.
  - Add `tsc --noEmit` and ESLint to the CI frontend job.
  - Remove `--passWithNoTests`, because frontend tests now exist.
