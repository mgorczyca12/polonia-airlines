---
description: "Use when creating, editing, or reviewing frontend code or planning new frontend applications: platform strategy, routes, features, components, styles, API clients, the web server layer, or frontend tests. Keeps changes aligned with the chosen frontend architecture."
applyTo: "frontend/**"
---

# Frontend architecture and conventions

[polonia-web](../../frontend/polonia-web/) is a React and TypeScript app built
with TanStack Start, file-based TanStack Router, Mantine and Vite.
[Frontend architecture](../../docs/architecture/frontend.md) describes current
routes, scope and the guest-session design. The
[frontend refactor plan](../../docs/ai-chats/2026-11-14-frontend-refactor-plan.md)
describes the target structure and migration order.

The codebase follows this structure; the
[frontend refactor plan](../../docs/ai-chats/2026-11-14-frontend-refactor-plan.md)
records how it got here. All new and changed code must follow these rules.

## Platform strategy

- Treat the current customer-facing web application as the primary frontend. It must be responsive across desktop and mobile browsers.
- Use implementation-neutral platform names for new applications: `polonia-web`, `polonia-desktop`, and `polonia-mobile`.
- Create `polonia-desktop` only when native desktop capabilities or an operations-focused workflow require them, such as offline support, kiosk mode, native printing, or hardware integration.
- Create `polonia-mobile` only when native mobile capabilities require App Store or Play Store distribution, push notifications, wallet boarding passes, offline itineraries, or deep device integrations.
- When multiple frontend applications exist, share non-visual code through frontend packages: `api-client` for API contracts and clients, `domain` for frontend domain types and rules, and `ui` for design tokens and reusable UI primitives.
- Share API clients, domain types, and design tokens across web and native applications, but use UI components appropriate to each platform.

## Layers and dependencies

```text
src/
  routes/     thin TanStack Router files
  app/        providers, shell, layouts, theme
  features/   user-facing capabilities, e.g. flight-search, flight-results, booking
  entities/   frontend domain models, mappers, rules and query options (airport, flight, fare, offer, reservation)
  shared/     api (client, contracts, guards), lib (pure helpers), ui (primitives), testing (test helpers)
  server/     server-only web API layer (web-api/ route table, session, upstream, validation)
```

- Dependencies point downward:
  `routes → app/features → entities → shared`. `shared` never imports
  `entities`, `features`, `app` or `routes`. `entities` never imports
  `features`.
- A feature does not import another feature's internals. If code is shared,
  move it to `entities` or `shared`, or import from the other feature's
  `index.ts`. Avoid cycles; ESLint enforces these boundaries and
  `import/no-cycle`.
- Each feature exposes its public API through `index.ts`. Routes and other
  features import only from that file, never from `model/` or `ui/` subpaths.
  Code inside a feature uses relative imports and never imports its own barrel.
- Inside a feature, use `model/` for view logic, hooks (queries and mutations
  composed for the feature), reducers and URL parsers, and `ui/` for
  components. Store `Component.module.scss` next to `Component.tsx`.
- Query-key factories and `queryOptions` for API resources live in
  `entities/<name>/<name>.queries.ts` so every feature shares one cache key.
- Use the `#/` import alias for cross-layer imports. Do not use `@/`.
- `entities` and `shared` mirror the planned `domain`, `api-client` and `ui`
  packages. Keep them free of route and feature knowledge so they can move to a
  package later.

## Routes

- Route files only handle `validateSearch`, head metadata and rendering a
  feature page component. They do not fetch data in
  effects, manage multi-step state or contain long markup.
- Use URL search params, validated by a parser in the owning feature, for
  shareable state: route, date, traveller counts, page and offer ID. Reject or
  flag unsupported values explicitly; never silently book a different journey.
- Never put guest tokens, passenger details or contact details in URLs,
  `localStorage`, `sessionStorage`, logs or analytics.

## Server state and the API boundary

- Browser code calls only the same-origin `/api/web/*` layer through the typed
  client in `shared/api`. Never call backend services directly from the browser.
- Validate every response at runtime with type guards in `shared/api` before
  use. Invalid responses are errors. Never fall back to sample or mock data.
- Use TanStack Query (`@tanstack/react-query`) for server state: entity
  `queryOptions`, `useMutation` for commands and `refetchInterval` for polling.
  Do not add hand-written `useEffect` fetching, retry counters or polling loops.
