using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using seating_service.Application.Abstractions;
using seating_service.Application.Features.InventoryFeatures.Commands.CleanupConfirmedInventory;
using seating_service.Application.Features.InventoryFeatures.Commands.ConfirmInventory;
using seating_service.Application.Features.InventoryFeatures.Commands.HoldInventory;
using seating_service.Application.Features.InventoryFeatures.Commands.ReleaseInventory;
using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;
using seating_service.Infrastructure.Persistence;
using seating_service.Infrastructure.Persistence.Redis;
using seating_service.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using Shared.Application.Events;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Persistence;
using Shared.Integration.Contracts;
using StackExchange.Redis;

namespace seating_service.integration.test;

// Requires explicitly configured external services. Missing configuration fails with setup instructions.
[Trait("Category", "ExternalInventory")]
public sealed class RedisPostgresInventoryTests
{
    [Fact(Skip = "Skipped untill external inventory services are available and configured properly")]
    public async Task CommittedConfirmationCleanupRemovesOnlyOwnerTokenAndRetainsSqlAllocation()
    {
        await using var fixture = await Fixture.Create();
        var first = fixture.Command("first");
        await fixture.Run(participant => participant.Hold(first, default));
        await fixture.Cleanup(first.CorrelationId);
        Assert.Equal(1, await fixture.RedisHoldCount());
        await fixture.Run(participant => participant.Confirm(new(first.CorrelationId), default));
        await fixture.Cleanup(first.CorrelationId);
        await fixture.Cleanup(first.CorrelationId);

        Assert.Equal(0, await fixture.RedisHoldCount());
        Assert.Equal(1, await fixture.Allocations());
        Assert.Equal(InventoryOperationState.Confirmed, await fixture.State(first.CorrelationId));
        var second = fixture.Command("other") with { Selections = [new("other", "other", "cabin", null)] };
        await fixture.Run(participant => participant.Hold(second, default));
        await fixture.Cleanup(first.CorrelationId);
        Assert.Equal(1, await fixture.RedisHoldCount());
        await fixture.Run(participant => participant.Confirm(new(second.CorrelationId), default));
        Assert.Equal(2, await fixture.Allocations());
    }

    [Fact(Skip = "Skipped untill external inventory services are available and configured properly")]
    public async Task ConcurrentLastSeatHasOneWinnerAndSqlSurvivesRedisLoss()
    {
        await using var fixture = await Fixture.Create();
        var commands = Enumerable.Range(0, 12).Select(index => fixture.Command($"P{index}")).ToArray();
        await Task.WhenAll(commands.Select(command =>
            fixture.Run(participant => participant.Hold(command, default))));

        var winner = await fixture.FindHeld();
        Assert.Single(winner);
        var correlationId = winner[0];
        await fixture.Run(participant => participant.Confirm(new(correlationId), default));
        await fixture.Run(participant => participant.Confirm(new(correlationId), default));
        Assert.Equal(1, await fixture.Allocations());

        await fixture.LoseRedisHolds();
        var another = fixture.Command("after-restart");
        await fixture.Run(participant => participant.Hold(another, default));
        Assert.Equal(InventoryOperationState.Rejected, await fixture.State(another.CorrelationId));
        Assert.Equal(1, await fixture.Allocations());
        await fixture.Run(participant => participant.Release(new(correlationId), default));
        await fixture.Run(participant => participant.Release(new(correlationId), default));
        Assert.Equal(0, await fixture.Allocations());
    }

    [Fact(Skip = "Skipped untill external inventory services are available and configured properly")]
    public async Task LuaAcquireIsAllOrNothingAndOwnershipCheckedAcrossItinerary()
    {
        await using var fixture = await Fixture.Create();
        var first = fixture.Command("first", "1A");
        await fixture.Run(participant => participant.Hold(first, default));
        var itinerary = fixture.Command("second") with
        {
            Selections = [new("second", "other", "cabin", null), new("second", "flight", "cabin", "1A")]
        };
        await fixture.Run(participant => participant.Hold(itinerary, default));
        Assert.Equal(InventoryOperationState.Rejected, await fixture.State(itinerary.CorrelationId));
        await fixture.Run(participant => participant.Release(new(itinerary.CorrelationId), default));

        var otherOnly = fixture.Command("third") with { Selections = [new("third", "other", "cabin", null)] };
        await fixture.Run(participant => participant.Hold(otherOnly, default));
        Assert.Equal(InventoryOperationState.Held, await fixture.State(otherOnly.CorrelationId));
        await fixture.Run(participant => participant.Confirm(new(first.CorrelationId), default));
        Assert.Equal(1, await fixture.Allocations());
    }

