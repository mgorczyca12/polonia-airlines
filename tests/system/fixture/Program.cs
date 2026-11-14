extern alias booking;
extern alias flight;
extern alias seating;

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Airport = flight::flight_service.Domain.Entities.Airport;
using Address = flight::flight_service.Domain.ValueObjects.Address;
using Flight = flight::flight_service.Domain.Entities.Flight;
using FlightContext = flight::flight_service.Infrastructure.Persistence.FlightServiceDbContext;
using BookingContext = booking::booking_service.Infrastructure.Persistence.BookingDbContext;
using FareProduct = booking::booking_service.Domain.Entities.FareProduct;
using FlightPrice = booking::booking_service.Domain.Entities.FlightPrice;
using BaggageAllowance = booking::booking_service.Domain.ValueObjects.BaggageAllowance;
using SeatingContext = seating::seating_service.Infrastructure.Persistence.SeatingDbContext;
using SeatInventory = seating::seating_service.Domain.Entities.SeatInventory;

var password = Environment.GetEnvironmentVariable("SYSTEM_TEST_DB_PASSWORD");
var date = Environment.GetEnvironmentVariable("SYSTEM_TEST_DEPARTURE");
if (string.IsNullOrWhiteSpace(password)
    || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var departureDate)
    || departureDate <= DateOnly.FromDateTime(DateTime.UtcNow))
{
    throw new InvalidOperationException("The isolated fixture requires a database password and a future UTC departure date.");
}

string Connection(string database)
{
    return $"Host=postgres;Database={database};Username=system_test;Password={password}";
}

await using var flights = new FlightContext(
    new DbContextOptionsBuilder<FlightContext>().UseNpgsql(Connection("polonia_flight")).Options);
await using var booking = new BookingContext(
    new DbContextOptionsBuilder<BookingContext>().UseNpgsql(Connection("polonia_booking")).Options);
await using var seating = new SeatingContext(
    new DbContextOptionsBuilder<SeatingContext>().UseNpgsql(Connection("polonia_seating")).Options);

foreach (var context in new DbContext[] { flights, booking, seating })
{
    if ((await context.Database.GetAppliedMigrationsAsync()).Any())
    {
        throw new InvalidOperationException("System fixtures require fresh, dedicated databases.");
    }

    await context.Database.MigrateAsync();
}

var origin = Airport.Create("Chicago", "KORD", "ORD", Address.Create("Chicago", "IL", "US"));
var destination = Airport.Create("Krakow", "EPKK", "KRK", Address.Create("Krakow", "Malopolska", "PL"));
var departure = departureDate.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
var available = Flight.Create("SY101", departure, departure.AddHours(8), origin, destination);
var limited = Flight.Create("SY102", departure.AddHours(1), departure.AddHours(9), origin, destination);
flights.Flights.AddRange(available, limited);
await flights.SaveChangesAsync();

var product = FareProduct.Create("SYSTEM_LIGHT", 1, "Economy", "Light", BaggageAllowance.Create(1, 23, 0, 0));
booking.FareProducts.Add(product);
foreach (var item in new[] { available, limited })
{
    var flightId = item.Id.ToString(CultureInfo.InvariantCulture);
    booking.FlightPrices.Add(FlightPrice.Create(flightId, product.Id, "USD", 125m, "system-economy"));
    seating.Inventories.Add(SeatInventory.Create(flightId, "system-economy", 1));
}

await booking.SaveChangesAsync();
await seating.SaveChangesAsync();
Console.WriteLine("System-test migrations and fixtures are ready.");
