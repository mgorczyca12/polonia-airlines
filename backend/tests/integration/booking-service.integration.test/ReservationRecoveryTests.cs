extern alias seating;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Infrastructure.Integrations.Sagas;
using booking_service.Infrastructure.Persistence;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Shared.Integration.Contracts;
using Fixture = booking_service.integration.test.ReservationBrokerWorkflowTests.Fixture;
using SeatingDbContext = seating::seating_service.Infrastructure.Persistence.SeatingDbContext;

namespace booking_service.integration.test;

[Trait("Category", "ExternalWorkflow")]
public sealed class ReservationRecoveryTests
{
    [Fact]
    public async Task Confirmation_RedisHoldLostDuringSeatingRestart_CancelsInsteadOfReconstructingHold()
    {
        await using var fixture = await Fixture.Create(true, startInProcess: false);
        await using var database = new SeatingDbContext(fixture.SeatingOptions);
        await database.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION workflow_confirmation_gate() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF NEW."State" = 'Confirmed' THEN
                    PERFORM pg_advisory_xact_lock(987654321);
                END IF;
                RETURN NEW;
            END $$;
            CREATE TRIGGER workflow_confirmation_gate BEFORE UPDATE ON "Operations"
                FOR EACH ROW EXECUTE FUNCTION workflow_confirmation_gate();
            """);
        await using var gate = await database.Database.BeginTransactionAsync();
        await database.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(987654321)");

        var seating = await fixture.StartSeating();
        var booking = await fixture.StartBooking();
        await WaitForConsumers(fixture);
        var reservation = await CreateReservation(booking.Client, fixture.ProductId);
        await WaitForSaga(fixture, "ConfirmingAllocations");
        await WaitForUnacknowledged(fixture, "ConfirmInventory");
        Assert.True(await fixture.Redis.GetDatabase().KeyExistsAsync(fixture.RedisKey));

        await seating.Kill();
        Assert.Equal("Held", (await database.Operations.AsNoTracking().SingleAsync()).State.Code);
        Assert.Equal(0, await database.Assignments.CountAsync());
        Assert.True(await fixture.Redis.GetDatabase().KeyDeleteAsync(fixture.RedisKey));
        await gate.RollbackAsync();
        await fixture.StartSeating();

        ReservationResponse? latest = null;
        await WaitUntil(async () =>
        {
            latest = await booking.Client.GetFromJsonAsync<ReservationResponse>(
                $"/api/booking/reservations/{reservation.Id}");
            return latest?.WorkflowStatus == "Cancelled";
        }, "Restarted seating did not compensate the lost Redis hold.");
        Assert.NotNull(latest);
        Assert.Equal("Cancelled", latest.Status);
        Assert.Equal("hold_expired_or_lost", latest.FailureCode);
        await using var context = new SeatingDbContext(fixture.SeatingOptions);
        var operation = await context.Operations.SingleAsync();
        Assert.Equal("Released", operation.State.Code);
        Assert.False(operation.AllocationsPendingRelease);
        Assert.Equal(0, await context.Assignments.CountAsync());
        Assert.All(await context.Inventories.ToArrayAsync(), inventory => Assert.Equal(0, inventory.ReservedSeats));
        Assert.False(await fixture.Redis.GetDatabase().KeyExistsAsync(fixture.RedisKey));
        await fixture.AssertQueuesSettled();
    }

    [Fact]
    public async Task Reservation_KilledConsumers_RedeliversUnacknowledgedHoldAndRestoresSaga()
    {
        await using var fixture = await Fixture.Create(true, startInProcess: false);
        var seating = await fixture.StartSeating();
        var booking = await fixture.StartBooking();
        await WaitForConsumers(fixture);
        await using var database = new SeatingDbContext(fixture.SeatingOptions);
        await using var transaction = await database.Database.BeginTransactionAsync();
        await database.Database.ExecuteSqlRawAsync(
            """SELECT * FROM "InventoryMutationLock" WHERE "Id" = 1 FOR UPDATE""");

        var reservation = await CreateReservation(booking.Client, fixture.ProductId);
        await WaitForSaga(fixture, "AwaitingCapacity");
        await WaitForUnacknowledged(fixture, "HoldInventory");
        Assert.Equal(0, await database.Operations.CountAsync());
        Assert.False(await fixture.Redis.GetDatabase().KeyExistsAsync(fixture.RedisKey));

        await seating.Kill();
        await booking.Kill();
        await WaitUntil(async () =>
        {
            var queue = await fixture.Queue("HoldInventory");
            return queue.GetProperty("messages_ready").GetInt32() == 1;
        }, "Killed seating consumer did not requeue its unacknowledged hold.");
        await fixture.AssertRequeuedDelivery("HoldInventory");
        await transaction.RollbackAsync();

        await fixture.StartSeating();
        await WaitUntil(async () =>
        {
            await using var context = new SeatingDbContext(fixture.SeatingOptions);
            return (await context.Operations.AsNoTracking().SingleOrDefaultAsync())?.State.Code == "Held";
        }, "Restarted seating did not persist the redelivered hold.");
        Assert.True(await fixture.Redis.GetDatabase().KeyExistsAsync(fixture.RedisKey));
        await WaitForSaga(fixture, "AwaitingCapacity");

        var restartedBooking = await fixture.StartBooking();
        restartedBooking.Client.DefaultRequestHeaders.Add("X-Guest-Token", reservation.GuestAccessToken);
        await AssertCompleted(fixture, restartedBooking.Client, reservation);
    }

    [Fact]
    public async Task Confirmation_SqlRollbackAndBookingCrash_RecoversWithoutDuplicateAllocations()
    {
        await using var fixture = await Fixture.Create(true, startInProcess: false);
        await using var database = new SeatingDbContext(fixture.SeatingOptions);
        // Sequences survive transaction rollback; the trigger fails exactly the first confirmation attempt.
        await database.Database.ExecuteSqlRawAsync("""
            CREATE SEQUENCE workflow_confirmation_attempts;
            CREATE FUNCTION workflow_confirmation_gate() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF NEW."State" = 'Confirmed' AND OLD."State" = 'Held' THEN
                    IF nextval('workflow_confirmation_attempts') = 1 THEN
                        RAISE EXCEPTION 'Injected confirmation serialization failure' USING ERRCODE = '40001';
                    END IF;
                    PERFORM pg_advisory_xact_lock(987654321);
                END IF;
                RETURN NEW;
            END $$;
            CREATE TRIGGER workflow_confirmation_gate BEFORE UPDATE ON "Operations"
                FOR EACH ROW EXECUTE FUNCTION workflow_confirmation_gate();
            """);
        await using var gate = await database.Database.BeginTransactionAsync();
        await database.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(987654321)");

        await fixture.StartSeating();
        var booking = await fixture.StartBooking();
        await WaitForConsumers(fixture);
        var reservation = await CreateReservation(booking.Client, fixture.ProductId);
        await WaitForSaga(fixture, "ConfirmingAllocations");
        await WaitForUnacknowledged(fixture, "ConfirmInventory");
        await WaitUntil(async () =>
        {
            var attempts = await database.Database.SqlQueryRaw<long>(
                """SELECT last_value AS "Value" FROM workflow_confirmation_attempts""").SingleAsync();
            return attempts >= 2;
        }, "Production consumer retry did not retry the injected SQL failure.");
        Assert.Equal("Held", (await database.Operations.AsNoTracking().SingleAsync()).State.Code);
        Assert.Equal(0, await database.Assignments.CountAsync());
        Assert.True(await fixture.Redis.GetDatabase().KeyExistsAsync(fixture.RedisKey));

        await booking.Kill();
        await gate.RollbackAsync();
        await WaitUntil(async () =>
        {
            await using var context = new SeatingDbContext(fixture.SeatingOptions);
            return (await context.Operations.AsNoTracking().SingleAsync()).State.Code == "Confirmed"
                && !await fixture.Redis.GetDatabase().KeyExistsAsync(fixture.RedisKey);
        }, "Confirmation did not commit and clean its hold while booking was offline.");
        await WaitForSaga(fixture, "ConfirmingAllocations");
        await using (var context = new BookingDbContext(fixture.BookingOptions))
        {
            Assert.Equal("Created", (await context.Reservations.SingleAsync()).Status.Status);
        }

        var restartedBooking = await fixture.StartBooking();
        restartedBooking.Client.DefaultRequestHeaders.Add("X-Guest-Token", reservation.GuestAccessToken);
        await AssertCompleted(fixture, restartedBooking.Client, reservation);

        var publisher = fixture.CreatePublisher();
        await publisher.StartAsync();
        try
        {
            var messageId = Guid.NewGuid();
            var distinctMessageId = Guid.NewGuid();
            var confirm = new ConfirmReservationInventory(reservation.WorkflowId);
            await publisher.Publish(confirm, context => context.MessageId = messageId);
            await publisher.Publish(confirm, context => context.MessageId = messageId);
            await publisher.Publish(confirm, context => context.MessageId = distinctMessageId);
            await WaitUntil(async () =>
            {
                await using var context = new SeatingDbContext(fixture.SeatingOptions);
                return await context.Set<InboxState>().CountAsync(inbox =>
                    (inbox.MessageId == messageId || inbox.MessageId == distinctMessageId)
                    && inbox.Consumed != null) == 2;
            }, "Duplicate confirmation deliveries did not settle through the production inbox.");
        }
        finally
        {
            await publisher.StopAsync();
        }

        await AssertCompleted(fixture, restartedBooking.Client, reservation);
    }

    private static async Task<ReservationResponse> CreateReservation(HttpClient client, Guid productId)
    {
        using var checkoutResponse = await client.PostAsync("/api/booking/checkouts", null);
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);
        var checkout = await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(checkout);
        client.DefaultRequestHeaders.Add("X-Guest-Token", checkout.GuestToken);
        using var offerResponse = await client.PostAsJsonAsync("/api/booking/offers",
            new OfferRequest("PLN", 2, [new(" flight/one ", productId), new(" flight/two ", productId)]));
        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(offer);
        using var response = await client.PostAsJsonAsync("/api/booking/reservations",
            new ReservationRequest(offer.Id, "recovery", "Guest", "guest@example.test",
                [new("First", "One", new DateOnly(1990, 1, 1)), new("Second", "Two", new DateOnly(1992, 2, 2))]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reservation = await response.Content.ReadFromJsonAsync<ReservationResponse>();
        Assert.NotNull(reservation);
        return reservation;
    }

    private static async Task WaitForConsumers(Fixture fixture)
    {
        await fixture.WaitForSeatingConsumers();
    }

    private static async Task WaitForUnacknowledged(Fixture fixture, string name)
    {
        await WaitUntil(async () =>
        {
            var queue = await fixture.Queue(name);
            return queue.TryGetProperty("messages_unacknowledged", out JsonElement count) && count.GetInt32() == 1;
        }, $"No in-flight delivery was observed on {name}.");
    }

    private static async Task WaitForSaga(Fixture fixture, string state)
    {
        await WaitUntil(async () =>
        {
            await using var context = new BookingDbContext(fixture.BookingOptions);
            return (await context.Set<ReservationSagaState>().AsNoTracking().SingleOrDefaultAsync())?.CurrentState == state;
        }, $"Persisted booking saga did not reach {state}.");
    }

    private static async Task AssertCompleted(Fixture fixture, HttpClient client, ReservationResponse created)
    {
        ReservationResponse? latest = null;
        await WaitUntil(async () =>
        {
            latest = await client.GetFromJsonAsync<ReservationResponse>($"/api/booking/reservations/{created.Id}");
            return latest?.WorkflowStatus == "Completed";
        }, "Recovered reservation did not complete.");
        Assert.NotNull(latest);
        Assert.Equal("Reserved", latest.Status);
        Assert.Null(latest.FailureCode);
        Assert.Equal(created.WorkflowId, latest.WorkflowId);
        await WaitUntil(async () => !await fixture.Redis.GetDatabase().KeyExistsAsync(fixture.RedisKey),
            "Recovered confirmation did not clean its Redis hold.");
        await using var seating = new SeatingDbContext(fixture.SeatingOptions);
        Assert.Equal("Confirmed", (await seating.Operations.SingleAsync()).State.Code);
        Assert.Equal(1, await seating.Operations.CountAsync());
        Assert.Equal(4, await seating.Assignments.CountAsync());
        Assert.All(await seating.Inventories.ToArrayAsync(), inventory => Assert.Equal(2, inventory.ReservedSeats));
        await using var booking = new BookingDbContext(fixture.BookingOptions);
        Assert.Equal(1, await booking.Reservations.CountAsync());
        Assert.Equal(1, await booking.Set<ReservationSagaState>().CountAsync());
        await fixture.AssertQueuesSettled();
    }

    private static async Task WaitUntil(Func<Task<bool>> condition, string failure)
    {
        await ReservationBrokerWorkflowTests.WaitUntil(condition, () => failure);
    }
}
