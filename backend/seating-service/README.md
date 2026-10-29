# Seating inventory participant

The service consumes `HoldReservationInventory`, `ConfirmReservationInventory`,
and `ReleaseReservationInventory` and publishes their shared reservation-workflow
outcomes. `/health` is liveness only; there are no public inventory administration
or hold endpoints. All external IDs are opaque, ordinal, case-sensitive strings.

## Structure

The service follows the same layering as `flight-service`; `Program.cs` chains
`AddApi().AddApplication().AddInfrastructure()`.

```text
Api/Extensions/DependencyInjection.cs   health checks
Application/
  DependencyInjection.cs                shared DispatchR pipelines + validators
  Abstractions/                         IInventoryRepository, IInventoryOperationRepository,
                                        IInventoryMutationLock, IInventoryHolds, IInventoryReleaseFence
  Features/InventoryFeatures/
    Commands/{HoldInventory,ConfirmInventory,ReleaseInventory,CleanupConfirmedInventory}/
    Errors/                             InventoryRejectionCode + contract-string mapping
    InventoryKeys.cs                    Redis key/token encoding
Domain/
  Entities/                             SeatInventory, SeatAssignment, InventoryOperation, InventorySeat
  ValueObjects/                         InventoryOperationState, HeldSelection(s)
Infrastructure/
  DependencyInjection.cs                DbContext, options, Redis, MassTransit, repositories
  Persistence/                          SeatingDbContext, Configuration/ (one class per entity),
                                        Converters/, Redis/ (RedisInventoryHolds + options),
                                        InventoryMutationLock (persistence-only guard row)
  Repositories/                         repositories, SQL mutation lock, independent release fence
  Integrations/Consumers/               one thin consumer per message; each only sends a command
```

Hold, confirm, release and cleanup are DispatchR command handlers returning
`FluentResults.Result`. They are plain commands, not transactional/retryable
ones, because the MassTransit consumer outbox owns the SQL transaction on the
`DbContext`; handlers persist through the shared `IUnitOfWork` once per message
path, after publishing outcomes through `IIntegrationEventPublisher`. The Hold
request rule (`HoldInventoryRequestRule`) is invoked by the handler, not a
FluentValidation validator, so an invalid hold still becomes a durable Rejected
operation with `invalid_selection_or_deadline`. Confirm/Release/Cleanup commands
have validators requiring a non-empty correlation ID; such messages fail
validation and follow the retry/error-queue policy. `InventoryOperation` is an
aggregate that enforces Held to Confirmed/Rejected/Released transitions and the
release fence; its state is a smart enum stored as a string and its held
selections are stored as JSON in `SelectionsJson`.

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
`RabbitMq__Host`, `User`, `Pass` and `ConnectionStrings__InventoryRedis` are
bound to validated options and checked when the host starts (`RabbitMq` uses
MassTransit's `RabbitMqTransportOptions`, like flight-service).
Use Redis persistence and normal secured network configuration, but safety does
not depend on retaining temporary holds.

For a **new, empty, dedicated** service database, the schema must be created
before the service starts. The service has no `--initialize-database` flag and
does not call `EnsureCreated` at startup; schema creation is deferred to
migrations, and **no migrations are checked in yet**. Schema provisioning is a
prerequisite and must include the `Operations`, `SeatInventories`, `Assignments`,
`Seats` and `InventoryMutationLock` tables, the MassTransit inbox/outbox tables,
the unique and filtered unique indexes, the `C` collations, and the seeded
`InventoryMutationLock` row (`Id = 1`, defined with `HasData` in its entity
configuration so a future migration will include it). The service fails closed
if the guard row is missing.

Provision flight/cabin capacity and aircraft seat membership through trusted
internal deployment/import tooling only. For example, against the provisioned schema:

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
These direct command-handler tests do not validate broker delivery. MassTransit
consumer/outbox wiring is configured in the host; full RabbitMQ delivery and
crash-recovery verification remains an external end-to-end check.

Test execution for this implementation session was explicitly deferred by the
user. Build validation is not a claim that unit, Redis, PostgreSQL, or broker
integration tests passed. The SQLite unit tests were run after the handler
restructure; the Redis/PostgreSQL integration project was only compiled, and
the MassTransit/DispatchR host wiring has not been exercised against a broker.
