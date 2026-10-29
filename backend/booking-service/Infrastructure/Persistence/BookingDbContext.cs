using booking_service.Domain.Entities;
using booking_service.Infrastructure.Integrations.Sagas;
using booking_service.Infrastructure.Persistence.Configuration;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace booking_service.Infrastructure.Persistence;

public class BookingDbContext : DbContext
{
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<FareProduct> FareProducts { get; set; }
    public DbSet<FlightPrice> FlightPrices { get; set; }
    public DbSet<GuestCheckout> GuestCheckouts { get; set; }
    public DbSet<CheckoutOffer> CheckoutOffers { get; set; }
    public DbSet<ReservationCheckout> ReservationCheckouts { get; set; }

    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ReservationConfiguration).Assembly,
            type => type.Namespace == typeof(ReservationConfiguration).Namespace);
        ((ISagaClassMap)new ReservationSagaMap()).Configure(modelBuilder);
    }
}
