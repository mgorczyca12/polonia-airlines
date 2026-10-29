using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class ReservationCheckoutConfiguration : IEntityTypeConfiguration<ReservationCheckout>
{
    public void Configure(EntityTypeBuilder<ReservationCheckout> builder)
    {
        builder.ToTable("ReservationCheckouts");
        builder.HasKey(checkout => checkout.Id);
        builder.Property(checkout => checkout.Id).ValueGeneratedNever();
        builder.Ignore(checkout => checkout.DomainEvents);
        builder.Property(checkout => checkout.GuestTokenHash).IsRequired().HasMaxLength(64);
        builder.Property(checkout => checkout.RequestId).IsRequired().HasMaxLength(100);
        builder.Property(checkout => checkout.PayloadHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(checkout => new { checkout.GuestTokenHash, checkout.RequestId })
            .IsUnique().HasDatabaseName("UX_ReservationCheckout_Request");
        builder.HasIndex(checkout => checkout.OfferId).IsUnique();
        builder.HasIndex(checkout => checkout.CorrelationId).IsUnique();
        builder.HasOne<Reservation>().WithMany()
            .HasForeignKey(checkout => checkout.ReservationId).OnDelete(DeleteBehavior.Restrict);
    }
}
