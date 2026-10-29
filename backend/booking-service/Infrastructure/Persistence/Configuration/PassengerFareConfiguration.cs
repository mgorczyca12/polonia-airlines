using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class PassengerFareConfiguration : IEntityTypeConfiguration<PassengerFare>
{
    public void Configure(EntityTypeBuilder<PassengerFare> builder)
    {
        builder.ToTable("PassengerFares");
        builder.HasKey(fare => fare.Id);
        builder.Property(fare => fare.ProductCode).IsRequired().HasMaxLength(100);
        builder.Property(fare => fare.CabinClass).IsRequired().HasMaxLength(100);
        builder.Property(fare => fare.CabinId).IsRequired().HasMaxLength(200);
        builder.Property(fare => fare.Brand).IsRequired().HasMaxLength(100);
        builder.Property(fare => fare.Currency).IsRequired().HasMaxLength(3);
        builder.Property(fare => fare.Amount).HasPrecision(18, 4);
        builder.HasOne(fare => fare.ReservationFlight).WithMany()
            .HasForeignKey(fare => fare.ReservationFlightId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(fare => new { fare.PassengerId, fare.ReservationFlightId }).IsUnique();
        builder.OwnsOne(fare => fare.BaggageAllowance, allowance =>
        {
            allowance.Ignore(value => value.MaxPieces);
            allowance.Property(value => value.MaxWeightPerPieceKg).HasPrecision(9, 3);
            allowance.Property(value => value.ExtraPiecePrice).HasPrecision(18, 4);
        });
    }
}
