using booking_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class FareProductConfiguration : IEntityTypeConfiguration<FareProduct>
{
    public void Configure(EntityTypeBuilder<FareProduct> builder)
    {
        builder.ToTable("FareProducts");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.Ignore(product => product.DomainEvents);
        builder.Property(product => product.Code).IsRequired().HasMaxLength(100);
        builder.Property(product => product.CabinClass).IsRequired().HasMaxLength(100);
        builder.Property(product => product.Brand).IsRequired().HasMaxLength(100);
        builder.HasIndex(product => new { product.Code, product.Version }).IsUnique();
        builder.OwnsOne(product => product.BaggageAllowance, allowance =>
        {
            allowance.Ignore(value => value.MaxPieces);
            allowance.Property(value => value.MaxWeightPerPieceKg).HasPrecision(9, 3);
            allowance.Property(value => value.ExtraPiecePrice).HasPrecision(18, 4);
        });
    }
}
