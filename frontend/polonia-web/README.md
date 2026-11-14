# polonia-web

Customer web application for Polonia Airlines: flight search, fare selection and
guest reservations. Built with React, TypeScript, TanStack Start/Router,
TanStack Query, Mantine and SCSS modules, served by Vite.

See [frontend architecture](../../docs/architecture/frontend.md) for routes,
current scope and the guest-session design, and the
[frontend instructions](../../.github/instructions/frontend.instructions.md) for
code conventions.

## Getting started

The app calls backend services only through its own server-side `/api/web` layer,
which needs these environment variables (see
[development setup](../../docs/architecture/development.md)):

| Variable               | Purpose                                                                                |
| ---------------------- | -------------------------------------------------------------------------------------- |
| `WEB_SESSION_PASSWORD` | Secret of 32+ characters that encrypts the guest cookie                                |
| `WEB_ORIGIN`           | Exact public origin, e.g. `http://localhost:3000`; required for mutation Origin checks |
| `FLIGHT_API_URL`       | Base URL of flight-service                                                             |
| `BOOKING_API_URL`      | Base URL of booking-service                                                            |

```shell
pnpm install
pnpm dev        # http://localhost:3000
```

## Scripts

| Script                      | Purpose                                                        |
| --------------------------- | -------------------------------------------------------------- |
| `pnpm dev`                  | Development server on port 3000                                |
| `pnpm build` / `pnpm start` | Production server-rendered build, served with srvx             |
| `pnpm test`                 | Vitest unit and component tests                                |
| `pnpm typecheck`            | TypeScript without emitting                                    |
| `pnpm lint`                 | ESLint, including layer and feature-boundary rules             |
| `pnpm format`               | Prettier check; `pnpm check` applies Prettier and ESLint fixes |

CI runs `typecheck`, `lint`, `format`, `build` and `test`. The
[Docker setup](../../docker/README.md) runs the Node server behind Nginx; this is
not a static-only deployment. Full browser-to-backend tests live in
[tests/system](../../tests/system/README.md).

## Structure

```text
src/
  routes/     thin file routes (validateSearch + one feature page)
  app/        shell, layouts, theme, query client
  features/   flight-search, flight-results, booking, home (index.ts, model/, ui/)
  entities/   airport, flight, fare, offer, reservation models, rules, queries
  shared/     api client and contracts, lib helpers, ui primitives, testing helpers
  server/     server-only /api/web route allowlist, session and validation
```
