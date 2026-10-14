using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class SeatMapPositionConfiguration : IEntityTypeConfiguration<SeatMapPosition>
{
    public void Configure(EntityTypeBuilder<SeatMapPosition> builder)
    {
        builder.ToTable("SeatMapPositions");
        builder.HasKey(position => position.Id);
        builder.Property(position => position.Column).IsRequired().HasMaxLength(16);
        builder.HasIndex(position => new { position.SeatMapRowId, position.Column }).IsUnique();
    }
}
