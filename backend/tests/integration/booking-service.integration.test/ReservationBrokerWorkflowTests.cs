extern alias seating;

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Integrations.Sagas;
using booking_service.Infrastructure.Persistence;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using SeatingDbContext = seating::seating_service.Infrastructure.Persistence.SeatingDbContext;
using SeatInventory = seating::seating_service.Domain.Entities.SeatInventory;
using InventoryOperationState = seating::seating_service.Domain.ValueObjects.InventoryOperationState;

namespace booking_service.integration.test;

[Trait("Category", "ExternalWorkflow")]
public sealed class ReservationBrokerWorkflowTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reservation_ThroughBroker_CompletesOrCompensatesItinerary(bool capacityAvailable)
    {
        await using var fixture = await Fixture.Create(capacityAvailable);
        using var client = fixture.Booking.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        using var checkoutResponse = await client.PostAsync("/api/booking/checkouts", null);
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);
        var checkout = await checkoutResponse.Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(checkout);
        client.DefaultRequestHeaders.Add("X-Guest-Token", checkout.GuestToken);

        using var offerResponse = await client.PostAsJsonAsync("/api/booking/offers",
            new OfferRequest("PLN", 2,
                [new(" flight/one ", fixture.ProductId), new(" flight/two ", fixture.ProductId)]));
        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(offer);

        var request = new ReservationRequest(offer.Id, "broker-workflow", "Guest", "guest@example.test",
            [new("First", "One", new DateOnly(1990, 1, 1)), new("Second", "Two", new DateOnly(1992, 2, 2))]);
        using var createdResponse = await client.PostAsJsonAsync("/api/booking/reservations", request);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<ReservationResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.WorkflowId);
        Assert.Equal(500m, created.TotalAmount);
        Assert.NotNull(createdResponse.Headers.Location);
        Assert.EndsWith($"/api/booking/reservations/{created.Id}", createdResponse.Headers.Location.ToString());

        var terminalState = capacityAvailable ? "Completed" : "Cancelled";
        ReservationResponse? latest = null;
        await WaitUntil(async () =>
        {
            latest = await client.GetFromJsonAsync<ReservationResponse>(
                $"/api/booking/reservations/{created.Id}");
            return latest?.WorkflowStatus == terminalState;
        }, () => $"Reservation {created.WorkflowId}: expected {terminalState}, got {latest?.WorkflowStatus}.");

        Assert.NotNull(latest);
        Assert.Equal(capacityAvailable ? "Reserved" : "Cancelled", latest.Status);
        if (capacityAvailable)
        {
            Assert.Null(latest.FailureCode);
        }
        else
        {
            Assert.Equal("inventory_unavailable", latest.FailureCode);
        }

        await WaitUntil(async () =>
        {
            await using var database = new SeatingDbContext(fixture.SeatingOptions);
            var operation = await database.Operations.AsNoTracking().SingleOrDefaultAsync();
            return operation?.State == (capacityAvailable
                    ? InventoryOperationState.Confirmed : InventoryOperationState.Released)
                && !operation.AllocationsPendingRelease
                && !await fixture.Redis.GetDatabase().KeyExistsAsync(fixture.RedisKey);
        }, () => "Seating confirmation cleanup or compensation did not settle.");

        await using var bookingDatabase = new BookingDbContext(fixture.BookingOptions);
        await using var seatingDatabase = new SeatingDbContext(fixture.SeatingOptions);
        var saga = await bookingDatabase.Set<ReservationSagaState>().SingleAsync();
        var operation = await seatingDatabase.Operations.SingleAsync();
        Assert.Equal(created.WorkflowId, saga.CorrelationId);
        Assert.Equal(created.WorkflowId, operation.CorrelationId);
        Assert.Equal(terminalState, saga.CurrentState);
        Assert.True(await bookingDatabase.Set<InboxState>().AnyAsync(state => state.Consumed != null));
        Assert.True(await seatingDatabase.Set<InboxState>().AnyAsync(state => state.Consumed != null));
        Assert.True((await bookingDatabase.CheckoutOffers.SingleAsync()).Consumed);

        var assignments = await seatingDatabase.Assignments.AsNoTracking().ToArrayAsync();
        Assert.Equal(capacityAvailable ? 4 : 0, assignments.Length);
        Assert.All(assignments, assignment =>
        {
            Assert.Equal(created.Id.ToString(CultureInfo.InvariantCulture), assignment.ReservationId);
            Assert.Null(assignment.SeatId);
        });
        if (capacityAvailable)
        {
            var passengerIds = await bookingDatabase.Reservations
                .Where(reservation => reservation.Id == created.Id)
                .SelectMany(reservation => reservation.Passengers)
                .Select(passenger => passenger.Id)
                .ToArrayAsync();
            foreach (var flight in new[] { " flight/one ", " flight/two " })
            {
                Assert.Equal(passengerIds.Select(id => id.ToString(CultureInfo.InvariantCulture)).Order(),
                    assignments.Where(assignment => assignment.FlightId == flight)
                        .Select(assignment => assignment.PassengerId).Order());
            }
        }

        Assert.All(await seatingDatabase.Inventories.AsNoTracking().ToArrayAsync(),
            inventory => Assert.Equal(capacityAvailable ? 2 : 0, inventory.ReservedSeats));

        using var replay = await client.PostAsJsonAsync("/api/booking/reservations", request);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        var replayed = await replay.Content.ReadFromJsonAsync<ReservationResponse>();
        Assert.NotNull(replayed);
        Assert.Equal(created.Id, replayed.Id);
        Assert.Equal(created.WorkflowId, replayed.WorkflowId);
        Assert.Equal(terminalState, replayed.WorkflowStatus);
        Assert.Equal(1, await bookingDatabase.Reservations.CountAsync());
        Assert.Equal(1, await seatingDatabase.Operations.CountAsync());
    }

    internal static async Task WaitUntil(Func<Task<bool>> condition, Func<string> failure)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(60))
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        Assert.Fail(failure());
    }

    internal sealed class Factory<TEntryPoint>(Dictionary<string, string?> settings)
        : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));
            builder.ConfigureServices(services => services.Configure<MassTransitHostOptions>(options =>
            {
                options.WaitUntilStarted = true;
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(15);
            }));
        }
    }

    internal sealed class Fixture : IAsyncDisposable
    {
        private readonly string postgres;
        private readonly HttpClient management;
        private readonly string bookingName = "booking_workflow_" + Guid.NewGuid().ToString("N");
        private readonly string seatingName = "seating_workflow_" + Guid.NewGuid().ToString("N");
        private readonly string vhost = "workflow_" + Guid.NewGuid().ToString("N");
        private readonly List<string> databases = [];
        private bool vhostCreated;
        private readonly Dictionary<string, string?> settings;
        private readonly List<WorkflowServiceProcess> processes = [];

        public Factory<Program> Booking { get; }
        public Factory<seating::Program> Seating { get; }
        public DbContextOptions<BookingDbContext> BookingOptions { get; }
        public DbContextOptions<SeatingDbContext> SeatingOptions { get; }
        public IConnectionMultiplexer Redis { get; }
        public string RedisKey { get; } = "{workflow-" + Guid.NewGuid().ToString("N") + "}:holds";
        public Guid ProductId { get; private set; }

        private Fixture(
            string postgres,
            IConnectionMultiplexer redis,
            HttpClient management,
            string host,
            string port,
            string user,
            string password)
        {
            this.postgres = postgres;
            this.management = management;
            Redis = redis;
            var bookingConnection = new NpgsqlConnectionStringBuilder(postgres) { Database = bookingName }.ConnectionString;
            var seatingConnection = new NpgsqlConnectionStringBuilder(postgres) { Database = seatingName }.ConnectionString;
            BookingOptions = new DbContextOptionsBuilder<BookingDbContext>().UseNpgsql(bookingConnection).Options;
            SeatingOptions = new DbContextOptionsBuilder<SeatingDbContext>().UseNpgsql(seatingConnection).Options;
            settings = new Dictionary<string, string?>
            {
                ["RabbitMq:Host"] = host,
                ["RabbitMq:Port"] = port,
                ["RabbitMq:User"] = user,
                ["RabbitMq:Pass"] = password,
                ["RabbitMq:VHost"] = vhost,
                ["ConnectionStrings:BookingDatabase"] = bookingConnection,
                ["ConnectionStrings:SeatingDatabase"] = seatingConnection,
                ["ConnectionStrings:InventoryRedis"] = Required("SEATING_TEST_REDIS"),
                ["Inventory:RedisKey"] = RedisKey,
                ["Logging:LogLevel:Default"] = "Warning"
            };
            Booking = new Factory<Program>(settings);
            Seating = new Factory<seating::Program>(settings);
        }

        public static async Task<Fixture> Create(bool capacityAvailable, bool startInProcess = true)
        {
            var postgres = Required("SEATING_TEST_POSTGRES");
            var redisConnection = Required("SEATING_TEST_REDIS");
            var host = Required("WORKFLOW_TEST_RABBITMQ_HOST");
            var port = Required("WORKFLOW_TEST_RABBITMQ_PORT");
            var user = Required("WORKFLOW_TEST_RABBITMQ_USER");
            var password = Required("WORKFLOW_TEST_RABBITMQ_PASSWORD");
            var managementUrl = new Uri(Required("WORKFLOW_TEST_RABBITMQ_MANAGEMENT"), UriKind.Absolute);
            var redis = await ConnectionMultiplexer.ConnectAsync(redisConnection);
            var management = new HttpClient { BaseAddress = managementUrl, Timeout = TimeSpan.FromSeconds(15) };
            management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
            var fixture = new Fixture(postgres, redis, management, host, port, user, password);
            try
            {
                await fixture.Initialize(capacityAvailable, user, startInProcess);
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        private static string Required(string variable)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"External workflow tests require {variable}. Use dedicated PostgreSQL, Redis and RabbitMQ instances.");
            }

            return value;
        }

        private async Task Initialize(bool capacityAvailable, string user, bool startInProcess)
        {
            await Redis.GetDatabase().PingAsync();
            await using var admin = new NpgsqlConnection(postgres);
            await admin.OpenAsync();
            using var quoter = new NpgsqlCommandBuilder();
            foreach (var database in new[] { bookingName, seatingName })
            {
                await using var create = new NpgsqlCommand("CREATE DATABASE " + quoter.QuoteIdentifier(database), admin);
                await create.ExecuteNonQueryAsync();
                databases.Add(database);
            }

            using var createVhost = await management.PutAsJsonAsync($"api/vhosts/{vhost}", new { });
            createVhost.EnsureSuccessStatusCode();
            vhostCreated = true;
            using var permissions = await management.PutAsJsonAsync(
                $"api/permissions/{vhost}/{Uri.EscapeDataString(user)}",
                new { configure = ".*", write = ".*", read = ".*" });
            permissions.EnsureSuccessStatusCode();

            await using (var booking = new BookingDbContext(BookingOptions))
            {
                await booking.Database.MigrateAsync();
                var product = FareProduct.Create("LIGHT", 1, "Economy", "Light", BaggageAllowance.None);
                ProductId = product.Id;
                booking.FareProducts.Add(product);
                booking.FlightPrices.AddRange(
                    FlightPrice.Create(" flight/one ", product.Id, "PLN", 125m, " cabin "),
                    FlightPrice.Create(" flight/two ", product.Id, "PLN", 125m, " cabin "));
                await booking.SaveChangesAsync();
            }

            await using (var seating = new SeatingDbContext(SeatingOptions))
            {
                await seating.Database.MigrateAsync();
                seating.Inventories.AddRange(
                    SeatInventory.Create(" flight/one ", " cabin ", 2),
                    SeatInventory.Create(" flight/two ", " cabin ", capacityAvailable ? 2 : 1));
                await seating.SaveChangesAsync();
            }

            if (startInProcess)
            {
                using var seatingClient = Seating.CreateClient();
                using var health = await seatingClient.GetAsync("/health");
                health.EnsureSuccessStatusCode();
                using var bookingClient = Booking.CreateClient();
            }
        }

        public async Task<WorkflowServiceProcess> StartBooking()
        {
            return await StartProcess(typeof(Program).Assembly.Location, "/api/booking/catalog");
        }

        public async Task<WorkflowServiceProcess> StartSeating()
        {
            return await StartProcess(typeof(seating::Program).Assembly.Location, "/health");
        }

        private async Task<WorkflowServiceProcess> StartProcess(string assembly, string readinessPath)
        {
            var process = new WorkflowServiceProcess(assembly, settings);
            processes.Add(process);
            await process.Start(readinessPath);
            return process;
        }

        public async Task<JsonElement> Queue(string name)
        {
            using var response = await management.GetAsync($"api/queues/{vhost}/{name}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        public async Task WaitForSeatingConsumers()
        {
            await WaitUntil(async () =>
            {
                using var response = await management.GetAsync($"api/queues/{vhost}");
                response.EnsureSuccessStatusCode();
                var queues = await response.Content.ReadFromJsonAsync<JsonElement>();
                return new[] { "HoldInventory", "ConfirmInventory" }.All(name =>
                    queues.EnumerateArray().Any(queue =>
                        queue.GetProperty("name").GetString() == name
                        && queue.GetProperty("consumers").GetInt32() > 0));
            }, () => "Seating receive endpoints did not become ready.");
        }

        public async Task AssertRequeuedDelivery(string name)
        {
            using var response = await management.PostAsJsonAsync($"api/queues/{vhost}/{name}/get",
                new { count = 1, ackmode = "ack_requeue_true", encoding = "auto" });
            response.EnsureSuccessStatusCode();
            var messages = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, messages.GetArrayLength());
            Assert.True(messages[0].GetProperty("redelivered").GetBoolean());
        }

        public async Task AssertQueuesSettled()
        {
            await WaitUntil(async () =>
            {
                using var response = await management.GetAsync($"api/queues/{vhost}");
                response.EnsureSuccessStatusCode();
                var queues = await response.Content.ReadFromJsonAsync<JsonElement>();
                var settled = true;
                foreach (var queue in queues.EnumerateArray())
                {
                    var name = queue.GetProperty("name").GetString()!;
                    if (!queue.TryGetProperty("messages", out var messages))
                    {
                        settled = false;
                        continue;
                    }

                    if (name.EndsWith("_error", StringComparison.Ordinal)
                        || name.EndsWith("_skipped", StringComparison.Ordinal))
                    {
                        Assert.Equal(0, messages.GetInt32());
                    }

                    settled &= messages.GetInt32() == 0;
                }

                return settled;
            }, () => "Workflow broker queues did not drain after recovery.");
        }

        public IBusControl CreatePublisher()
        {
            return Bus.Factory.CreateUsingRabbitMq(bus =>
                bus.Host(settings["RabbitMq:Host"]!, ushort.Parse(settings["RabbitMq:Port"]!, CultureInfo.InvariantCulture),
                    vhost, host =>
                    {
                        host.Username(settings["RabbitMq:User"]!);
                        host.Password(settings["RabbitMq:Pass"]!);
                    }));
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                foreach (var process in processes)
                {
                    await process.DisposeAsync();
                }

                try
                {
                    await Booking.DisposeAsync();
                }
                finally
                {
                    await Seating.DisposeAsync();
                }
            }
            finally
            {
                try
                {
                    await Redis.GetDatabase().KeyDeleteAsync(RedisKey);
                    if (vhostCreated)
                    {
                        using var response = await management.DeleteAsync($"api/vhosts/{vhost}");
                        response.EnsureSuccessStatusCode();
                    }

                    await using var admin = new NpgsqlConnection(postgres);
                    await admin.OpenAsync();
                    using var quoter = new NpgsqlCommandBuilder();
                    foreach (var database in databases)
                    {
                        await using var drop = new NpgsqlCommand(
                            "DROP DATABASE " + quoter.QuoteIdentifier(database) + " WITH (FORCE)", admin);
                        await drop.ExecuteNonQueryAsync();
                    }
                }
                finally
                {
                    management.Dispose();
                    Redis.Dispose();
                }
            }
        }
    }
}