- Queries currently run in the browser: the `/api/web` client uses relative
  URLs and the guest cookie, so do not call it from route loaders during SSR.
  First loads show section skeletons instead.
- Pass `AbortSignal` through all request functions. Treat cancellation as normal,
  not as an error.
- Keep explicit, idempotent recovery for reservation submission. Do not enable
  automatic retries for mutations.
- Map API DTOs to view models in `entities` mappers. Components do not consume
  raw DTOs.
- Server-only code uses the `.server.ts` suffix and `createServerOnlyFn` or
  server routes. Never import it from client components. The server layer stays
  an explicit allowlist. Mutations require the exact-Origin check, guest tokens
  stay in the encrypted HttpOnly cookie, and errors return safe ProblemDetails-like
  JSON without upstream internals.

## Components and state

- Split each page into one container, which calls hooks and owns workflow state,
  and presentational components, which receive props and render. Extract a
  component or hook when a file mixes concerns or grows beyond about 200 lines.
- Encapsulate workflows in named hooks such as `useReservationStatus` and
  `useFareSelection`. Use `useReducer` for multi-step flows and avoid long lists
  of related `useState` calls.
- Model states as discriminated unions and render them with lookup tables or
  `switch`. Do not nest ternaries more than one level in JSX.
- Business rules, such as when a reservation is confirmed or terminal, belong in
  `entities` as pure, tested functions, not inline in components.
- Use `@mantine/form` for forms, with validation that matches the server's rules.
- Avoid premature memoization. Derive values during render instead of syncing
  them into state with effects.

## Loading, empty and error states

- Every async section has distinct loading, empty and error states. Empty
  results succeed with an explicit message, and errors offer a safe retry
  where one makes sense.
- Use a section-level Mantine `Skeleton`, shaped like the real content and shown
  after a short delay, for first loads only. During refetching, paging and
  polling, keep existing content visible with a subtle indicator. Mutations
  use the button's loading state. Mark loading regions `aria-busy` with one
  accessible label.

## Styling, UI and accessibility

- Use Mantine components and the app theme with colocated SCSS modules. Use
  shared tokens and mixins from `src/styles/abstracts`. Do not add Tailwind,
  another component library or inline style objects for layout.
- Use `lucide-react` for icons.
- Layouts must work from a 390-pixel mobile viewport to desktop without
  horizontal overflow.
- Use semantic HTML with labeled form controls and a logical heading order.
  Make everything keyboard reachable with visible focus, and set `aria-expanded`
  and `aria-controls` on disclosures.
- Show timestamps in UTC with an explicit "UTC" label, matching the backend
  contract. Use the formatting helpers in `shared/lib` instead of creating new
  `Intl` formatters in components.
- Check [frontend architecture](../../docs/architecture/frontend.md) for which
  features are currently supported. Keep features that aren't available yet
  visibly disabled, and show booking outcomes only from the API's authoritative
  state.

## TypeScript and formatting

- Keep TypeScript strict. Do not use `any`, `@ts-ignore` or non-null assertions
  unless an invariant is proven nearby. Use `import type` for type-only imports.
- Follow [prettier.config.js](../../frontend/polonia-web/prettier.config.js),
  which uses no semicolons, single quotes and trailing commas. The whole package
  is formatted and CI checks it with `pnpm format`.
- Comment only non-obvious decisions.

## Tests

- Put Vitest unit and component tests next to the source as `*.test.ts(x)`. Use
  Testing Library queries by role or label. Render components with
  `renderWithProviders` and `stubMatchMedia` from `#/shared/testing/render`,
  and mock `#/shared/api` rather than `fetch` in component tests.
- Test entity rules, mappers, search-param parsers, hooks and the server-layer
  security behavior. Assert user-visible outcomes, not implementation details.
- Full browser-to-backend system tests belong in
  [tests/system](../../tests/system/README.md).

## Checks

Run these from `frontend/polonia-web` before finishing frontend changes. CI runs
the same commands:

```shell
pnpm typecheck
pnpm lint
pnpm format
pnpm test
pnpm build
```

`pnpm check` applies Prettier and ESLint fixes. Do not claim a check passed
unless you ran it. Use `pnpm` and the existing lockfile; add dependencies only
when a task requires them and record the reason.
