using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using seating_service.Application.Abstractions;
using seating_service.Application.Features.Inventory;
using seating_service.Domain.Entities;
using seating_service.Infrastructure;
using seating_service.Infrastructure.Persistence;
using Shared.Integration.Contracts;

namespace seating_service.unit.test;

public sealed class InventoryProtocolTests
{
    [Fact]
    public async Task PostCommitCleanupIsIdempotentAndRestoresSellableCapacityWithoutNewOutcome()
    {
        await using var fixture = await Fixture.Create(capacity: 2);
        var first = fixture.Command("first");
        await fixture.Run(participant => participant.Hold(first, default));
        await fixture.Run(participant => participant.Confirm(new(first.CorrelationId), default));
        Assert.Single(fixture.Holds.Active);
        var outcomeCount = fixture.Published.Count;

        await fixture.Cleanup(first.CorrelationId);
        await fixture.Cleanup(first.CorrelationId);
        Assert.Empty(fixture.Holds.Active);
        Assert.Equal(outcomeCount, fixture.Published.Count);
        Assert.Equal(1, await fixture.Allocations());

        var second = fixture.Command("second");
        await fixture.Run(participant => participant.Hold(second, default));
        Assert.Equal(InventoryOperationState.Held, await fixture.State(second.CorrelationId));
    }

    [Fact]
    public async Task CleanupFailureRetainsConfirmedSqlAndTokenUntilRetry()
    {
        await using var fixture = await Fixture.Create();
        var hold = fixture.Command("passenger");
        await fixture.Run(participant => participant.Hold(hold, default));
        await fixture.Run(participant => participant.Confirm(new(hold.CorrelationId), default));
        fixture.Holds.Unavailable = true;

        await Assert.ThrowsAsync<InjectedFailure>(() => fixture.Cleanup(hold.CorrelationId));
        Assert.Equal(InventoryOperationState.Confirmed, await fixture.State(hold.CorrelationId));
        Assert.Equal(1, await fixture.Allocations());
        Assert.Single(fixture.Holds.Active);

        fixture.Holds.Unavailable = false;
        await fixture.Cleanup(hold.CorrelationId);
        Assert.Empty(fixture.Holds.Active);
        Assert.Equal(1, await fixture.Allocations());
    }

    [Fact]
    public async Task StaleCleanupDoesNotRemoveHeldOrOtherOwnersToken()
    {
        await using var fixture = await Fixture.Create();
        var held = fixture.Command("held");
        await fixture.Run(participant => participant.Hold(held, default));
        await fixture.Cleanup(held.CorrelationId);
        await fixture.Cleanup(Guid.NewGuid());
        Assert.True(fixture.Holds.Active.ContainsKey(held.CorrelationId));

        var confirmed = fixture.Command("confirmed");
        await fixture.Run(participant => participant.Hold(confirmed, default));
        await fixture.Run(participant => participant.Confirm(new(confirmed.CorrelationId), default));
        await fixture.Cleanup(confirmed.CorrelationId);
        Assert.True(fixture.Holds.Active.ContainsKey(held.CorrelationId));
        Assert.False(fixture.Holds.Active.ContainsKey(confirmed.CorrelationId));

        await fixture.Release(confirmed.CorrelationId);
        await fixture.Cleanup(confirmed.CorrelationId);
        Assert.True(fixture.Holds.Active.ContainsKey(held.CorrelationId));
    }

    [Fact]
    public async Task DuplicateHoldConfirmAndReleaseAreIdempotent()
    {
        await using var fixture = await Fixture.Create();
        var hold = fixture.Command("passenger", "1A");
        await fixture.Run(participant => participant.Hold(hold, default));
        await fixture.Run(participant => participant.Hold(hold, default));
        Assert.Single(fixture.Holds.Active);

        await fixture.Run(participant => participant.Confirm(new(hold.CorrelationId), default));
        await fixture.Run(participant => participant.Confirm(new(hold.CorrelationId), default));
        Assert.Equal(1, await fixture.Allocations());
        Assert.Single(fixture.Holds.Active);
        Assert.Equal(2, fixture.Published.OfType<ReservationInventoryConfirmed>().Count());

        await fixture.Release(hold.CorrelationId);
        await fixture.Release(hold.CorrelationId);
        Assert.Equal(0, await fixture.Allocations());
        Assert.Empty(fixture.Holds.Active);
        Assert.Equal(InventoryOperationState.Released, await fixture.State(hold.CorrelationId));
    }

