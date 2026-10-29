using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Persistence;
using booking_service.Infrastructure.Integrations.Sagas;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Integration.Contracts;

namespace booking_service.integration.test;

public sealed class ReservationSagaTests
{
    [Fact]
    public async Task SuccessfulInventoryConfirmation_ReservesBookingAndRetainsTerminalSaga()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Start();
        await fixture.Publish(new ReservationInventoryHeld(fixture.Id));
        Assert.True(await fixture.Harness.Published.Any<ConfirmReservationInventory>());
        await fixture.Publish(new ReservationInventoryConfirmed(fixture.Id));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Completed));
        Assert.Equal(ReservationStatus.Reserved, await fixture.ReservationStatus());

        await fixture.Publish(new ReservationRequested(
            fixture.Id, fixture.ReservationId.ToString(), fixture.Clock.GetUtcNow().AddMinutes(10), []));
        await fixture.Publish(new ReservationWorkflowExpired(fixture.Id));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Completed));
        Assert.Equal(ReservationStatus.Reserved, await fixture.ReservationStatus());
    }

    [Fact]
    public async Task RejectedInventory_WaitsForReleaseBeforeCancelling()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Start();
        await fixture.Publish(new ReservationInventoryRejected(fixture.Id, "inventory.unavailable"));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Compensating));
        Assert.Equal(ReservationStatus.Created, await fixture.ReservationStatus());
        Assert.True(await fixture.Harness.Published.Any<ReleaseReservationInventory>());

        await fixture.Publish(new ReservationInventoryReleased(fixture.Id));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Cancelled));
        Assert.Equal(ReservationStatus.Cancelled, await fixture.ReservationStatus());
    }

    [Fact]
    public async Task ExpirationAndLateSuccess_ReleaseRatherThanConfirm()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Start();
        fixture.Clock.Advance(TimeSpan.FromMinutes(11));
        await fixture.Publish(new ReservationWorkflowExpired(fixture.Id));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Compensating));

        await fixture.Publish(new ReservationInventoryConfirmed(fixture.Id));
        Assert.Equal(ReservationStatus.Created, await fixture.ReservationStatus());
        await fixture.Publish(new ReservationInventoryReleased(fixture.Id));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Cancelled));
        await fixture.Publish(new ReservationInventoryHeld(fixture.Id));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Cancelled));
        Assert.Equal(ReservationStatus.Cancelled, await fixture.ReservationStatus());
    }

    [Fact]
    public async Task StuckCompensation_EscalatesAndStillAcceptsRecovery()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Start();
        await fixture.Publish(new ReservationInventoryRejected(fixture.Id, "inventory.unavailable"));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Compensating));
        fixture.Clock.Advance(TimeSpan.FromMinutes(31));
        await fixture.Publish(new ReservationWorkflowExpired(fixture.Id));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.RequiresIntervention));
        await fixture.Publish(new ReservationInventoryReleased(fixture.Id));
        Assert.NotNull(await fixture.Saga.Exists(fixture.Id, machine => machine.Cancelled));
    }

    [Fact]
    public async Task PersistedDeadline_IsDispatchedAfterReopeningDatabaseScope()
    {
        await using var fixture = await Fixture.Create();
        await fixture.PersistExpiredDeadline();
        await fixture.DispatchDeadlines();

        Assert.True(await fixture.Harness.Published.Any<ReservationWorkflowExpired>());
        Assert.True(await fixture.NextDeadline() > fixture.Clock.GetUtcNow());
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }

        public void Advance(TimeSpan amount)
        {
            now += amount;
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider services;
        private readonly SqliteConnection connection;
        public Guid Id { get; } = Guid.NewGuid();
        public int ReservationId { get; private set; }
        public Clock Clock { get; }
        public ITestHarness Harness { get; }
        public ISagaStateMachineTestHarness<ReservationStateMachine, ReservationSagaState> Saga { get; }

        private Fixture(ServiceProvider services, SqliteConnection connection, Clock clock)
        {
            this.services = services;
            this.connection = connection;
            Clock = clock;
            Harness = services.GetRequiredService<ITestHarness>();
            Saga = Harness.GetSagaStateMachineHarness<ReservationStateMachine, ReservationSagaState>();
        }

        public static async Task<Fixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var clock = new Clock();
            var services = new ServiceCollection();
            services.AddSingleton<TimeProvider>(clock);
            services.AddDbContext<BookingDbContext>(options => options.UseSqlite(connection));
            services.AddScoped<CompleteReservationActivity>();
            services.AddScoped<CancelReservationActivity>();
            services.AddMassTransitTestHarness(bus =>
            {
                bus.AddSagaStateMachine<ReservationStateMachine, ReservationSagaState>().InMemoryRepository();
                bus.SetTestTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
            });
            var fixture = new Fixture(services.BuildServiceProvider(true), connection, clock);
            await using var scope = fixture.services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            await database.Database.EnsureCreatedAsync();
            var reservation = Reservation.Create(
                PnrCode.From("ABC123"), null, "Guest", "guest@example.com", ["Flight-1"], "PLN", clock.GetUtcNow());
            reservation.AddPassenger(
                "Guest", "Traveller", new DateOnly(1990, 1, 1),
                [FareQuote.Create("Flight-1", "BASIC", "Cabin-1", 100m, "PLN", BaggageAllowance.None, "Cabin-1", 1, "Basic")]);
            database.Reservations.Add(reservation);
            await database.SaveChangesAsync();
            fixture.ReservationId = reservation.Id;
            await fixture.Harness.Start();
            return fixture;
        }

        public async Task Start()
        {
            await Publish(new ReservationRequested(
                Id,
                ReservationId.ToString(),
                Clock.GetUtcNow().AddMinutes(10),
                [new InventorySelection("Passenger-1", "Flight-1", "Cabin-1", null)]));
            Assert.NotNull(await Saga.Exists(Id, machine => machine.AwaitingCapacity));
            Assert.True(await Harness.Published.Any<HoldReservationInventory>());
        }

        public async Task Publish<T>(T message)
            where T : class
        {
            await Harness.Bus.Publish(message);
            Assert.True(await Saga.Consumed.Any<T>());
        }

        public async Task<ReservationStatus> ReservationStatus()
        {
            await using var scope = services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            return (await database.Reservations.SingleAsync(reservation => reservation.Id == ReservationId)).Status;
        }

        public async Task PersistExpiredDeadline()
        {
            await using var scope = services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            database.Set<ReservationSagaState>().Add(new ReservationSagaState
            {
                CorrelationId = Id,
                ReservationId = ReservationId.ToString(),
                CurrentState = "AwaitingCapacity",
                HoldUntil = Clock.GetUtcNow().AddMinutes(-1),
                NextCheckAt = Clock.GetUtcNow().AddMinutes(-1)
            });
            await database.SaveChangesAsync();
        }

        public async Task DispatchDeadlines()
        {
            var worker = new ReservationDeadlineService(
                services.GetRequiredService<IServiceScopeFactory>(),
                Clock,
                NullLogger<ReservationDeadlineService>.Instance);
            await worker.PublishDueAsync(CancellationToken.None);
            worker.Dispose();
        }

        public async Task<DateTimeOffset> NextDeadline()
        {
            await using var scope = services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            return await database.Set<ReservationSagaState>()
                .Where(state => state.CorrelationId == Id)
                .Select(state => state.NextCheckAt)
                .SingleAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Harness.Stop();
            await services.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
