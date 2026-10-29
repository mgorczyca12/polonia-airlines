using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using seating_service.Domain.Entities;

namespace seating_service.Infrastructure.Persistence.Configuration;

public sealed class SeatInventoryConfiguration : IEntityTypeConfiguration<SeatInventory>
{
    public void Configure(EntityTypeBuilder<SeatInventory> builder)
    {
        builder.ToTable("SeatInventories");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.FlightId).UseCollation("C");
        builder.Property(value => value.CabinId).UseCollation("C");
        builder.HasIndex(value => new { value.FlightId, value.CabinId }).IsUnique();
        builder.Ignore(value => value.DomainEvents);
        builder.Ignore(value => value.BookingLimit);
        builder.Ignore(value => value.RemainingBookingCapacity);
        builder.Ignore(value => value.OverbookedSeats);
        builder.HasMany(value => value.Assignments)
            .WithOne(value => value.SeatInventory)
            .HasForeignKey(value => value.SeatInventoryId)
            .IsRequired();
        builder.Navigation(value => value.Assignments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
