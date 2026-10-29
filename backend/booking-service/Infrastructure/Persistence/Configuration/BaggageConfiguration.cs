using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class BaggageConfiguration : IEntityTypeConfiguration<Baggage>
{
    public void Configure(EntityTypeBuilder<Baggage> builder)
    {
        builder.ToTable("Baggage");
        builder.HasKey(baggage => baggage.Id);
        builder.Property(baggage => baggage.Name).IsRequired().HasMaxLength(200);
        builder.Property(baggage => baggage.WeightKg).HasPrecision(9, 3);
        builder.Property(baggage => baggage.Charge).HasPrecision(18, 4);
        builder.HasOne(baggage => baggage.PassengerFare).WithMany()
            .HasForeignKey(baggage => baggage.PassengerFareId).OnDelete(DeleteBehavior.Restrict);
    }
}
