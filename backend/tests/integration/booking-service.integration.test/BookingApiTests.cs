using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using booking_service.Application.Contracts;
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

        var replay = await client.PostAsJsonAsync("/api/booking/reservations", request);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(reservation.Id, (await replay.Content.ReadFromJsonAsync<ReservationResponse>())!.Id);
        var changed = await client.PostAsJsonAsync("/api/booking/reservations", request with { ContactName = "Other" });
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Guest-Token");
        client.DefaultRequestHeaders.Add("X-Guest-Token", new string('A', 64));
        var unauthorized = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, unauthorized.StatusCode);
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
