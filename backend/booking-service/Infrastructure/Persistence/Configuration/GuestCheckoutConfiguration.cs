using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class GuestCheckoutConfiguration : IEntityTypeConfiguration<GuestCheckout>
{
    public void Configure(EntityTypeBuilder<GuestCheckout> builder)
    {
        builder.ToTable("GuestCheckouts");
        builder.HasKey(checkout => checkout.Id);
        builder.Property(checkout => checkout.Id).ValueGeneratedNever().HasMaxLength(64);
        builder.Ignore(checkout => checkout.DomainEvents);
    }
}
