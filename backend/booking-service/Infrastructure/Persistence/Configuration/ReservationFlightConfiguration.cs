using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class ReservationFlightConfiguration : IEntityTypeConfiguration<ReservationFlight>
{
    public void Configure(EntityTypeBuilder<ReservationFlight> builder)
    {
        builder.ToTable("ReservationFlights");
        builder.HasKey(flight => flight.Id);
        builder.Property(flight => flight.FlightId).IsRequired().HasMaxLength(200);
        builder.HasIndex(flight => new { flight.ReservationId, flight.FlightId }).IsUnique();
        builder.HasIndex(flight => new { flight.ReservationId, flight.SequenceNumber }).IsUnique();
    }
}
