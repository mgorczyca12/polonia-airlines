using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.ToTable("Flights");
        builder.HasKey(flight => flight.Id);
        builder.Ignore(flight => flight.DomainEvents);
        builder.Property(flight => flight.FlightNumber).IsRequired().HasMaxLength(32);
        builder.HasOne(flight => flight.DepartureAirport).WithMany()
            .HasForeignKey(flight => flight.DepartureAirportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(flight => flight.ArrivalAirport).WithMany()
            .HasForeignKey(flight => flight.ArrivalAirportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(flight => flight.FlightSegments).WithOne(segment => segment.Flight)
            .HasForeignKey(segment => segment.FlightId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(flight => flight.FlightSegments)
            .HasField("flightSegments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
