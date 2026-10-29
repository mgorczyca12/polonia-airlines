using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Sagas;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace booking_service.Infrastructure.Persistence;

public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<FareProduct> FareProducts => Set<FareProduct>();
    public DbSet<FlightPrice> FlightPrices => Set<FlightPrice>();
    public DbSet<GuestCheckout> GuestCheckouts => Set<GuestCheckout>();
    public DbSet<CheckoutOffer> Offers => Set<CheckoutOffer>();
    public DbSet<ReservationCheckout> ReservationCheckouts => Set<ReservationCheckout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reservation = modelBuilder.Entity<Reservation>();
        reservation.Ignore(x => x.DomainEvents);
        reservation.Ignore(x => x.TotalAmount);
        reservation.Ignore(x => x.FlightIds);
        reservation.Property(x => x.Code).HasConversion(x => x.Value, x => PnrCode.From(x)).HasMaxLength(6);
        reservation.Property(x => x.Status).HasConversion(x => x.Status, x => ReservationStatus.From(x));
        reservation.Property(x => x.Currency).HasMaxLength(3);
        reservation.Property(x => x.ContactName).HasMaxLength(200);
        reservation.Property(x => x.ContactEmail).HasMaxLength(254);
        reservation.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Reservation_Pnr");
        reservation.HasMany(x => x.Flights).WithOne(x => x.Reservation).HasForeignKey(x => x.ReservationId);
        reservation.HasMany(x => x.Passengers).WithOne(x => x.Reservation).HasForeignKey(x => x.ReservationId);
        reservation.HasMany(x => x.Payments).WithOne(x => x.Reservation).HasForeignKey(x => x.ReservationId);
        reservation.Navigation(x => x.Flights).UsePropertyAccessMode(PropertyAccessMode.Field);
        reservation.Navigation(x => x.Passengers).UsePropertyAccessMode(PropertyAccessMode.Field);
        reservation.Navigation(x => x.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);

        var passenger = modelBuilder.Entity<Passenger>();
        passenger.Ignore(x => x.TotalAmount);
        passenger.HasMany(x => x.Fares).WithOne(x => x.Passenger).HasForeignKey(x => x.PassengerId);
        passenger.HasMany(x => x.Baggage).WithOne(x => x.Passenger).HasForeignKey(x => x.PassengerId);
        passenger.Navigation(x => x.Fares).UsePropertyAccessMode(PropertyAccessMode.Field);
        passenger.Navigation(x => x.Baggage).UsePropertyAccessMode(PropertyAccessMode.Field);
        modelBuilder.Entity<PassengerFare>().HasOne(x => x.ReservationFlight).WithMany().HasForeignKey(x => x.ReservationFlightId);
        modelBuilder.Entity<PassengerFare>().HasIndex(x => new { x.PassengerId, x.ReservationFlightId }).IsUnique();
        modelBuilder.Entity<ReservationFlight>().HasIndex(x => new { x.ReservationId, x.FlightId }).IsUnique();
        modelBuilder.Entity<ReservationFlight>().HasIndex(x => new { x.ReservationId, x.SequenceNumber }).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => new { x.ReservationId, x.Reference }).IsUnique();
        modelBuilder.Entity<Baggage>().HasOne(x => x.PassengerFare).WithMany().HasForeignKey(x => x.PassengerFareId);
        modelBuilder.Entity<PassengerFare>().OwnsOne(x => x.BaggageAllowance, owned => owned.Ignore(x => x.MaxPieces));
        modelBuilder.Entity<FareProduct>().OwnsOne(x => x.BaggageAllowance, owned => owned.Ignore(x => x.MaxPieces));
        modelBuilder.Entity<FareProduct>().HasIndex(x => new { x.Code, x.Version }).IsUnique();
        modelBuilder.Entity<FareProduct>().Property(x => x.Code).HasMaxLength(100);
        modelBuilder.Entity<FareProduct>().Property(x => x.CabinClass).HasMaxLength(100);
        modelBuilder.Entity<FlightPrice>().Property(x => x.CabinId).HasMaxLength(200);
        modelBuilder.Entity<FlightPrice>().Property(x => x.FlightId).HasMaxLength(200);
        modelBuilder.Entity<FlightPrice>().Property(x => x.Currency).HasMaxLength(3);
        modelBuilder.Entity<FlightPrice>().Property(x => x.Amount).HasPrecision(18, 4);
        modelBuilder.Entity<GuestCheckout>().Property(x => x.TokenHash).HasMaxLength(64);
        modelBuilder.Entity<ReservationCheckout>().Property(x => x.GuestTokenHash).HasMaxLength(64);
        modelBuilder.Entity<ReservationCheckout>().Property(x => x.RequestId).HasMaxLength(100);
        modelBuilder.Entity<ReservationCheckout>().Property(x => x.PayloadHash).HasMaxLength(64);
        modelBuilder.Entity<FlightPrice>().HasIndex(x => new { x.FlightId, x.ProductId, x.Currency }).IsUnique();
        modelBuilder.Entity<FlightPrice>().HasOne<FareProduct>().WithMany().HasForeignKey(x => x.ProductId);
        modelBuilder.Entity<GuestCheckout>().HasKey(x => x.TokenHash);
        modelBuilder.Entity<CheckoutOffer>().HasOne<GuestCheckout>().WithMany().HasForeignKey(x => x.GuestTokenHash);
        modelBuilder.Entity<CheckoutOffer>().Property(x => x.ExpiresAt)
            .HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
        modelBuilder.Entity<ReservationCheckout>().HasIndex(x => new { x.GuestTokenHash, x.RequestId })
            .IsUnique().HasDatabaseName("UX_ReservationCheckout_Request");
        modelBuilder.Entity<ReservationCheckout>().HasIndex(x => x.OfferId).IsUnique();
        modelBuilder.Entity<ReservationCheckout>().HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId);
        ((MassTransit.EntityFrameworkCoreIntegration.ISagaClassMap)new ReservationSagaMap()).Configure(modelBuilder);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
