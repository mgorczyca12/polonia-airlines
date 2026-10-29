using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class FlightPriceConfiguration : IEntityTypeConfiguration<FlightPrice>
{
    public void Configure(EntityTypeBuilder<FlightPrice> builder)
    {
        builder.ToTable("FlightPrices");
        builder.HasKey(price => price.Id);
        builder.Property(price => price.Id).ValueGeneratedNever();
        builder.Ignore(price => price.DomainEvents);
        builder.Property(price => price.FlightId).IsRequired().HasMaxLength(200);
        builder.Property(price => price.CabinId).IsRequired().HasMaxLength(200);
        builder.Property(price => price.Currency).IsRequired().HasMaxLength(3);
        builder.Property(price => price.Amount).HasPrecision(18, 4);
        builder.HasIndex(price => new { price.FlightId, price.ProductId, price.Currency }).IsUnique();
        builder.HasOne<FareProduct>().WithMany()
            .HasForeignKey(price => price.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