    [Fact(Skip = "Skipped untill external inventory services are available and configured properly")]
    public async Task LossAndExpiryRejectOldConfirmationAndTerminalReleasePreventsResurrection()
    {
        await using var fixture = await Fixture.Create();
        var lost = fixture.Command("lost");
        await fixture.Run(participant => participant.Hold(lost, default));
        await fixture.LoseRedisHolds();
        await fixture.Run(participant => participant.Confirm(new(lost.CorrelationId), default));
        await fixture.Run(participant => participant.Hold(lost, default));
        Assert.Equal(InventoryOperationState.Rejected, await fixture.State(lost.CorrelationId));

        var expiring = fixture.Command("expiring") with { HoldUntil = DateTimeOffset.UtcNow.AddSeconds(2) };
        await fixture.Run(participant => participant.Hold(expiring, default));
        await Task.Delay(TimeSpan.FromSeconds(3));
        await fixture.Run(participant => participant.Confirm(new(expiring.CorrelationId), default));
        Assert.Equal(InventoryOperationState.Rejected, await fixture.State(expiring.CorrelationId));

        var compensated = fixture.Command("compensated");
        await fixture.Run(participant => participant.Release(new(compensated.CorrelationId), default));
        await fixture.Run(participant => participant.Hold(compensated, default));
        await fixture.Run(participant => participant.Confirm(new(compensated.CorrelationId), default));
        Assert.Equal(InventoryOperationState.Released, await fixture.State(compensated.CorrelationId));
        Assert.Equal(0, await fixture.Allocations());
    }

