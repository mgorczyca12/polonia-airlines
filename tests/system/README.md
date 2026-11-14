# System tests

These Playwright tests exercise the real web application, flight, booking and
seating services with PostgreSQL, Redis and RabbitMQ. They create synthetic
one-way USD flights and verify confirmed allocations and capacity rejection.
Payment and ticket issuance are not implemented.

## Run locally

Requires Docker with Compose, Node.js, pnpm and a Playwright Chromium browser.
From this directory:

```powershell
pnpm install --frozen-lockfile
pnpm exec playwright install chromium
pnpm test
```

The runner creates a random Compose project, passwords and localhost web port,
builds the actual service/web images, waits for infrastructure, applies all
three checked-in EF migrations in a one-shot fixture, and seeds matching flight,
price and seating references. Browsers enter through Nginx; only its dynamically
chosen web port is published. A separate fixture flight is used for each scenario.
The fixture runs only against its Compose `postgres` host and rejects databases
with migration history. Before launching Playwright, the runner also checks that
the customer web API can read both fixture flights and their fares. Those reads
verify more than container liveness but do not by themselves prove broker readiness.

The runner collects container logs on failure and always attempts to remove its
own Compose project and volumes. If cleanup fails, the reported project name can
be inspected with Docker; do not remove other projects. Playwright reports,
traces and screenshots are local test artifacts and may contain synthetic booking
details. The test dates and credentials are generated for each run.

An ungraceful process termination can prevent cleanup; a port selected just
before Compose starts can also be taken by another process, in which case the
run fails and its own resources are cleaned up. Do not point these tests at
existing or shared infrastructure.

## CI

This suite is intentionally not wired into automatic CI until its first real run
has been validated. A future dedicated job needs Docker Compose, Node/pnpm and
Playwright Chromium, followed by `pnpm test`. Do not replace these tests with
mock browser fixtures or treat service liveness as proof of schema readiness.
