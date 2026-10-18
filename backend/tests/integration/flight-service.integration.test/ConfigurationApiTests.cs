using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using flight_service.Application.Contracts.Responses;
using flight_service.Application.Features.ConfigurationFeatures.Queries.Validators;
using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;
using flight_service.Infrastructure.Persistence;
using flight_service.Infrastructure.Persistence.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace flight_service.integration.test;

public sealed class ConfigurationApiTests
{
    [Fact]
    public async Task Create_ValidConfiguration_ReturnsLocationAndPersistsEmptySeatMap()
    {
        using var factory = new ConfigurationFactory();
        using var client = factory.CreateSeededClient();
        var created = await client.PostAsJsonAsync("/api/configurations",
            new { aircraftTypeId = factory.TypeId, code = "NEW", name = "New layout", version = 2 });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var configuration = await client.GetFromJsonAsync<ConfigurationResponse>(created.Headers.Location);
        Assert.NotNull(configuration);
        Assert.True(configuration.Id > 0);
        Assert.Equal(factory.TypeId, configuration.AircraftTypeId);
        Assert.Equal("NEW", configuration.Code);
        Assert.True(configuration.IsActive);
        Assert.True(configuration.SeatMap.Id > 0);
        Assert.Empty(configuration.SeatMap.Rows);

        var duplicate = await client.PostAsJsonAsync("/api/configurations",
            new { aircraftTypeId = factory.TypeId, code = "NEW", name = "New layout", version = 2 });
        await AssertProblem(duplicate, HttpStatusCode.Conflict, "configuration_exists");
        var missingType = await client.PostAsJsonAsync("/api/configurations",
            new { aircraftTypeId = int.MaxValue, code = "NEW", name = "New layout", version = 2 });
        await AssertProblem(missingType, HttpStatusCode.NotFound, "aircraft_type_not_found");
        var invalid = await client.PostAsJsonAsync("/api/configurations",
            new { aircraftTypeId = factory.TypeId, code = "", name = "New layout", version = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(3, await scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>()
            .AircraftConfigurations.CountAsync());
    }

    [Fact]
    public async Task SetRow_AddEditAndRemove_PreservesMatchingPositionsAndOrdersUiLayout()
    {
        using var factory = new ConfigurationFactory();
        using var client = factory.CreateSeededClient();
        var route = $"/api/configurations/{factory.ConfigurationId}/seat-map/rows";
        var later = await client.PutAsJsonAsync($"{route}/12", Row(factory.CabinId, "Galley", "C", "A"));
        Assert.Equal(HttpStatusCode.NoContent, later.StatusCode);
        var earlier = await client.PutAsJsonAsync($"{route}/2", new
        {
            cabinId = factory.CabinId,
            type = "Seats, ExitRow",
            positions = new[]
            {
                new { column = "B", aisleGroup = 1, isSeat = true },
                new { column = "A", aisleGroup = 0, isSeat = false }
            }
        });
        Assert.Equal(HttpStatusCode.NoContent, earlier.StatusCode);
        var before = await client.GetFromJsonAsync<ConfigurationResponse>($"/api/configurations/{factory.ConfigurationId}");
        Assert.NotNull(before);
        Assert.Equal(new[] { 2, 12 }, before.SeatMap.Rows.Select(row => row.Number));
        var row = before.SeatMap.Rows[0];
        Assert.Equal("Seats, ExitRow", row.Type);
        Assert.Equal("Economy", row.CabinName);
        Assert.Equal("Y", row.CabinCode);
        Assert.Equal(new[] { "A", "B" }, row.Positions.Select(position => position.Column));
        Assert.False(row.Positions[0].IsSeat);
        var existingId = row.Positions[0].Id;

        var edited = await client.PutAsJsonAsync($"{route}/2", Row(factory.CabinId, "Seats", "A", "C"));
        Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);
        var after = await client.GetFromJsonAsync<ConfigurationResponse>($"/api/configurations/{factory.ConfigurationId}");
        Assert.NotNull(after);
        Assert.Equal(row.Id, after.SeatMap.Rows[0].Id);
        Assert.Equal(existingId, after.SeatMap.Rows[0].Positions[0].Id);
        Assert.True(after.SeatMap.Rows[0].Positions[0].IsSeat);
        Assert.Equal(new[] { "A", "C" }, after.SeatMap.Rows[0].Positions.Select(position => position.Column));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{route}/2")).StatusCode);
        await AssertProblem(await client.DeleteAsync($"{route}/2"), HttpStatusCode.NotFound, "seat_map_row_not_found");
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>();
        Assert.Equal(1, await context.SeatMapRows.CountAsync());
        Assert.Equal(2, await context.SeatMapPositions.CountAsync());
    }

    [Fact]
    public async Task AssignAircraft_ThenReassign_ReturnsCurrentAircraftLayoutAndExplicitFailures()
    {
        using var factory = new ConfigurationFactory();
        using var client = factory.CreateSeededClient();
        var flightRoute = $"/api/Flight/flight/{factory.FlightId}";
        await AssertProblem(await client.GetAsync($"{flightRoute}/seat-map"),
            HttpStatusCode.Conflict, "flight_aircraft_unassigned");
        await AssertProblem(await client.GetAsync("/api/Flight/flight/2147483647/seat-map"),
            HttpStatusCode.NotFound, "flight_not_found");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/Flight/flight/0/seat-map")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync($"{flightRoute}/aircraft", new { aircraftId = factory.AircraftId })).StatusCode);
        var rowResult = await client.PutAsJsonAsync(
            $"/api/configurations/{factory.ConfigurationId}/seat-map/rows/4", Row(factory.CabinId, "Seats", "A"));
        Assert.Equal(HttpStatusCode.NoContent, rowResult.StatusCode);
        var first = await client.GetFromJsonAsync<FlightSeatMapResponse>($"{flightRoute}/seat-map");
        Assert.NotNull(first);
        Assert.Equal(factory.FlightId, first.FlightId);
        Assert.Equal(factory.AircraftId, first.AircraftId);
        Assert.Equal(factory.ConfigurationId, first.ConfigurationId);
        Assert.Equal("SP-ONE", first.Registration);
        Assert.Equal(4, Assert.Single(first.SeatMap.Rows).Number);
        using var json = JsonDocument.Parse(await client.GetStringAsync($"{flightRoute}/seat-map"));
        Assert.False(json.RootElement.TryGetProperty("isSuccess", out _));
        Assert.False(json.RootElement.TryGetProperty("aircraft", out _));
        Assert.True(json.RootElement.TryGetProperty("seatMap", out _));

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync($"{flightRoute}/aircraft", new { aircraftId = factory.OtherAircraftId })).StatusCode);
        var second = await client.GetFromJsonAsync<FlightSeatMapResponse>($"{flightRoute}/seat-map");
        Assert.NotNull(second);
        Assert.Equal(factory.OtherAircraftId, second.AircraftId);
        Assert.Equal("ALT", second.ConfigurationCode);
        Assert.Empty(second.SeatMap.Rows);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync($"/api/configurations/{factory.OtherConfigurationId}/active",
                new { isActive = false })).StatusCode);
        await AssertProblem(await client.PutAsJsonAsync($"{flightRoute}/aircraft", new { aircraftId = factory.OtherAircraftId }),
            HttpStatusCode.Conflict, "aircraft_unavailable");
        await AssertProblem(await client.PutAsJsonAsync($"{flightRoute}/aircraft", new { aircraftId = int.MaxValue }),
            HttpStatusCode.NotFound, "aircraft_not_found");
        var stillAssigned = await client.GetFromJsonAsync<FlightSeatMapResponse>($"{flightRoute}/seat-map");
        Assert.NotNull(stillAssigned);
        Assert.Equal(factory.OtherAircraftId, stillAssigned.AircraftId);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync($"/api/configurations/{factory.OtherConfigurationId}/active",
                new { isActive = true })).StatusCode);
        var active = await client.GetFromJsonAsync<ConfigurationResponse>($"/api/configurations/{factory.OtherConfigurationId}");
        Assert.NotNull(active);
        Assert.True(active.IsActive);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("negative-aisle")]
    [InlineData("invalid-type")]
    [InlineData("null-positions")]
    [InlineData("null-position")]
    [InlineData("empty-column")]
    public async Task SetRow_InvalidInput_ReturnsValidationProblemWithoutPartialWrites(string invalid)
    {
        using var factory = new ConfigurationFactory();
        using var client = factory.CreateSeededClient();
        var positions = invalid switch
        {
            "null-positions" => null,
            "null-position" => new object?[] { null },
            "duplicate" => new object?[]
            {
                new { column = "A", aisleGroup = 0, isSeat = true },
                new { column = "A", aisleGroup = 1, isSeat = true }
            },
            _ => new object?[]
            {
                new { column = invalid == "empty-column" ? " " : "A",
                    aisleGroup = invalid == "negative-aisle" ? -1 : 0, isSeat = true }
            }
        };
        var response = await client.PutAsJsonAsync(
            $"/api/configurations/{factory.ConfigurationId}/seat-map/rows/1",
            new { cabinId = factory.CabinId, type = invalid == "invalid-type" ? 64 : 1, positions });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>().SeatMapRows.CountAsync());
    }

    [Fact]
    public async Task AssignAircraft_MaintenanceOrWrongType_ReturnsConflictWithoutChangingAssignment()
    {
        using var factory = new ConfigurationFactory();
        using var client = factory.CreateSeededClient();
        var route = $"/api/Flight/flight/{factory.FlightId}/aircraft";
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>();
            var aircraft = await context.Aircrafts.SingleAsync(value => value.Id == factory.AircraftId);
            aircraft.SendToMaintenance();
            var configuration = await context.AircraftConfigurations.SingleAsync(value => value.Id == factory.ConfigurationId);
            context.Aircrafts.Add(Aircraft.Create("SP-ERR", "000003",
                AircraftType.Create("A320", "A320", "Airbus"), configuration));
            await context.SaveChangesAsync();
        }

        await AssertProblem(await client.PutAsJsonAsync(route, new { aircraftId = factory.AircraftId }),
            HttpStatusCode.Conflict, "aircraft_unavailable");
        int mismatchedId;
        using (var scope = factory.Services.CreateScope())
        {
            mismatchedId = await scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>().Aircrafts
                .Where(value => value.Registration == "SP-ERR").Select(value => value.Id).SingleAsync();
        }

        await AssertProblem(await client.PutAsJsonAsync(route, new { aircraftId = mismatchedId }),
            HttpStatusCode.Conflict, "aircraft_configuration_mismatch");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(route, new { aircraftId = 0 })).StatusCode);
        await AssertProblem(await client.GetAsync($"/api/Flight/flight/{factory.FlightId}/seat-map"),
            HttpStatusCode.Conflict, "flight_aircraft_unassigned");
    }

    [Fact]
    public async Task ConfigurationCommands_MissingResources_ReturnNotFoundWithoutPartialRows()
    {
        using var factory = new ConfigurationFactory();
        using var client = factory.CreateSeededClient();
        await AssertProblem(await client.PutAsJsonAsync("/api/configurations/2147483647/active", new { isActive = false }),
            HttpStatusCode.NotFound, "configuration_not_found");
        await AssertProblem(await client.GetAsync("/api/configurations/2147483647"),
            HttpStatusCode.NotFound, "configuration_not_found");
        await AssertProblem(await client.PutAsJsonAsync(
            "/api/configurations/2147483647/seat-map/rows/1", Row(factory.CabinId, "Seats", "A")),
            HttpStatusCode.NotFound, "configuration_not_found");
        await AssertProblem(await client.PutAsJsonAsync(
            $"/api/configurations/{factory.ConfigurationId}/seat-map/rows/1", Row(int.MaxValue, "Seats", "A")),
            HttpStatusCode.NotFound, "cabin_not_found");
        var empty = await client.PutAsJsonAsync(
            $"/api/configurations/{factory.ConfigurationId}/seat-map/rows/2", Row(factory.CabinId, "Galley"));
        Assert.Equal(HttpStatusCode.NoContent, empty.StatusCode);
        var layout = await client.GetFromJsonAsync<ConfigurationResponse>($"/api/configurations/{factory.ConfigurationId}");
        Assert.NotNull(layout);
        Assert.Empty(Assert.Single(layout.SeatMap.Rows).Positions);
    }

    [Fact]
    public async Task GetFlightSeatMap_DirectQuery_DoesNotTrackAndPropagatesInvalidInputAndCancellation()
    {
        using var factory = new ConfigurationFactory();
        using var client = factory.CreateSeededClient();
        await client.PutAsJsonAsync($"/api/Flight/flight/{factory.FlightId}/aircraft", new { aircraftId = factory.AircraftId });
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>();
        var queries = new ConfigurationQueries(context, new LayoutIdValidator());

        Assert.True((await queries.GetFlightSeatMap(factory.FlightId, CancellationToken.None)).IsSuccess);
        Assert.Empty(context.ChangeTracker.Entries());
        await Assert.ThrowsAsync<ValidationException>(() => queries.GetFlightSeatMap(0, CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queries.GetFlightSeatMap(factory.FlightId, cancellation.Token));
    }

    [Fact]
    public async Task GetOpenApi_ConfigurationFeature_DocumentsQueryAndCommandStatuses()
    {
        using var factory = new ConfigurationFactory();
        using var client = factory.CreateSeededClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");
        var query = paths.GetProperty("/api/Flight/flight/{flightId}/seat-map").GetProperty("get").GetProperty("responses");
        foreach (var status in new[] { "200", "400", "404", "409", "500" })
        {
            Assert.True(query.TryGetProperty(status, out _));
        }

        Assert.True(paths.GetProperty("/api/configurations").GetProperty("post").GetProperty("responses")
            .TryGetProperty("201", out _));
        Assert.True(paths.TryGetProperty("/api/configurations/{configurationId}/seat-map/rows/{rowNumber}", out _));
    }

    private static object Row(int cabinId, string type, params string[] columns)
    {
        return new { cabinId, type, positions = columns.Select(column => new { column, aisleGroup = 0, isSeat = true }) };
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
    }

    private sealed class ConfigurationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public int TypeId { get; private set; }
        public int CabinId { get; private set; }
        public int ConfigurationId { get; private set; }
        public int OtherConfigurationId { get; private set; }
        public int AircraftId { get; private set; }
        public int OtherAircraftId { get; private set; }
        public int FlightId { get; private set; }

        public ConfigurationFactory()
        {
            connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RabbitMq:User"] = "unused",
                    ["RabbitMq:Pass"] = "unused"
                }));
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType?.Namespace?.StartsWith("MassTransit", StringComparison.Ordinal) == true).ToArray())
                {
                    services.Remove(descriptor);
                }

                services.RemoveAll<FlightServiceDbContext>();
                services.RemoveAll<DbContextOptions<FlightServiceDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<FlightServiceDbContext>>();
                services.AddDbContext<FlightServiceDbContext>(options => options.UseSqlite(connection));
            });
        }

        public HttpClient CreateSeededClient()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>();
            context.Database.EnsureCreated();
            var type = AircraftType.Create("B788", "787-8", "Boeing");
            var configuration = AircraftConfiguration.Create(type, "STD", "Standard", 1);
            var other = AircraftConfiguration.Create(type, "ALT", "Alternate", 1);
            var firstAircraft = Aircraft.Create("SP-ONE", "000001", type, configuration);
            var secondAircraft = Aircraft.Create("SP-TWO", "000002", type, other);
            var cabin = SeatMapCabin.Create("Economy", "Y");
            var airport = Airport.Create("Warsaw", "EPWA", "WAW", Address.Create("Warsaw", "Mazovia", "PL"));
            var flight = Flight.Create("PA123", new DateTime(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc), airport, airport);
            context.Aircrafts.AddRange(firstAircraft, secondAircraft);
            context.SeatMapCabins.Add(cabin);
            context.Flights.Add(flight);
            context.SaveChanges();
            TypeId = type.Id;
            CabinId = cabin.Id;
            ConfigurationId = configuration.Id;
            OtherConfigurationId = other.Id;
            AircraftId = firstAircraft.Id;
            OtherAircraftId = secondAircraft.Id;
            FlightId = flight.Id;
            return CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                connection.Dispose();
            }
        }
    }
}
