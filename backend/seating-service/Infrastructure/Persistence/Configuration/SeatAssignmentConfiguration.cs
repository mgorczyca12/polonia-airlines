using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using seating_service.Domain.Entities;

namespace seating_service.Infrastructure.Persistence.Configuration;

public sealed class SeatAssignmentConfiguration : IEntityTypeConfiguration<SeatAssignment>
{
    public void Configure(EntityTypeBuilder<SeatAssignment> builder)
    {
        builder.ToTable("Assignments");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.FlightId).UseCollation("C");
        builder.Property(value => value.ReservationId).UseCollation("C");
        builder.Property(value => value.PassengerId).UseCollation("C");
        builder.Property(value => value.SeatId).UseCollation("C");
        builder.HasIndex(value => new { value.SeatInventoryId, value.PassengerId }).IsUnique();
        builder.HasIndex(value => new { value.FlightId, value.SeatId })
            .IsUnique()
            .HasFilter("\"SeatId\" IS NOT NULL");
    }
}
