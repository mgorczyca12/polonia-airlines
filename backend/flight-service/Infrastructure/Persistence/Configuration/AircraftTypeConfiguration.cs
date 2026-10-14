using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class AircraftTypeConfiguration : IEntityTypeConfiguration<AircraftType>
{
    public void Configure(EntityTypeBuilder<AircraftType> builder)
    {
        builder.ToTable("AircraftTypes");
        builder.HasKey(type => type.Id);
        builder.Property(type => type.IcaoCode).IsRequired().HasMaxLength(4);
        builder.Property(type => type.Model).IsRequired().HasMaxLength(200);
        builder.Property(type => type.Manufacturer).IsRequired().HasMaxLength(200);
        builder.HasIndex(type => type.IcaoCode).IsUnique();
    }
}
