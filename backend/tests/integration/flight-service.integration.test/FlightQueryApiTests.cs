using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using flight_service.Application.Contracts.Requests;
using flight_service.Application.Contracts.Responses;
using flight_service.Api.Middleware;
using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;
using flight_service.Infrastructure.Persistence;
using flight_service.Infrastructure.Persistence.Queries;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace flight_service.integration.test;

public class FlightQueryApiTests
{
    private const string Search = "/api/Flight/flights?DepartureAirport=WAW&ArrivalAirport=KRK&DepartureDate=2026-10-13";

    [Fact]
    public async Task GetFlight_ExistingFlight_ReturnsDtoWithUtcOffsetsAndNoEntityOrResultFields()
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        var response = await client.GetAsync($"/api/Flight/flight/{factory.FirstFlightId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var flight = await response.Content.ReadFromJsonAsync<FlightResponse>();
        Assert.NotNull(flight);
        Assert.Equal(factory.FirstFlightId, flight.Id);
        Assert.Equal("WAW", flight.DepartureAirport);
        Assert.Equal("KRK", flight.ArrivalAirport);
        Assert.Equal(TimeSpan.Zero, flight.DepartureTime.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 13, 0, 0, 0, TimeSpan.Zero), flight.DepartureTime);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(6, json.RootElement.EnumerateObject().Count());
        Assert.False(json.RootElement.TryGetProperty("isSuccess", out _));
        Assert.False(json.RootElement.TryGetProperty("flightSegments", out _));
    }

    [Fact]
    public async Task GetFlight_MissingFlight_ReturnsNotFoundWithIdentifiableProblemDetails()
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        var response = await client.GetAsync("/api/Flight/flight/2147483647");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("flight_not_found", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task GetFlights_MatchingAndNonmatchingFlights_FiltersUtcDayAndAirportsAndPaginatesStably()
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        var all = await client.GetFromJsonAsync<FlightResponse[]>(Search);
        Assert.NotNull(all);
        Assert.Equal(new[] { "PA101", "PA102", "PA103", "PA104" },
            all.Select(flight => flight.FlightNumber));
        var secondPage = await client.GetFromJsonAsync<FlightResponse[]>(Search + "&Page=2&PageSize=2");
        Assert.NotNull(secondPage);
        Assert.Equal(new[] { "PA103", "PA104" }, secondPage.Select(flight => flight.FlightNumber));
        var repeatedPage = await client.GetFromJsonAsync<FlightResponse[]>(Search + "&Page=2&PageSize=2");
        Assert.Equal(secondPage, repeatedPage);
    }

    [Fact]
    public async Task GetFlights_NoMatchingPage_ReturnsSuccessfulEmptyArray()
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        var response = await client.GetAsync(Search + "&Page=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<FlightResponse[]>())!);
    }

    [Fact]
    public async Task GetFlights_DefaultAndMaximumPageSizes_ReturnsExpectedPageLengths()
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>();
            var departure = await context.Airports.SingleAsync(airport => airport.IataCode == "WAW");
            var arrival = await context.Airports.SingleAsync(airport => airport.IataCode == "KRK");
            var day = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
            context.Flights.AddRange(Enumerable.Range(1, 101).Select(index =>
                Flight.Create($"PA{index + 200}", day.AddMinutes(index),
                    day.AddHours(2).AddMinutes(index), departure, arrival)));
            await context.SaveChangesAsync();
        }

        const string search = "/api/Flight/flights?DepartureAirport=WAW&ArrivalAirport=KRK&DepartureDate=2026-10-15";
        var defaultPage = await client.GetFromJsonAsync<FlightResponse[]>(search);
        Assert.NotNull(defaultPage);
        Assert.Equal(20, defaultPage.Length);
        Assert.Equal("PA201", defaultPage[0].FlightNumber);
        Assert.Equal("PA220", defaultPage[^1].FlightNumber);
        var maximumPage = await client.GetFromJsonAsync<FlightResponse[]>(search + "&PageSize=100");
        Assert.NotNull(maximumPage);
        Assert.Equal(100, maximumPage.Length);
        var finalPage = await client.GetFromJsonAsync<FlightResponse[]>(search + "&PageSize=100&Page=2");
        Assert.NotNull(finalPage);
        Assert.Equal("PA301", Assert.Single(finalPage).FlightNumber);
    }

    [Theory]
    [InlineData("/api/Flight/flights", "DepartureAirport")]
    [InlineData(Search + "&Page=0", "Page")]
    [InlineData(Search + "&PageSize=101", "PageSize")]
    [InlineData(Search + "&Page=2147483647", "Page")]
    [InlineData("/api/Flight/flights?DepartureAirport=waw&ArrivalAirport=KRK&DepartureDate=2026-10-13", "DepartureAirport")]
    [InlineData("/api/Flight/flights?DepartureAirport=WAW&ArrivalAirport=KRK&DepartureDate=invalid", "DepartureDate")]
    [InlineData("/api/Flight/flight/0", "flightId")]
    [InlineData("/api/Flight/flight/not-an-id", "flightId")]
    public async Task GetFlightsAndGetFlight_InvalidInputs_ReturnFieldSpecificValidationProblemsBeforeQuerying(string path, string field)
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>()
                .Database.ExecuteSqlRawAsync("DROP TABLE Flights");
        }

        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    public async Task GetFlights_DatabaseFailureInDevelopment_LogsExceptionAndReturnsSafeProblemDetails(string accept)
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>()
                .Database.ExecuteSqlRawAsync("DROP TABLE Flights");
        }

        var response = await client.GetAsync(Search);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("SQLite", body);
        Assert.DoesNotContain("no such table", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.Contains(factory.Logs.Entries, entry =>
            entry.Category == typeof(ApiExceptionHandler).FullName
            && entry.Level == LogLevel.Error && entry.Exception is SqliteException);
    }

    [Fact]
    public async Task GetFlightAndGetFlights_ValidAndCancelledRequests_AvoidTrackingAndPropagateCancellation()
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>();
        var queries = new FlightQueries(context);
        var result = await queries.GetFlight(factory.FirstFlightId, CancellationToken.None);
        Assert.True(result.IsSuccess);
        var search = new SearchFlightsRequest
        {
            DepartureAirport = "WAW",
            ArrivalAirport = "KRK",
            DepartureDate = new DateOnly(2026, 10, 13)
        };
        Assert.True((await queries.GetFlights(search, CancellationToken.None)).IsSuccess);
        Assert.Empty(context.ChangeTracker.Entries());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            queries.GetFlight(factory.FirstFlightId, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            queries.GetFlights(search, cancellation.Token));
    }

    [Fact]
    public async Task GetFlight_NoProductionDatabaseProvider_ReturnsSafeServerError()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var response = await client.GetAsync("/api/Flight/flight/1");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("provider", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    [Fact]
    public async Task GetOpenApi_DevelopmentEnvironment_DocumentsFlightResponsesAndErrors()
    {
        using var factory = new FlightApiFactory();
        using var client = factory.CreateSeededClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths");
        var singleResponses = paths
            .GetProperty("/api/Flight/flight/{flightId}")
            .GetProperty("get")
            .GetProperty("responses");
        Assert.True(singleResponses.TryGetProperty("200", out _));
        Assert.True(singleResponses.TryGetProperty("400", out _));
        Assert.True(singleResponses.TryGetProperty("404", out _));
        Assert.True(singleResponses.TryGetProperty("500", out _));
    }

    private sealed class FlightApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");

        public ExceptionLogProvider Logs { get; } = new();
        public int FirstFlightId { get; private set; }

        public FlightApiFactory()
        {
            connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<FlightServiceDbContext>();
                services.RemoveAll<DbContextOptions<FlightServiceDbContext>>();
                services.AddDbContext<FlightServiceDbContext>(options => options.UseSqlite(connection));
                services.AddSingleton<ILoggerProvider>(Logs);
            });
        }

        public HttpClient CreateSeededClient()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>();
            context.Database.EnsureCreated();

            var departure = Airport.Create("Warsaw", "EPWA", "WAW", Address.Create("Warsaw", "Mazovia", "PL"));
            var arrival = Airport.Create("Krakow", "EPKK", "KRK", Address.Create("Krakow", "Malopolska", "PL"));
            var other = Airport.Create("Gdansk", "EPGD", "GDN", Address.Create("Gdansk", "Pomerania", "PL"));
            var day = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);
            var first = Flight.Create("PA101", day, day.AddHours(1), departure, arrival);
            context.Flights.AddRange(
                first,
                Flight.Create("PA102", day.AddHours(10), day.AddHours(11), departure, arrival),
                Flight.Create("PA103", day.AddHours(10), day.AddHours(12), departure, arrival),
                Flight.Create("PA104", day.AddDays(1).AddTicks(-1), day.AddDays(1).AddHours(1), departure, arrival),
                Flight.Create("PA105", day.AddDays(1), day.AddDays(1).AddHours(1), departure, arrival),
                Flight.Create("PA106", day.AddHours(10), day.AddHours(11), departure, other),
                Flight.Create("PA107", day.AddTicks(-1), day.AddHours(1), departure, arrival));

            context.SaveChanges();
            FirstFlightId = first.Id;

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

    private sealed class ExceptionLogProvider : ILoggerProvider
    {
        public ConcurrentQueue<(string Category, LogLevel Level, Exception Exception)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new ExceptionLogger(categoryName, Entries);
        }

        public void Dispose()
        {
        }

        private sealed class ExceptionLogger(
            string categoryName,
            ConcurrentQueue<(string Category, LogLevel Level, Exception Exception)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (exception is not null)
                {
                    entries.Enqueue((categoryName, logLevel, exception));
                }
            }
        }
    }
}
