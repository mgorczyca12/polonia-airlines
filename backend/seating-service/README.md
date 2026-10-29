# Seating inventory participant

The service consumes `HoldReservationInventory`, `ConfirmReservationInventory`,
and `ReleaseReservationInventory` and publishes their shared reservation-workflow
outcomes. `/health` is liveness only; there are no public inventory administration
or hold endpoints. All external IDs are opaque, ordinal, case-sensitive strings.

## Storage and protocol

PostgreSQL stores cabin inventory, confirmed `SeatAssignment` rows, the workflow
operation ledger, and MassTransit consumer inbox/outbox. `SeatInventory` is mapped
with its assignments backing field. Confirmation and release still use its
`ConfirmAllocation` / `ReleaseAllocation` methods: the adapter does not bypass the
aggregate. `ReservedSeats` is a confirmed-only count, checked against loaded
assignments. Unique indexes enforce passenger allocation per flight/cabin and
non-null physical seat per flight. `Seats` is the internal aircraft-layout
membership projection; a physical selection must belong to the requested cabin.
Cabin-only allocations are permitted, including configured overbooking.

Redis is **authoritative for temporary validity**, not a cache or SQL fallback.
One hash (default `{reservation-inventory}:holds`) contains owner tokens and
absolute expiry timestamps, never asynchronously maintained held-seat counters.
One Lua execution uses Redis server time, lazily removes expired tokens, derives
active cabin/physical counts, and acquires **all** selections or none. It checks
confirmed SQL counts and confirmed passenger/physical-seat exclusions supplied
while holding the SQL mutation lock. Retries reuse the same correlation owner and
expiry and recheck current confirmed capacity. No retry extends a hold. Absolute
deadlines must be in the future and at most ten minutes away; messages contain
1–100 selections. Expiry is canonicalized to UTC milliseconds for stable tokens
across PostgreSQL timestamp precision and offset conversion.

Every mutation requires a READ COMMITTED SQL transaction (provided by the
MassTransit consumer outbox). It first locks the durable singleton
`InventoryMutationLock` row, then the itinerary's inventory rows in ordinal
flight/cabin order. The singleton intentionally serializes **all** inventory
workflows, including workflows with no existing ledger row. Reads happen after
the lock under READ COMMITTED, avoiding stale serializable snapshots around
external Redis side effects. Provisioning must follow the same lock protocol.
This is a simple first-milestone throughput tradeoff, not a distributed lock in
Redis. Consumers retry failures five times at two-second intervals, including
PostgreSQL concurrency/deadlock errors and Redis failures; exhausted messages
require normal broker error-queue operations.

The SQL operation ledger uses the workflow correlation as its key:

- Held metadata does **not** make a hold valid without its exact Redis token.
- Duplicate successful operations replay their prior outcome without allocating
  twice. Unknown confirmation creates a durable rejection.
- Rejected and Released rows are durable terminal tombstones. Release arriving
  before Hold prevents resurrection; late Hold/Confirm publishes rejection.
- Confirmation validates the token and rechecks SQL capacity before applying all
  allocations in the same SQL transaction as the outcome outbox write.
- Confirmation deliberately does **not** delete its Redis token inline. Its
  SQL consumer outbox delivers `ReservationInventoryConfirmed` to an independent
  seating cleanup consumer only after confirmation commits. Cleanup locks the
  same SQL mutation guard, verifies the durable operation is still Confirmed,
  and removes only that correlation's Redis token. It publishes no outcome and
  does not block booking completion. Duplicate deliveries are safe.
- Until asynchronous cleanup succeeds, the confirmed allocation and old token
  conservatively double count capacity; they cannot oversell. Redis failures
  are logged and rethrown through the configured retry/error-queue policy.
  Expiry remains the conservative fallback if cleanup is delayed or exhausts
  retries. Cleanup SQL rollback after Redis deletion is safe because confirmed
  allocations were already committed; replay simply removes the token again.
  Stale cleanup events for Held, Rejected, Released, or missing operations do
  nothing and cannot remove a different owner's token.
- Release first commits an independent, locked SQL Released tombstone, before
  Redis cleanup or the consumer-outbox mutation. A durable
  `AllocationsPendingRelease` flag records whether confirmed allocations still
  require removal. Thus even a Redis outage cannot allow a late Hold/Confirm to
  resurrect the workflow. The consumer transaction then removes only this
  correlation's Redis token and confirmed allocations through the aggregate,
  clears the pending flag, and commits the Released outcome atomically with
  cleanup completion. Redis failure never acknowledges release: the fence
  survives while allocation removal rolls back and retry resumes cleanup.
  If SQL rolls back after Redis removal, retry also completes safely.
- A Lua acquisition surviving a SQL rollback can be retried by the same owner.
  Tokens without a committed operation/outcome remain conservative until expiry.

