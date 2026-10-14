using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class SeatMapCabinConfiguration : IEntityTypeConfiguration<SeatMapCabin>
{
    public void Configure(EntityTypeBuilder<SeatMapCabin> builder)
    {
        builder.ToTable("SeatMapCabins");
        builder.HasKey(cabin => cabin.Id);
        builder.Property(cabin => cabin.Name).IsRequired().HasMaxLength(200);
        builder.Property(cabin => cabin.IataCode).IsRequired().HasMaxLength(8);
        builder.HasIndex(cabin => cabin.IataCode).IsUnique();
    }
}
