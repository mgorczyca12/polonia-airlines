using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace booking_service.Infrastructure.Persistence.Configuration;

public sealed class CheckoutOfferConfiguration : IEntityTypeConfiguration<CheckoutOffer>
{
    public void Configure(EntityTypeBuilder<CheckoutOffer> builder)
    {
        builder.ToTable("CheckoutOffers");
        builder.HasKey(offer => offer.Id);
        builder.Property(offer => offer.Id).ValueGeneratedNever();
        builder.Ignore(offer => offer.DomainEvents);
        builder.Ignore(offer => offer.Currency);
        builder.Ignore(offer => offer.TotalAmount);
        builder.Property(offer => offer.GuestTokenHash).IsRequired().HasMaxLength(64);
        builder.HasOne<GuestCheckout>().WithMany()
            .HasForeignKey(offer => offer.GuestTokenHash).OnDelete(DeleteBehavior.Restrict);

        // Ticks keep expiry comparable and orderable on every database provider.
        builder.Property(offer => offer.ExpiresAt)
            .HasConversion(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        builder.Property(offer => offer.Fares)
            .HasConversion(
                fares => OfferedFareJson.Serialize(fares),
                json => OfferedFareJson.Deserialize(json),
                new ValueComparer<IReadOnlyList<OfferedFare>>(
                    (left, right) => left!.SequenceEqual(right!),
                    fares => fares.Aggregate(0, (hash, fare) => HashCode.Combine(hash, fare)),
                    fares => fares.ToArray()))
            .IsRequired();
    }
}
