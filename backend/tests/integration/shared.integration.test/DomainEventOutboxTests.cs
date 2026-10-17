using DispatchR;
using DispatchR.Abstractions.Send;
using FluentResults;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions;
using Shared.Application.DependencyInjection;
using Shared.Application.Commands;
using Shared.Domain.Entities;
using Shared.Domain.Events;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.Messaging;
using Shared.Integration.Contracts;

namespace Shared.IntegrationTests;

public sealed class DomainEventOutboxTests
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Send_TransactionalCommand_PersistsBusinessAndOutboxOnlyForSuccess(bool succeed, int expected)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = CreateServices(connection);
        services.AddSharedApplication(typeof(DomainEventOutboxTests).Assembly);
        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            await context.Database.EnsureCreatedAsync();
            var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new OutboxCommand(succeed), CancellationToken.None);

            Assert.Equal(succeed, result.IsSuccess);
            Assert.Equal(expected, scope.ServiceProvider.GetRequiredService<HandlerState>().CreatedCount);
        }

        await using var readScope = provider.CreateAsyncScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<EventDbContext>();
        Assert.Equal(expected, await readContext.Aggregates.CountAsync());
        Assert.Equal(expected, await readContext.Set<OutboxMessage>().CountAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_DispatchesCascadingEventsAndPersistsOutboxWithoutImmediateDelivery()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateServices(connection).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        await context.Database.EnsureCreatedAsync();
        var aggregate = new EventAggregate("created");
        context.Aggregates.Add(aggregate);
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        Assert.Equal(0, await context.Set<OutboxMessage>().CountAsync());
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        context.ChangeTracker.Clear();
        Assert.Equal("handled", (await context.Aggregates.SingleAsync()).Name);
        var message = await context.Set<OutboxMessage>().SingleAsync();
        Assert.Contains(aggregate.Id.ToString(), message.Body);
        Assert.Equal(1, await context.Set<OutboxState>().CountAsync());
        Assert.Empty(aggregate.DomainEvents);
        var state = scope.ServiceProvider.GetRequiredService<HandlerState>();
        Assert.Equal(1, state.CreatedCount);
        Assert.Equal(1, state.FollowUpCount);
    }

    [Fact]
    public async Task RollbackTransactionAsync_RemovesBothBusinessChangesAndOutboxMessages()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateServices(connection).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        await context.Database.EnsureCreatedAsync();
        context.Aggregates.Add(new EventAggregate("created"));
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        Assert.Equal(1, await context.Aggregates.CountAsync());
        Assert.Equal(1, await context.Set<OutboxMessage>().CountAsync());
        await unitOfWork.RollbackTransactionAsync(CancellationToken.None);

        await using var readScope = provider.CreateAsyncScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<EventDbContext>();
        Assert.Equal(0, await readContext.Aggregates.CountAsync());
        Assert.Equal(0, await readContext.Set<OutboxMessage>().CountAsync());
        Assert.Equal(0, await readContext.Set<OutboxState>().CountAsync());
    }

    [Theory]
    [InlineData("exception")]
    [InlineData("cancellation")]
    [InlineData("nested-save")]
    public async Task SaveChangesAsync_HandlerFailure_PropagatesWithoutPersistingAndPreventsReuse(string outcome)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateServices(connection).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        await context.Database.EnsureCreatedAsync();
        context.Aggregates.Add(new EventAggregate("created"));
        var state = scope.ServiceProvider.GetRequiredService<HandlerState>();
        state.Outcome = outcome;
        using var cancellation = new CancellationTokenSource();
        state.Cancellation = cancellation;
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginTransactionAsync(cancellation.Token);

        if (outcome == "cancellation")
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                unitOfWork.SaveChangesAsync(cancellation.Token));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                unitOfWork.SaveChangesAsync(cancellation.Token));
        }

        await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.SaveChangesAsync(CancellationToken.None));
        await using var readScope = provider.CreateAsyncScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<EventDbContext>();
        Assert.Equal(0, await readContext.Aggregates.CountAsync());
        Assert.Equal(0, await readContext.Set<OutboxMessage>().CountAsync());
    }

    private static ServiceCollection CreateServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<EventDbContext>(options => options.UseSqlite(connection));
        services.AddSharedUnitOfWork<EventDbContext>();
        services.AddSharedInfrastructure();
        services.AddScoped<HandlerState>();
        services.AddDomainEventHandler<Created, CreatedHandler>();
        services.AddDomainEventHandler<Created, CreatedHandler>();
        services.AddDomainEventHandler<FollowUp, FollowUpHandler>();
        services.AddMassTransit(bus =>
        {
            bus.AddSharedEntityFrameworkOutbox<EventDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.DisableInboxCleanupService();
            });
            bus.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
        });
        return services;
    }

    public sealed record Created(Guid AggregateId) : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    }

    public sealed record FollowUp : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    }

    public sealed record CreatedIntegrationEvent(Guid AggregateId, DateTimeOffset OccurredAt) : IIntegrationEvent;

    public sealed class HandlerState
    {
        public int CreatedCount { get; set; }
        public int FollowUpCount { get; set; }
        public string Outcome { get; set; } = "success";
        public CancellationTokenSource? Cancellation { get; set; }
    }

    private sealed class CreatedHandler(
        EventDbContext context,
        IIntegrationEventPublisher publisher,
        IUnitOfWork unitOfWork,
        HandlerState state) : IDomainEventHandler<Created>
    {
        public async Task HandleAsync(Created domainEvent, CancellationToken cancellationToken)
        {
            state.CreatedCount++;
            await publisher.PublishAsync(
                new CreatedIntegrationEvent(domainEvent.AggregateId, domainEvent.OccurredAt),
                cancellationToken);

            if (state.Outcome == "exception")
            {
                throw new InvalidOperationException("Domain handler failed.");
            }

            if (state.Outcome == "cancellation")
            {
                state.Cancellation!.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (state.Outcome == "nested-save")
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var aggregate = await context.Aggregates.FindAsync(
                new object[] { domainEvent.AggregateId },
                cancellationToken);
            Assert.NotNull(aggregate);
            aggregate.MarkHandled();
        }
    }

    private sealed class FollowUpHandler(HandlerState state) : IDomainEventHandler<FollowUp>
    {
        public Task HandleAsync(FollowUp domainEvent, CancellationToken cancellationToken)
        {
            state.FollowUpCount++;
            return Task.CompletedTask;
        }
    }

    public sealed class EventDbContext(DbContextOptions<EventDbContext> options) : DbContext(options)
    {
        public DbSet<EventAggregate> Aggregates => Set<EventAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EventAggregate>().HasKey(aggregate => aggregate.Id);
            modelBuilder.Entity<EventAggregate>().Ignore(aggregate => aggregate.DomainEvents);
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }

    public sealed record OutboxCommand(bool Succeed) : ITransactionalCommand<OutboxCommand, Result>;

    public sealed class OutboxCommandHandler(EventDbContext context)
        : IRequestHandler<OutboxCommand, ValueTask<Result>>
    {
        public ValueTask<Result> Handle(OutboxCommand request, CancellationToken cancellationToken)
        {
            context.Aggregates.Add(new EventAggregate("created"));
            return ValueTask.FromResult(request.Succeed ? Result.Ok() : Result.Fail("Command rejected."));
        }
    }

    public sealed class EventAggregate : AggregateRoot<Guid>
    {
        private EventAggregate()
        {
        }

        public EventAggregate(string name) : base(Guid.NewGuid())
        {
            Name = name;
            AddDomainEvent(new Created(Id));
        }

        public string Name { get; private set; } = null!;

        public void MarkHandled()
        {
            Name = "handled";
            AddDomainEvent(new FollowUp());
        }
    }
}
