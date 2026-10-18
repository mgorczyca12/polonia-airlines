using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class SeatMapRowConfiguration : IEntityTypeConfiguration<SeatMapRow>
{
    public void Configure(EntityTypeBuilder<SeatMapRow> builder)
    {
        builder.ToTable("SeatMapRows");
        builder.HasKey(row => row.Id);
        builder.HasIndex(row => new { row.SeatMapId, row.Number }).IsUnique();
        builder.Property(row => row.Type)
            .HasConversion(type => type.Value, value => SeatMapRowType.FromValue(value)).IsRequired();
        builder.HasOne(row => row.Cabin).WithMany()
            .HasForeignKey(row => row.CabinId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(row => row.SeatMapPositions).WithOne(position => position.SeatMapRow)
            .HasForeignKey(position => position.SeatMapRowId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(row => row.SeatMapPositions)
            .HasField("seatMapPositions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
