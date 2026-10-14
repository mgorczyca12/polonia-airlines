using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class AirportGateConfiguration : IEntityTypeConfiguration<AirportGate>
{
    public void Configure(EntityTypeBuilder<AirportGate> builder)
    {
        builder.ToTable("AirportGates");
        builder.HasKey(gate => gate.Id);
        builder.Property(gate => gate.Terminal).IsRequired().HasMaxLength(32);
        builder.Property(gate => gate.GateNumber).IsRequired().HasMaxLength(32);
        builder.HasIndex(gate => new { gate.AirportId, gate.Terminal, gate.GateNumber }).IsUnique();
    }
}
