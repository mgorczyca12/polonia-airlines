using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class AircraftEntityConfiguration : IEntityTypeConfiguration<Aircraft>
{
    public void Configure(EntityTypeBuilder<Aircraft> builder)
    {
        builder.ToTable("Aircrafts");
        builder.HasKey(aircraft => aircraft.Id);
        builder.Ignore(aircraft => aircraft.DomainEvents);
        builder.Property(aircraft => aircraft.Registration).IsRequired().HasMaxLength(32);
        builder.Property(aircraft => aircraft.Icao24Address).IsRequired().HasMaxLength(6);
        builder.HasIndex(aircraft => aircraft.Registration).IsUnique();
        builder.HasIndex(aircraft => aircraft.Icao24Address).IsUnique();
        builder.Property(aircraft => aircraft.OperationalState)
            .HasConversion(state => state.Code, code => RestoreState(code))
            .HasMaxLength(32).IsRequired()
            .Metadata.SetValueComparer(new ValueComparer<AircraftOperationalState>(
                (left, right) => left == null ? right == null : right != null && left.Code == right.Code,
                state => state.Code.GetHashCode(),
                state => state));
        builder.HasOne(aircraft => aircraft.AircraftType).WithMany()
            .HasForeignKey(aircraft => aircraft.AircraftTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(aircraft => aircraft.AircraftConfiguration).WithMany()
            .HasForeignKey(aircraft => aircraft.AircraftConfigurationId).OnDelete(DeleteBehavior.Restrict);
    }

    // Domain transitions compare the canonical instances by reference.
    private static AircraftOperationalState RestoreState(string code)
    {
        return code switch
        {
            "ACTIVE" => AircraftOperationalState.Active,
            "INACTIVE" => AircraftOperationalState.Inactive,
            "MAINTENANCE" => AircraftOperationalState.Maintenance,
            "DECOMMISSIONED" => AircraftOperationalState.Decommissioned,
            _ => new AircraftOperationalState(code)
        };
    }
}