    [Fact(Skip = "Skipped untill external inventory services are available and configured properly")]
    public async Task RedisAcquireSurvivingSqlRollbackIsIdempotentOnRetry()
    {
        await using var fixture = await Fixture.Create();
        var command = fixture.Command("passenger");
        await fixture.Run(participant => participant.Hold(command, default), commit: false);
        await fixture.Run(participant => participant.Hold(command, default));
        await fixture.Run(participant => participant.Confirm(new(command.CorrelationId), default));
        Assert.Equal(1, await fixture.Allocations());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string schema = "seating_test_" + Guid.NewGuid().ToString("N");
        private readonly string redisKey = "{seating-test-" + Guid.NewGuid().ToString("N") + "}:holds";
        private readonly DbContextOptions<SeatingDbContext> options;
        private readonly IConnectionMultiplexer redis;
        private readonly RedisInventoryHolds holds;
        private readonly DiscardPublisher publisher = new();

        private Fixture(string postgres, IConnectionMultiplexer redis)
        {
            this.redis = redis;
            var connectionString = new NpgsqlConnectionStringBuilder(postgres) { SearchPath = schema };
            options = new DbContextOptionsBuilder<SeatingDbContext>()
                .UseNpgsql(connectionString.ConnectionString)
                .Options;
            holds = new RedisInventoryHolds(
                redis,
                Options.Create(new RedisInventoryOptions { RedisKey = redisKey }));
        }

        public static async Task<Fixture> Create()
        {
            var postgres = Environment.GetEnvironmentVariable("SEATING_TEST_POSTGRES");
            var redisConnection = Environment.GetEnvironmentVariable("SEATING_TEST_REDIS");
            if (string.IsNullOrWhiteSpace(postgres) || string.IsNullOrWhiteSpace(redisConnection))
            {
                throw new InvalidOperationException(
                    "External inventory tests require SEATING_TEST_POSTGRES and SEATING_TEST_REDIS. " +
                    "Use a dedicated PostgreSQL database with CREATE SCHEMA permission and a test Redis instance.");
            }

            var redisOptions = ConfigurationOptions.Parse(redisConnection);
            redisOptions.BacklogPolicy = BacklogPolicy.FailFast;
            var redis = await ConnectionMultiplexer.ConnectAsync(redisOptions);
            var fixture = new Fixture(postgres, redis);
            try
            {
                await redis.GetDatabase().PingAsync();
                await using var database = new SeatingDbContext(fixture.options);
                using var identifierQuoter = new NpgsqlCommandBuilder();
                var createSchema = "CREATE SCHEMA " + identifierQuoter.QuoteIdentifier(fixture.schema);
                await database.Database.ExecuteSqlRawAsync(createSchema);
                await database.Database.ExecuteSqlRawAsync(database.Database.GenerateCreateScript());
                database.Inventories.AddRange(
                    SeatInventory.Create("flight", "cabin", 1),
                    SeatInventory.Create("other", "cabin", 1));
                database.Seats.Add(InventorySeat.Create("flight", "cabin", "1A"));
                await database.SaveChangesAsync();
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public HoldReservationInventory Command(string passenger, string? seat = null)
        {
            return new(Guid.NewGuid(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow.AddMinutes(10),
                [new(passenger, "flight", "cabin", seat)]);
        }

        public async Task Run(Func<InventoryHandlers, Task> action, bool commit = true)
        {
            await using var database = new SeatingDbContext(options);
            await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await action(new InventoryHandlers(
                database,
                holds,
                new InventoryReleaseFence(options),
                publisher,
                TimeProvider.System));
            if (commit)
            {
                await transaction.CommitAsync();
            }
        }

        public async Task<Guid[]> FindHeld()
        {
            await using var database = new SeatingDbContext(options);
            return await database.Operations
                .Where(operation => operation.State == InventoryOperationState.Held)
                .Select(operation => operation.Id)
                .ToArrayAsync();
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

        public async Task LoseRedisHolds()
        {
            await redis.GetDatabase().KeyDeleteAsync(redisKey);
        }

        public async Task Cleanup(Guid correlationId)
        {
            await using var database = new SeatingDbContext(options);
            await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            var handler = new CleanupConfirmedInventoryCommandHandler(
                new SqlInventoryMutationLock(database),
                new InventoryOperationRepository(database),
                holds);
            await handler.Handle(new CleanupConfirmedInventoryCommand(correlationId), default);
            await transaction.CommitAsync();
        }

        public async Task<long> RedisHoldCount()
        {
            return await redis.GetDatabase().HashLengthAsync(redisKey);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await LoseRedisHolds();
                await using var database = new SeatingDbContext(options);
                using var identifierQuoter = new NpgsqlCommandBuilder();
                var dropSchema = "DROP SCHEMA IF EXISTS " + identifierQuoter.QuoteIdentifier(schema) + " CASCADE";
                await database.Database.ExecuteSqlRawAsync(dropSchema);
            }
            finally
            {
                redis.Dispose();
            }
        }
    }

    private sealed class InventoryHandlers(
        SeatingDbContext database,
        IInventoryHolds holds,
        IInventoryReleaseFence releaseFence,
        IIntegrationEventPublisher publisher,
        TimeProvider timeProvider)
    {
        private readonly SqlInventoryMutationLock mutationLock = new(database);
        private readonly InventoryOperationRepository operations = new(database);
        private readonly InventoryRepository inventories = new(database);
        private readonly EfUnitOfWork<SeatingDbContext> unitOfWork = new(database, new DomainEventDispatcher([]));

        public async Task Hold(HoldReservationInventory message, CancellationToken cancellationToken)
        {
            var handler = new HoldInventoryCommandHandler(
                mutationLock, operations, inventories, holds, publisher, unitOfWork, timeProvider);
            var command = new HoldInventoryCommand(
                message.CorrelationId, message.ReservationId, message.HoldUntil, message.Selections);
            Assert.True((await handler.Handle(command, cancellationToken)).IsSuccess);
        }

        public async Task Confirm(ConfirmReservationInventory message, CancellationToken cancellationToken)
        {
            var handler = new ConfirmInventoryCommandHandler(
                mutationLock, operations, inventories, holds, publisher, unitOfWork);
            var result = await handler.Handle(new ConfirmInventoryCommand(message.CorrelationId), cancellationToken);
            Assert.True(result.IsSuccess);
        }

        public async Task Release(ReleaseReservationInventory message, CancellationToken cancellationToken)
        {
            var handler = new ReleaseInventoryCommandHandler(
                releaseFence, mutationLock, operations, inventories, holds, publisher, unitOfWork);
            var result = await handler.Handle(new ReleaseInventoryCommand(message.CorrelationId), cancellationToken);
            Assert.True(result.IsSuccess);
        }
    }

    private sealed class DiscardPublisher : IIntegrationEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
            where TEvent : class, IIntegrationEvent
        {
            return Task.CompletedTask;
        }
    }
}
