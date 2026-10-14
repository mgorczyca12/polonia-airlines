using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class FlightSegmentConfiguration : IEntityTypeConfiguration<FlightSegment>
{
    public void Configure(EntityTypeBuilder<FlightSegment> builder)
    {
        builder.ToTable("FlightSegments");
        builder.HasKey(segment => segment.Id);
        builder.HasIndex(segment => new { segment.FlightId, segment.SequenceNumber }).IsUnique();
        builder.HasOne(segment => segment.DepartureAirport).WithMany()
            .HasForeignKey(segment => segment.DepartureAirportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(segment => segment.ArrivalAirport).WithMany()
            .HasForeignKey(segment => segment.ArrivalAirportId).OnDelete(DeleteBehavior.Restrict);
    }
}
