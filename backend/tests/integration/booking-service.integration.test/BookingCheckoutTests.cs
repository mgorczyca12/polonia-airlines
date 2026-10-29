using System.Text.Json;
using booking_service.Application;
using booking_service.Application.Errors;
using booking_service.Application.Features.CheckoutFeatures.Commands.CreateCheckout;
using booking_service.Application.Features.CheckoutFeatures.Commands.CreateOffer;
using booking_service.Application.Features.CheckoutFeatures.Queries;
using booking_service.Application.Features.ReservationFeatures.Queries;
using booking_service.Infrastructure.Repositories;
using booking_service.Application.Abstractions;
using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure;
using booking_service.Infrastructure.Persistence;
using booking_service.Infrastructure.Persistence.Queries;
using DispatchR;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.DependencyInjection;
using Shared.Integration.Contracts;

namespace booking_service.integration.test;

public sealed class BookingCheckoutTests
{
    [Fact]
    public async Task ServerPricing_PersistsExactSnapshotsAndOutboxWithNativePassengerIds()
    {
        await using var fixture = await Fixture.Create();
        var token = await fixture.Checkout();
        var offer = await fixture.Offer(token, passengerCount: 2);
        Assert.Equal(500m, offer.TotalAmount);
        Assert.Equal(fixture.Clock.GetUtcNow().AddMinutes(15), offer.ExpiresAt);
        Assert.All(offer.Fares, fare => Assert.Equal(" opaque cabin ", fare.CabinId));

        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            (await database.FareProducts.SingleAsync()).Retire();
            var replacement = FareProduct.Create("LIGHT", 2, "Economy", "New brand", BaggageAllowance.None);
            database.FareProducts.Add(replacement);
            database.FlightPrices.Add(FlightPrice.Create("flight/opaque", replacement.Id, "PLN", 900m, "NEW-CABIN"));
            await database.SaveChangesAsync();
        }

        var response = await fixture.Reserve(token, Request(offer.Id, 2));
        Assert.True(response.IsSuccess);
        Assert.Equal("Created", response.Value.Status);
        Assert.Equal("Pending", response.Value.WorkflowStatus);
        Assert.Equal(500m, response.Value.TotalAmount);
        Assert.Equal(6, response.Value.Code.Length);
        Assert.Equal(token, response.Value.GuestAccessToken);
        Assert.All(response.Value.Fares, fare =>
        {
            Assert.Equal(1, fare.ProductVersion);
            Assert.Equal("Original brand", fare.Brand);
            Assert.Equal(1, fare.Baggage.IncludedPieces);
            Assert.Equal(250m, fare.Amount);
        });

