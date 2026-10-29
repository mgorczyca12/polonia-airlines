using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.ToTable("Passengers");
        builder.HasKey(passenger => passenger.Id);
        builder.Ignore(passenger => passenger.TotalAmount);
        builder.Property(passenger => passenger.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(passenger => passenger.LastName).IsRequired().HasMaxLength(100);
        builder.HasMany(passenger => passenger.Fares).WithOne(fare => fare.Passenger)
            .HasForeignKey(fare => fare.PassengerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(passenger => passenger.Baggage).WithOne(baggage => baggage.Passenger)
            .HasForeignKey(baggage => baggage.PassengerId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(passenger => passenger.Fares)
            .HasField("fares").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(passenger => passenger.Baggage)
            .HasField("baggage").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
