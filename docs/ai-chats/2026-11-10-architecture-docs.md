# Architecture documentation refresh

Previous session: [2026-11-09 unit test readability](./2026-11-09-unit-test-readability.md).

## Goal and changes

- Updated the architecture index to describe the implemented booking API and booking-to-seating RabbitMQ workflow, while keeping the web app and flight-service boundaries explicit.
- Corrected migration status: initial migrations and authoring are present; migration CI, release bundles, and deployment automation remain planned.
- Updated the architecture integration-testing status to reflect locally passed booking workflow/recovery tests and pending hosted/broader outage verification.
- Brought the frontend architecture page in line with the passenger, demo-payment, and local-confirmation flow, including its non-persistent, non-integrated limitations.

## Checks

- `git diff --check`: passed.
- Documentation reviewed against current service/frontend docs and implementations; no tests run (documentation-only changes).