    [Fact]
    public async Task CompensationBeforeHoldCreatesDurableTerminalTombstone()
    {
        await using var fixture = await Fixture.Create();
        var hold = fixture.Command("passenger");
        await fixture.Release(hold.CorrelationId);
        await fixture.Run(participant => participant.Hold(hold, default));
        await fixture.Run(participant => participant.Confirm(new(hold.CorrelationId), default));

        Assert.Equal(InventoryOperationState.Released, await fixture.State(hold.CorrelationId));
        Assert.Empty(fixture.Holds.Active);
        Assert.Equal(0, await fixture.Allocations());
        Assert.All(fixture.Published.OfType<ReservationInventoryRejected>(), rejected =>
            Assert.Equal("cancelled", rejected.Code));
    }

    [Fact]
    public async Task RejectedWorkflowCannotBeRevivedWithDifferentSelection()
    {
        await using var fixture = await Fixture.Create();
        var hold = fixture.Command("passenger", "unprovisioned");
        await fixture.Run(participant => participant.Hold(hold, default));
        await fixture.Run(participant => participant.Hold(
            hold with { Selections = [new("passenger", "flight", "cabin", null)] }, default));

        Assert.Equal(InventoryOperationState.Rejected, await fixture.State(hold.CorrelationId));
        Assert.Empty(fixture.Holds.Active);
    }

    [Fact]
    public async Task RedisLossFailsOldConfirmClosedAndCannotRecreateHold()
    {
        await using var fixture = await Fixture.Create();
        var hold = fixture.Command("passenger");
        await fixture.Run(participant => participant.Hold(hold, default));
        fixture.Holds.Active.Clear();
        await fixture.Run(participant => participant.Confirm(new(hold.CorrelationId), default));
        await fixture.Run(participant => participant.Hold(hold, default));

        Assert.Equal(0, await fixture.Allocations());
        Assert.Empty(fixture.Holds.Active);
        Assert.Contains(fixture.Published.OfType<ReservationInventoryRejected>(),
            rejected => rejected.Code == "hold_expired_or_lost");
    }

    [Fact]
    public async Task RedisLossPreservesConfirmedSqlCapacity()
    {
        await using var fixture = await Fixture.Create(capacity: 1);
        var first = fixture.Command("first");
        await fixture.Run(participant => participant.Hold(first, default));
        await fixture.Run(participant => participant.Confirm(new(first.CorrelationId), default));
        fixture.Holds.Active.Clear();

        var second = fixture.Command("second");
        await fixture.Run(participant => participant.Hold(second, default));
        await fixture.Run(participant => participant.Confirm(new(second.CorrelationId), default));

        Assert.Equal(1, await fixture.Allocations());
        Assert.Equal(InventoryOperationState.Rejected, await fixture.State(second.CorrelationId));
        await fixture.Run(participant => participant.Confirm(new(first.CorrelationId), default));
        Assert.IsType<ReservationInventoryConfirmed>(fixture.Published.Last());
    }

    [Fact]
    public async Task ExpiryAllowsNewHoldButOldConfirmRejects()
    {
        await using var fixture = await Fixture.Create(capacity: 1);
        var first = fixture.Command("first");
        await fixture.Run(participant => participant.Hold(first, default));
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(11);
        var second = fixture.Command("second");
        await fixture.Run(participant => participant.Hold(second, default));
        await fixture.Run(participant => participant.Confirm(new(first.CorrelationId), default));
        await fixture.Run(participant => participant.Confirm(new(second.CorrelationId), default));

        Assert.Equal(1, await fixture.Allocations());
        Assert.Equal(InventoryOperationState.Rejected, await fixture.State(first.CorrelationId));
    }

