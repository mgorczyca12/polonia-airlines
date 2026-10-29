using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public const string UniquePnrIndex = "UX_Reservation_Pnr";

    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(reservation => reservation.Id);
        builder.Ignore(reservation => reservation.DomainEvents);
        builder.Ignore(reservation => reservation.TotalAmount);
        builder.Ignore(reservation => reservation.FlightIds);
        builder.Property(reservation => reservation.Code)
            .HasConversion(code => code.Value, value => PnrCode.From(value))
            .HasMaxLength(6).IsRequired();
        builder.HasIndex(reservation => reservation.Code).IsUnique().HasDatabaseName(UniquePnrIndex);
        builder.Property(reservation => reservation.Status)
            .HasConversion(status => status.Status, value => ReservationStatus.From(value))
            .HasMaxLength(32).IsRequired();
        builder.Property(reservation => reservation.Currency).IsRequired().HasMaxLength(3);
        builder.Property(reservation => reservation.ContactName).IsRequired().HasMaxLength(200);
        builder.Property(reservation => reservation.ContactEmail).IsRequired().HasMaxLength(254);
        builder.HasMany(reservation => reservation.Flights).WithOne(flight => flight.Reservation)
            .HasForeignKey(flight => flight.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(reservation => reservation.Passengers).WithOne(passenger => passenger.Reservation)
            .HasForeignKey(passenger => passenger.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(reservation => reservation.Payments).WithOne(payment => payment.Reservation)
            .HasForeignKey(payment => payment.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(reservation => reservation.Flights)
            .HasField("flights").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(reservation => reservation.Passengers)
            .HasField("passengers").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(reservation => reservation.Payments)
            .HasField("payments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