Redis unavailability propagates for retry; no success or default availability is
fabricated. Losing Redis data invalidates existing unconfirmed holds. Never
reconstruct Redis tokens from Held SQL metadata. After Redis restart, old Confirm
fails closed and old confirmed allocations remain in SQL; new acquires always
include those confirmed counts. Late Lua completion after a cancelled/timed-out
call can only leave an orphan temporary token: SQL confirmation still requires a
committed Held operation, exact token, and fresh locked capacity checks.
If Redis loses a hold immediately after confirmation validation, the SQL mutation
lock still serializes subsequent confirms/acquires against newly committed
allocations. PostgreSQL's physical-seat unique index is the final collision
defense. Retriable uniqueness/concurrency failures roll back the entire SQL
consumer transaction and re-read fresh inventory on redelivery; programming or
infrastructure exceptions are not converted into business rejection messages.
Do not delete ledger tombstones while late deliveries remain possible.

All service instances must share the same Redis database/key and PostgreSQL
database, and have synchronized clocks (expiry enforcement itself uses Redis
server time). The script uses exactly one key and therefore one Redis hash slot;
this is not a sharded Redis Cluster inventory design. A global hash scan and SQL
singleton lock bound scalability. Do not shard keys or weaken locking without
replacing the all-itinerary atomicity protocol.

## Internal setup

Required runtime settings (supply secrets externally):

```text
ConnectionStrings__SeatingDatabase=Host=...;Database=...;Username=...;Password=...
ConnectionStrings__InventoryRedis=host:6379,password=...
RabbitMq__Host=...
RabbitMq__Port=5672
RabbitMq__VHost=/
RabbitMq__User=...
RabbitMq__Pass=...
RabbitMq__UseSsl=true
```

`Inventory__RedisKey` optionally overrides the shared hash key; changing it
invalidates outstanding holds and must be coordinated across all instances.
Use Redis persistence and normal secured network configuration, but safety does
not depend on retaining temporary holds.

For a **new, empty, dedicated** service database, run the internal one-shot schema
initializer (not automatic startup behavior):

```powershell
dotnet run --project backend\seating-service -- --initialize-database
```

This uses EF `EnsureCreated`, including the mutation guard and outbox tables.
It is a first-schema bootstrap, not an upgrade/migration mechanism; existing
deployments require reviewed SQL/migrations before schema changes.

Provision flight/cabin capacity and aircraft seat membership through trusted
internal deployment/import tooling only. For example, against the new schema:

```sql
BEGIN;
SELECT * FROM "InventoryMutationLock" WHERE "Id" = 1 FOR UPDATE;
INSERT INTO "SeatInventories"
    ("FlightId", "CabinId", "TotalSeats", "OverbookingAllowance", "ReservedSeats")
VALUES ('flight-reference', 'economy-reference', 2, 0, 0);
INSERT INTO "Seats" ("FlightId", "CabinId", "SeatId")
VALUES ('flight-reference', 'economy-reference', '1A'),
       ('flight-reference', 'economy-reference', '1B');
COMMIT;
```

Use owning-service canonical IDs without normalization. Do not directly mutate
confirmed assignments or `ReservedSeats`, remove allocated seats, or lower
capacity below current allocations/outstanding holds. Dynamic aircraft changes,
capacity/overbooking policy updates, and inventory provisioning event consumers
are deferred; the first milestone requires pre-provisioned inventories.

## Tests

Unit source includes domain tests and SQLite ledger/protocol tests with an
explicit test-only removal of PostgreSQL `FOR UPDATE` syntax and fake hold
storage. These test application transitions, not Redis Lua or database
concurrency guarantees.

The external integration project executes the actual Lua against Redis and
actual PostgreSQL transactions/constraints. It creates a unique PostgreSQL schema
and Redis key per fixture, deletes only those resources, and tests concurrent
last-capacity acquisition, multi-selection atomicity, owner-safe release, loss,
expiry, and retry after SQL rollback. It requires a dedicated test database user
with `CREATE SCHEMA` permission:

```powershell
$env:SEATING_TEST_POSTGRES = 'Host=...;Database=...;Username=...;Password=...'
$env:SEATING_TEST_REDIS = 'localhost:6379'
dotnet test backend\tests\integration\seating-service.integration.test --filter Category=ExternalInventory
```

Missing external configuration fails with setup instructions; tests do not
silently skip or substitute an in-memory Redis/PostgreSQL implementation.
These direct participant tests do not validate broker delivery. MassTransit
consumer/outbox wiring is configured in the host; full RabbitMQ delivery and
crash-recovery verification remains an external end-to-end check.

Test execution for this implementation session was explicitly deferred by the
user. Build validation is not a claim that unit, Redis, PostgreSQL, or broker
integration tests passed.