    [Fact]
    public async Task DuplicateHoldDoesNotExtendOriginalDeadline()
    {
        await using var fixture = await Fixture.Create();
        var hold = fixture.Command("passenger");
        await fixture.Run(participant => participant.Hold(hold, default));
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(9);
        await fixture.Run(participant => participant.Hold(hold, default));
        fixture.Clock.Now = fixture.Clock.Now.AddMinutes(2);
        await fixture.Run(participant => participant.Confirm(new(hold.CorrelationId), default));

        Assert.Equal(InventoryOperationState.Rejected, await fixture.State(hold.CorrelationId));
        Assert.Equal(0, await fixture.Allocations());
    }

    [Fact]
    public async Task OverbookingPermitsCabinAllocationWithoutFabricatingPhysicalSeats()
    {
        await using var fixture = await Fixture.Create(capacity: 1, overbookingAllowance: 1);
        var assigned = fixture.Command("assigned", "1A");
        var unassigned = fixture.Command("unassigned");
        await fixture.Run(participant => participant.Hold(assigned, default));
        await fixture.Run(participant => participant.Hold(unassigned, default));
        await fixture.Run(participant => participant.Confirm(new(assigned.CorrelationId), default));
        await fixture.Run(participant => participant.Confirm(new(unassigned.CorrelationId), default));

        Assert.Equal(2, await fixture.Allocations());
        var unknownSeat = fixture.Command("unknown-seat", "1B");
        await fixture.Run(participant => participant.Hold(unknownSeat, default));
        Assert.Contains(fixture.Published.OfType<ReservationInventoryRejected>(),
            rejected => rejected.CorrelationId == unknownSeat.CorrelationId && rejected.Code == "inventory_not_provisioned");
    }

    [Fact]
    public async Task AcquireSuccessThenSqlRollbackRetriesSameOwnerWithoutDoubleCounting()
    {
        await using var fixture = await Fixture.Create(capacity: 1);
        var hold = fixture.Command("passenger");
        await Assert.ThrowsAsync<InjectedFailure>(() => fixture.Run(
            participant => participant.Hold(hold, default), failBeforeCommit: true));
        Assert.Single(fixture.Holds.Active);

        await fixture.Run(participant => participant.Hold(hold, default));
        await fixture.Run(participant => participant.Confirm(new(hold.CorrelationId), default));
        Assert.Single(fixture.Holds.Active);
        Assert.Equal(1, await fixture.Allocations());
    }

    [Fact]
    public async Task UnavailableRedisDoesNotPublishHeldOrReleaseAndRollsBackSql()
    {
        await using var fixture = await Fixture.Create();
        var hold = fixture.Command("passenger");
        fixture.Holds.Unavailable = true;
        await Assert.ThrowsAsync<InjectedFailure>(() =>
            fixture.Run(participant => participant.Hold(hold, default)));
        Assert.Empty(fixture.Published);
        fixture.Holds.Unavailable = false;
        await fixture.Run(participant => participant.Hold(hold, default));
        await fixture.Run(participant => participant.Confirm(new(hold.CorrelationId), default));

        fixture.Holds.Unavailable = true;
        await Assert.ThrowsAsync<InjectedFailure>(() =>
            fixture.Release(hold.CorrelationId));
        Assert.Equal(1, await fixture.Allocations());
        Assert.Equal(InventoryOperationState.Released, await fixture.State(hold.CorrelationId));
        Assert.DoesNotContain(fixture.Published, message => message is ReservationInventoryReleased);
        await fixture.Run(participant => participant.Hold(hold, default));
        Assert.IsType<ReservationInventoryRejected>(fixture.Published.Last());
        fixture.Holds.Unavailable = false;
        await fixture.Release(hold.CorrelationId);
        Assert.Equal(0, await fixture.Allocations());
    }

