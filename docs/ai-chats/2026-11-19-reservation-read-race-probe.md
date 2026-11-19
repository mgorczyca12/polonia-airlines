# Reservation status race probe

Date: 2026-11-19. Prior context: [Dependabot CI investigation](./2026-11-17-dependabot-ci-investigation.md).

## Result

- Temporarily added a SQLite-backed API test that paused the reservation GET immediately before its workflow-state query, then changed the reservation to `Reserved` and the workflow to `Completed`.
- The focused test passed (1/1) and observed a response containing reservation status `Created` with workflow status `Completed`. This confirms the API can combine values read at different times under that ordering; it does not prove that the unforced timing caused the earlier CI failures.
- Updated reservation GET reads to use a PostgreSQL repeatable-read transaction, keeping reservation and saga status on one database snapshot.
- A permanent forced concurrent-writer test was not viable on SQLite because its transaction blocked the writer. Replaced it with a permanent SQLite HTTP integration test that verifies all reservation-read queries execute in the same transaction.
- The new test passed, and all 10 `BookingApiTests` passed. The existing reservation API test also passed with repeatable-read enabled.
- Both required backend format checks passed, and `git diff --check` passed. The broker-backed workflow tests were not run because their dedicated PostgreSQL, Redis, and RabbitMQ settings were not configured; reproducing the exact production interleaving against PostgreSQL remains unverified locally.
- Committed the fix as `1350f2b` and pushed to main in `013e2d8`. Verified [CI run 44600161254](https://github.com/mgorczyca12/polonia-airlines/actions/runs/44600161254) passed on that main commit.
- Updated [issue #51](https://github.com/mgorczyca12/polonia-airlines/issues/51) with the reproduction limits, fix, and CI evidence, then verified it was closed as completed.