        await using var read = fixture.Provider.CreateAsyncScope();
        var context = read.ServiceProvider.GetRequiredService<BookingDbContext>();
        var stored = await context.Reservations.Include(x => x.Passengers).ThenInclude(x => x.Fares).SingleAsync();
        Assert.Equal(500m, stored.TotalAmount);
        Assert.All(stored.Passengers.SelectMany(x => x.Fares), fare =>
        {
            Assert.Equal(1, fare.ProductVersion);
            Assert.Equal(" opaque cabin ", fare.CabinId);
        });
        var message = await context.Set<OutboxMessage>().SingleAsync();
        using var json = JsonDocument.Parse(message.Body);
        var payload = json.RootElement.GetProperty("message");
        Assert.Equal(stored.Id.ToString(), payload.GetProperty("reservationId").GetString());
        var selections = payload.GetProperty("selections").EnumerateArray().ToArray();
        Assert.Equal(2, selections.Length);
        Assert.Equal(stored.Passengers.Select(x => x.Id.ToString()).Order(),
            selections.Select(x => x.GetProperty("passengerId").GetString()).Order());
        Assert.All(selections, x => Assert.Equal(" opaque cabin ", x.GetProperty("cabinId").GetString()));
        Assert.Equal(fixture.Clock.GetUtcNow().AddMinutes(10), payload.GetProperty("holdUntil").GetDateTimeOffset());
        Assert.True((await context.CheckoutOffers.SingleAsync()).Consumed);
    }

    [Fact]
    public async Task DurableIdempotency_ReplaysAfterExpiryButRejectsChangedPayloadAndSecondConsumption()
    {
        await using var fixture = await Fixture.Create();
        var token = await fixture.Checkout();
        var offer = await fixture.Offer(token);
        var request = Request(offer.Id);
        var first = await fixture.Reserve(token, request);
        Assert.True(first.IsSuccess);

        fixture.Clock.Advance(TimeSpan.FromMinutes(16));
        var replay = await fixture.Reserve(token, request);
        Assert.True(replay.IsSuccess);
        Assert.Equal(first.Value.Id, replay.Value.Id);
        var conflict = await fixture.Reserve(token, request with { ContactName = "Different" });
        AssertCode(conflict, "idempotency_conflict");

        fixture.Clock.Advance(TimeSpan.FromMinutes(-16));
        var consumed = await fixture.Reserve(token, request with { RequestId = "second-request" });
        AssertCode(consumed, "offer_consumed");
        await using var scope = fixture.Provider.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Equal(1, await database.Reservations.CountAsync());
        Assert.Equal(1, await database.ReservationCheckouts.CountAsync());
        Assert.Equal(1, await database.Set<OutboxMessage>().CountAsync());
    }

    [Fact]
    public async Task GuestBoundary_BlocksOtherGuestsUnknownTokensAndExpiredOffers()
    {
        await using var fixture = await Fixture.Create();
        var owner = await fixture.Checkout();
        var stranger = await fixture.Checkout();
        Assert.NotEqual(owner, stranger);
        Assert.Equal(64, owner.Length);
        var offer = await fixture.Offer(owner);
        AssertCode(await fixture.Reserve(stranger, Request(offer.Id)), "not_found");

        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var queries = scope.ServiceProvider.GetRequiredService<IOfferQueries>();
            AssertCode(await queries.GetOffer(new GetOfferRequest(offer.Id, stranger), default), "not_found");
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            AssertCode(await mediator.Send(new CreateOfferCommand(new string('A', 64),
                new OfferRequest("PLN", 1, [new("flight/opaque", fixture.ProductId)])), default), "not_found");
        }

        fixture.Clock.Advance(TimeSpan.FromMinutes(15));
        AssertCode(await fixture.Reserve(owner, Request(offer.Id)), "offer_expired");
        fixture.Clock.Advance(TimeSpan.FromMinutes(-15));
        var reservation = await fixture.Reserve(owner, Request(offer.Id));
        await using var lookup = fixture.Provider.CreateAsyncScope();
        var query = lookup.ServiceProvider.GetRequiredService<IReservationQueries>();
        AssertCode(await query.GetReservation(new GetReservationRequest(reservation.Value.Id, stranger), default), "not_found");
        Assert.True((await query.GetReservation(new GetReservationRequest(reservation.Value.Id, owner), default)).IsSuccess);
    }

    [Fact]
    public async Task InvalidInputAndUnavailablePrices_DoNotCreateOffersOrReservations()
    {
        await using var fixture = await Fixture.Create();
        var token = await fixture.Checkout();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(async () =>
            await mediator.Send(new CreateOfferCommand(token,
                new OfferRequest("pln", 0, [])), default));
        var missingPrice = await mediator.Send(new CreateOfferCommand(token,
            new OfferRequest("EUR", 1, [new("flight/opaque", fixture.ProductId)])), default);
        AssertCode(missingPrice, "price_unavailable");
        var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.Equal(0, await database.CheckoutOffers.CountAsync());
        Assert.Equal(0, await database.Reservations.CountAsync());
    }

    private static ReservationRequest Request(Guid offerId, int count = 1)
    {
        return new ReservationRequest(offerId, "request-1", "Guest", "guest@example.test",
            Enumerable.Range(1, count).Select(i => new PassengerRequest($"First{i}", "Last", new DateOnly(1990, 1, 1))).ToArray());
    }

    private static void AssertCode<T>(FluentResults.Result<T> result, string code)
    {
        Assert.True(result.IsFailed);
        Assert.Equal(code, Assert.IsType<BookingError>(Assert.Single(result.Errors)).Code);
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }

        public void Advance(TimeSpan value)
        {
            now += value;
        }
    }

    private sealed class Fixture(SqliteConnection connection, ServiceProvider provider, Clock clock, Guid productId) : IAsyncDisposable
    {
        public ServiceProvider Provider { get; } = provider;
        public Clock Clock { get; } = clock;
        public Guid ProductId { get; } = productId;

        public static async Task<Fixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var clock = new Clock();
            var builder = WebApplication.CreateBuilder();
            builder.AddApplication();
            builder.Services.AddSingleton<TimeProvider>(clock);
            builder.Services.AddDbContext<BookingDbContext>(options => options.UseSqlite(connection));
            builder.Services.AddSharedUnitOfWork<BookingDbContext>();
            builder.Services.AddSharedInfrastructure();
            builder.Services.AddScoped<IGuestCheckoutRepository, GuestCheckoutRepository>();
            builder.Services.AddScoped<ICheckoutOfferRepository, CheckoutOfferRepository>();
            builder.Services.AddScoped<IFareProductRepository, FareProductRepository>();
            builder.Services.AddScoped<IFlightPriceRepository, FlightPriceRepository>();
            builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
            builder.Services.AddScoped<IReservationCheckoutRepository, ReservationCheckoutRepository>();
            builder.Services.AddScoped<IOfferQueries, OfferQueries>();
            builder.Services.AddScoped<IReservationQueries, ReservationQueries>();
            builder.Services.AddMassTransit(bus =>
            {
                bus.AddSharedEntityFrameworkOutbox<BookingDbContext>(outbox =>
                {
                    outbox.UsePostgres();
                    outbox.DisableInboxCleanupService();
                });
                bus.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
            });
            var provider = builder.Services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            await database.Database.EnsureCreatedAsync();
            var product = FareProduct.Create("LIGHT", 1, "Economy", "Original brand",
                BaggageAllowance.Create(1, 23m, 1, 50m));
            database.FareProducts.Add(product);
            database.FlightPrices.Add(FlightPrice.Create("flight/opaque", product.Id, "PLN", 250m, " opaque cabin "));
            await database.SaveChangesAsync();
            return new Fixture(connection, provider, clock, product.Id);
        }

        public async Task<string> Checkout()
        {
            await using var scope = Provider.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new CreateCheckoutCommand(), default);
            return result.Value.GuestToken;
        }

        public async Task<OfferResponse> Offer(string token, int passengerCount = 1)
        {
            await using var scope = Provider.CreateAsyncScope();
            var created = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new CreateOfferCommand(token,
                new OfferRequest("PLN", passengerCount, [new("flight/opaque", ProductId)])), default);
            Assert.True(created.IsSuccess);
            var offer = await scope.ServiceProvider.GetRequiredService<IOfferQueries>()
                .GetOffer(new GetOfferRequest(created.Value, token), default);
            Assert.True(offer.IsSuccess);
            return offer.Value;
        }

        public async Task<FluentResults.Result<ReservationResponse>> Reserve(string token, ReservationRequest request)
        {
            await using var scope = Provider.CreateAsyncScope();
            var created = await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new CreateReservationCommand(token, request), default);
            if (created.IsFailed)
            {
                return created.ToResult<ReservationResponse>();
            }

            return await scope.ServiceProvider.GetRequiredService<IReservationQueries>()
                .GetReservation(new GetReservationRequest(created.Value, token), default);
        }

        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
