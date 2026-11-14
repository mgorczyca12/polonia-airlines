using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace booking_service.integration.test;

public sealed class BookingApiTests
{
    [Fact]
    public async Task Checkout_Offer_Reservation_ReturnsPending201AndRequiresGuestCapability()
    {
        using var factory = new Factory();
        using var client = factory.CreateSeededClient();

        var checkout = await (await client.PostAsync("/api/booking/checkouts", null))
            .Content.ReadFromJsonAsync<CheckoutResponse>();
        Assert.NotNull(checkout);
        client.DefaultRequestHeaders.Add("X-Guest-Token", checkout.GuestToken);

        var offered = await client.PostAsJsonAsync("/api/booking/offers",
            new OfferRequest("PLN", 1, [new(" flight ", factory.ProductId)]));
        Assert.Equal(HttpStatusCode.Created, offered.StatusCode);

        var offer = await offered.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(offer);

        var request = new ReservationRequest(offer.Id, "browser-request", "Guest", "guest@example.test",
            [new("First", "Last", new DateOnly(1990, 1, 1))]);

        var response = await client.PostAsJsonAsync("/api/booking/reservations", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var reservation = await response.Content.ReadFromJsonAsync<ReservationResponse>();
        Assert.NotNull(reservation);
        Assert.Equal("Created", reservation.Status);
        Assert.Equal("Pending", reservation.WorkflowStatus);
        Assert.Equal(125m, reservation.TotalAmount);
        Assert.Equal(" cabin ", Assert.Single(reservation.Fares).CabinId);
        var reloaded = await client.GetFromJsonAsync<ReservationResponse>(response.Headers.Location);
        Assert.NotNull(reloaded);
        Assert.Equal("Guest", reloaded.ContactName);
        Assert.Equal("guest@example.test", reloaded.ContactEmail);
        Assert.Equal("First", Assert.Single(reloaded.Passengers).FirstName);
        Assert.Equal("Last", Assert.Single(reloaded.Passengers).LastName);
        var recovered = await client.GetFromJsonAsync<ReservationResponse>(
            "/api/booking/reservations/by-request/browser-request");
        Assert.NotNull(recovered);
        Assert.Equal(reservation.Id, recovered.Id);

        var replay = await client.PostAsJsonAsync("/api/booking/reservations", request);

        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(reservation.Id, (await replay.Content.ReadFromJsonAsync<ReservationResponse>())!.Id);

        var changed = await client.PostAsJsonAsync("/api/booking/reservations", request with { ContactName = "Other" });
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);

        client.DefaultRequestHeaders.Remove("X-Guest-Token");
        client.DefaultRequestHeaders.Add("X-Guest-Token", new string('A', 64));

        var unauthorized = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/booking/reservations/by-request/browser-request")).StatusCode);

        using var json = JsonDocument.Parse(await unauthorized.Content.ReadAsStringAsync());
        Assert.Equal("not_found", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvalidQueriesAndCommands_Return400AndNoAnonymousAdministrationExists()
    {
        using var factory = new Factory();
        using var client = factory.CreateSeededClient();

        client.DefaultRequestHeaders.Add("X-Guest-Token", new string('A', 64));

        var invalidId = await client.GetAsync("/api/booking/reservations/0");

        Assert.Equal(HttpStatusCode.BadRequest, invalidId.StatusCode);

        var invalidOffer = await client.PostAsJsonAsync("/api/booking/offers",
            new OfferRequest("pln", 10, [new(" flight ", factory.ProductId)]));

        Assert.Equal(HttpStatusCode.BadRequest, invalidOffer.StatusCode);

        var mutation = await client.PostAsJsonAsync("/api/booking/catalog", new { code = "UNTRUSTED" });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, mutation.StatusCode);
    }

    [Fact]
    public async Task Catalog_ReturnsClassWithoutPretendingItIsAPhysicalCabin()
    {
        using var factory = new Factory();
        using var client = factory.CreateSeededClient();

        var catalog = await client.GetFromJsonAsync<ProductResponse[]>("/api/booking/catalog");
        var product = Assert.Single(catalog!);

        Assert.Equal("Economy", product.CabinClass);

        using var json = JsonDocument.Parse(await client.GetStringAsync("/api/booking/catalog"));

        Assert.False(json.RootElement[0].TryGetProperty("cabinId", out _));
        Assert.False(json.RootElement[0].TryGetProperty("isSuccess", out _));
    }

    [Fact]
    public async Task FareSearch_ExactReferencesAndActivePrices_ReturnOnlyMatchingProducts()
    {
        using var factory = new Factory();
        using var client = factory.CreateSeededClient();
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var retired = FareProduct.Create("RETIRED", 1, "Economy", "Retired", BaggageAllowance.None);
            retired.Retire();
            database.FareProducts.Add(retired);
            database.FlightPrices.Add(FlightPrice.Create(" flight ", retired.Id, "PLN", 1m, " cabin "));
            database.FlightPrices.Add(FlightPrice.Create("other", factory.ProductId, "PLN", 200m, "other-cabin"));
            database.SaveChanges();
        }

        var fares = await client.GetFromJsonAsync<FlightFareResponse[]>(
            "/api/booking/fares?FlightIds=%20flight%20&FlightIds=other&Currency=PLN");
        Assert.NotNull(fares);
        Assert.Equal(2, fares.Length);
        Assert.Equal(" flight ", fares[0].FlightId);
        Assert.Equal(125m, fares[0].Amount);
        Assert.Equal(factory.ProductId, fares[0].ProductId);
        Assert.Equal(" cabin ", fares[0].CabinId);
        Assert.Equal("other", fares[1].FlightId);

        Assert.Empty((await client.GetFromJsonAsync<FlightFareResponse[]>(
            "/api/booking/fares?FlightIds=flight&Currency=PLN"))!);
        Assert.Empty((await client.GetFromJsonAsync<FlightFareResponse[]>(
            "/api/booking/fares?FlightIds=%20flight%20&Currency=USD"))!);
    }