    [Fact]
    public async Task MultiBucketAcquireIsAtomicAndReleaseCannotStealAnotherOwner()
    {
        await using var fixture = await Fixture.Create(capacity: 1);
        var first = fixture.Command("first", "1A");
        await fixture.Run(participant => participant.Hold(first, default));
        var second = fixture.Command("second") with
        {
            Selections = [new("second", "flight", "cabin", null), new("second", "other", "cabin", null)]
        };
        await fixture.Run(participant => participant.Hold(second, default));
        Assert.Single(fixture.Holds.Active);

        await fixture.Release(second.CorrelationId);
        Assert.True(fixture.Holds.Active.ContainsKey(first.CorrelationId));
        await fixture.Run(participant => participant.Confirm(new(first.CorrelationId), default));
        Assert.Equal(1, await fixture.Allocations());
    }

    [Fact]
    public void HoldKeysPreserveOpaqueIdsAndCanonicalizeExpiryPrecision()
    {
        var id = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow.AddTicks(7);
        var first = InventoryKeys.Hold(id, time, [new("P", "F", "C", null)]);
        var second = InventoryKeys.Hold(id, time.ToOffset(TimeSpan.FromHours(-5)), [new("P", "F", "C", null)]);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.NotEqual(InventoryKeys.Bucket(" F", "C"), InventoryKeys.Bucket("F", "C"));
        Assert.NotEqual(InventoryKeys.Bucket("f", "C"), InventoryKeys.Bucket("F", "C"));
        Assert.NotEqual(InventoryKeys.Bucket("a|b", "c"), InventoryKeys.Bucket("a", "b|c"));
    }

    [Fact]
    public void ValidationRejectsDuplicatePassengersSeatsAndExcessiveDeadline()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new HoldReservationInventory(
            Guid.NewGuid(), "reservation", now.AddMinutes(10), [new("P", "F", "C", null)]);
        Assert.True(InventoryKeys.IsValid(command, now));
        Assert.False(InventoryKeys.IsValid(command with { HoldUntil = now.AddMinutes(11) }, now));
        Assert.False(InventoryKeys.IsValid(command with
        {
            Selections = [new("P", "F", "C", null), new("P", "F", "C", null)]
        }, now));
        Assert.False(InventoryKeys.IsValid(command with
        {
            Selections = [new("P", "F", "C", "1A"), new("Q", "F", "other", "1A")]
        }, now));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<SeatingDbContext> options;
        private readonly IPublishEndpoint publisher;
        public readonly Clock Clock = new();
        public readonly FakeHolds Holds;
        public List<object> Published => ((PublishRecorder)publisher).Messages;

        private Fixture(SqliteConnection connection)
        {
            this.connection = connection;
            options = new DbContextOptionsBuilder<SeatingDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(new SqliteLockSyntax())
                .Options;
            publisher = DispatchProxy.Create<IPublishEndpoint, PublishRecorder>();
            Holds = new FakeHolds(Clock);
        }

