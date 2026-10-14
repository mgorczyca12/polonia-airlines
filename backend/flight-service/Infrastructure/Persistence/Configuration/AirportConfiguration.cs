using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        builder.ToTable("Airports");
        builder.HasKey(airport => airport.Id);
        builder.Ignore(airport => airport.DomainEvents);
        builder.Property(airport => airport.Name).IsRequired().HasMaxLength(200);
        builder.Property(airport => airport.IcaoCode).IsRequired().HasMaxLength(4);
        builder.Property(airport => airport.IataCode).IsRequired().HasMaxLength(3);
        builder.HasIndex(airport => airport.IcaoCode).IsUnique();
        builder.HasIndex(airport => airport.IataCode).IsUnique();
        builder.OwnsOne(airport => airport.Address, address =>
        {
            address.Property(value => value.City).IsRequired().HasMaxLength(200);
            address.Property(value => value.State).IsRequired().HasMaxLength(200);
            address.Property(value => value.Country).IsRequired().HasMaxLength(100);
        });
        builder.Navigation(airport => airport.Address).IsRequired();
        builder.HasMany(airport => airport.Gates).WithOne(gate => gate.Airport)
            .HasForeignKey(gate => gate.AirportId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(airport => airport.Gates)
            .HasField("gates").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
