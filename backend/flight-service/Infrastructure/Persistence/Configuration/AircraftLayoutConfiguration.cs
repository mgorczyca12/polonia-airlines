using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class AircraftLayoutConfiguration : IEntityTypeConfiguration<AircraftConfiguration>
{
    public void Configure(EntityTypeBuilder<AircraftConfiguration> builder)
    {
        builder.ToTable("AircraftConfigurations");
        builder.HasKey(configuration => configuration.Id);
        builder.Ignore(configuration => configuration.DomainEvents);
        builder.Property(configuration => configuration.Code).IsRequired().HasMaxLength(32);
        builder.Property(configuration => configuration.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(configuration => new
        {
            configuration.AircraftTypeId,
            configuration.Code,
            configuration.Version
        }).IsUnique();
        builder.HasOne(configuration => configuration.AircraftType).WithMany()
            .HasForeignKey(configuration => configuration.AircraftTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(configuration => configuration.SeatMapRows).WithOne(row => row.AircraftConfiguration)
            .HasForeignKey(row => row.AircraftConfigurationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(configuration => configuration.SeatMapRows)
            .HasField("seatMapRows").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
