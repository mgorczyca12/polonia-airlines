using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Reference).IsRequired().HasMaxLength(200);
        builder.Property(payment => payment.Currency).IsRequired().HasMaxLength(3);
        builder.Property(payment => payment.Amount).HasPrecision(18, 4);
        builder.HasIndex(payment => new { payment.ReservationId, payment.Reference }).IsUnique();
    }
}
