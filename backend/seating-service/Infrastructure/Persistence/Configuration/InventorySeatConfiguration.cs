using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using seating_service.Domain.Entities;

namespace seating_service.Infrastructure.Persistence.Configuration;

public sealed class InventorySeatConfiguration : IEntityTypeConfiguration<InventorySeat>
{
    public void Configure(EntityTypeBuilder<InventorySeat> builder)
    {
        builder.ToTable("Seats");
        builder.Ignore(value => value.Id);
        builder.HasKey(value => new { value.FlightId, value.SeatId });
        builder.Property(value => value.FlightId).UseCollation("C");
        builder.Property(value => value.CabinId).UseCollation("C");
        builder.Property(value => value.SeatId).UseCollation("C");
    }
}