        public static async Task<Fixture> Create(int capacity = 2, int overbookingAllowance = 0)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            connection.CreateCollation("C", StringComparer.Ordinal.Compare);
            var fixture = new Fixture(connection);
            await using var database = new SeatingDbContext(fixture.options);
            await database.Database.EnsureCreatedAsync();
            database.Inventories.AddRange(
                SeatInventory.Create("flight", "cabin", capacity, overbookingAllowance),
                SeatInventory.Create("other", "cabin", capacity, overbookingAllowance));
            database.Seats.Add(new InventorySeat { FlightId = "flight", CabinId = "cabin", SeatId = "1A" });
            await database.SaveChangesAsync();
            return fixture;
        }

        public HoldReservationInventory Command(string passenger, string? seat = null)
        {
            return new(Guid.NewGuid(), Guid.NewGuid().ToString(), Clock.Now.AddMinutes(10),
                [new(passenger, "flight", "cabin", seat)]);
        }

        public async Task Run(Func<InventoryParticipant, Task> action, bool failBeforeCommit = false)
        {
            await using var database = new SeatingDbContext(options);
            await using var transaction = await database.Database.BeginTransactionAsync();
            await action(new InventoryParticipant(database, Holds, new ExistingReleaseFence(), publisher, Clock));
            if (failBeforeCommit)
            {
                throw new InjectedFailure();
            }

            await transaction.CommitAsync();
        }

        public async Task Release(Guid correlationId)
        {
            await new InventoryReleaseFence(options).EnsureReleased(correlationId, default);
            await Run(participant => participant.Release(new(correlationId), default));
        }

        public async Task Cleanup(Guid correlationId)
        {
            await using var database = new SeatingDbContext(options);
            await using var transaction = await database.Database.BeginTransactionAsync();
            await new ConfirmedInventoryCleanup(database, Holds).Cleanup(correlationId, default);
            await transaction.CommitAsync();
        }

        public async Task<int> Allocations()
        {
            await using var database = new SeatingDbContext(options);
            return await database.Assignments.CountAsync();
        }

        public async Task<InventoryOperationState> State(Guid id)
        {
            await using var database = new SeatingDbContext(options);
            return (await database.Operations.FindAsync(id))!.State;
        }

        public ValueTask DisposeAsync()
        {
            return connection.DisposeAsync();
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return Now;
        }
    }

    private sealed class InjectedFailure : Exception;

    private sealed class ExistingReleaseFence : IInventoryReleaseFence
    {
        public Task EnsureReleased(Guid correlationId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    // SQLite tests exercise ledger/domain transitions, not PostgreSQL row-lock concurrency.
    private sealed class SqliteLockSyntax : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            command.CommandText = command.CommandText.Replace(" FOR UPDATE", string.Empty, StringComparison.Ordinal);
            return ValueTask.FromResult(result);
        }
    }

    public class PublishRecorder : DispatchProxy
    {
        public readonly List<object> Messages = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IPublishEndpoint.Publish))
            {
                throw new NotSupportedException(targetMethod?.Name);
            }

            Messages.Add(args![0]!);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHolds(Clock clock) : IInventoryHolds
    {
        public Dictionary<Guid, InventoryHold> Active { get; } = new();
        public bool Unavailable { get; set; }

        public Task<bool> Acquire(
            InventoryHold hold,
            HoldCapacity[] capacities,
            string[] confirmedPassengers,
            string[] confirmedSeats,
            CancellationToken cancellationToken)
        {
            Check(cancellationToken);
            if (Active.TryGetValue(hold.Owner, out var existing))
            {
                if (JsonSerializer.Serialize(existing) != JsonSerializer.Serialize(hold))
                {
                    return Task.FromResult(false);
                }
            }

            var items = Active.Values
                .Where(value => value.Owner != hold.Owner)
                .SelectMany(value => value.Items)
                .Concat(hold.Items)
                .ToArray();
            if (items.Select(item => item.Passenger).Concat(confirmedPassengers).Distinct().Count() !=
                items.Length + confirmedPassengers.Length ||
                items.Where(item => item.Seat is not null).Select(item => item.Seat).Concat(confirmedSeats).Distinct().Count() !=
                items.Count(item => item.Seat is not null) + confirmedSeats.Length ||
                capacities.Any(capacity =>
                    capacity.Confirmed + items.Count(item => item.Bucket == capacity.Bucket) > capacity.BookingLimit ||
                    capacity.ConfirmedPhysical + items.Count(item => item.Bucket == capacity.Bucket && item.Seat is not null) >
                    capacity.PhysicalLimit))
            {
                return Task.FromResult(false);
            }

            Active[hold.Owner] = hold;
            return Task.FromResult(true);
        }

        public Task<bool> IsValid(InventoryHold hold, CancellationToken cancellationToken)
        {
            Check(cancellationToken);
            return Task.FromResult(Active.TryGetValue(hold.Owner, out var existing) &&
                JsonSerializer.Serialize(existing) == JsonSerializer.Serialize(hold));
        }

        public Task Release(Guid owner, CancellationToken cancellationToken)
        {
            Check(cancellationToken);
            Active.Remove(owner);
            return Task.CompletedTask;
        }

        private void Check(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Unavailable)
            {
                throw new InjectedFailure();
            }

            foreach (var owner in Active.Where(pair => pair.Value.ExpiresAt <= clock.Now).Select(pair => pair.Key).ToArray())
            {
                Active.Remove(owner);
            }
        }
    }
}
