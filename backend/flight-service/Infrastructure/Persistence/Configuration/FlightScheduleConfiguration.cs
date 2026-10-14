using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace flight_service.Infrastructure.Persistence.Configuration;

public sealed class FlightScheduleConfiguration : IEntityTypeConfiguration<FlightSchedule>
{
    public void Configure(EntityTypeBuilder<FlightSchedule> builder)
    {
        builder.ToTable("FlightSchedules");
        builder.HasKey(schedule => schedule.Id);
        builder.Ignore(schedule => schedule.DomainEvents);
        builder.Property(schedule => schedule.OperatingDays)
            .HasConversion(days => days.Value, value => OperatingDays.FromValue(value)).IsRequired();
        builder.HasOne(schedule => schedule.Flight).WithMany()
            .HasForeignKey(schedule => schedule.FlightId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(schedule => schedule.DepartureAirport).WithMany()
            .HasForeignKey(schedule => schedule.DepartureAirportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(schedule => schedule.ArrivalAirport).WithMany()
            .HasForeignKey(schedule => schedule.ArrivalAirportId).OnDelete(DeleteBehavior.Restrict);
    }
}
