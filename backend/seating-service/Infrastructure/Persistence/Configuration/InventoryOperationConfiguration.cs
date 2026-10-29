using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;
using seating_service.Infrastructure.Persistence.Converters;

namespace seating_service.Infrastructure.Persistence.Configuration;

public sealed class InventoryOperationConfiguration : IEntityTypeConfiguration<InventoryOperation>
{
    public void Configure(EntityTypeBuilder<InventoryOperation> builder)
    {
        builder.ToTable("Operations");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).HasColumnName("CorrelationId");
        builder.Ignore(value => value.CorrelationId);
        builder.Ignore(value => value.DomainEvents);
        builder.Property(value => value.ReservationId).IsRequired();
        builder.Property(value => value.Selections)
            .HasColumnName("SelectionsJson")
            .HasConversion(new HeldSelectionsConverter(), HeldSelectionsConverter.Comparer)
            .IsRequired();
        builder.Property(value => value.State)
            .HasConversion(state => state.Code, code => InventoryOperationState.From(code))
            .HasMaxLength(32)
            .IsRequired();
    }
}