    [Theory]
    [InlineData("Currency=USD")]
    [InlineData("FlightIds=1&Currency=usd")]
    [InlineData("FlightIds=1&FlightIds=1&Currency=USD")]
    [InlineData("FlightIds=%20%20&Currency=USD")]
    public async Task FareSearch_InvalidInput_ReturnsValidationProblem(string query)
    {
        using var factory = new Factory();
        using var client = factory.CreateSeededClient();

        var response = await client.GetAsync($"/api/booking/fares?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task FareSearch_AboveBatchLimit_ReturnsBadRequest()
    {
        using var factory = new Factory();
        using var client = factory.CreateSeededClient();
        var maximum = string.Join("&", Enumerable.Range(1, 100).Select(id => $"FlightIds={id}"));
        var query = string.Join("&", Enumerable.Range(1, 101).Select(id => $"FlightIds={id}"));

        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"/api/booking/fares?{maximum}&Currency=USD")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync($"/api/booking/fares?{query}&Currency=USD")).StatusCode);
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");

        public Guid ProductId { get; private set; }

        public Factory()
        {
            connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RabbitMq:Host"] = "localhost",
                    ["RabbitMq:User"] = "unused",
                    ["RabbitMq:Pass"] = "unused"
                }));
            builder.ConfigureServices(services =>
            {
                var messaging = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                    (descriptor.ImplementationType?.Namespace?.StartsWith("MassTransit", StringComparison.Ordinal) == true ||
                     descriptor.ImplementationType?.Name == "ReservationDeadlineService")).ToArray();
                foreach (var descriptor in messaging)
                {
                    services.Remove(descriptor);
                }

                services.RemoveAll<BookingDbContext>();
                services.RemoveAll<DbContextOptions<BookingDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<BookingDbContext>>();
                services.AddDbContext<BookingDbContext>(options => options.UseSqlite(connection));
            });
        }

        public HttpClient CreateSeededClient()
        {
            using var scope = Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            database.Database.EnsureCreated();
            var product = FareProduct.Create("LIGHT", 1, "Economy", "Light", BaggageAllowance.None);
            ProductId = product.Id;
            database.FareProducts.Add(product);
            database.FlightPrices.Add(FlightPrice.Create(" flight ", product.Id, "PLN", 125m, " cabin "));
            database.SaveChanges();
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
