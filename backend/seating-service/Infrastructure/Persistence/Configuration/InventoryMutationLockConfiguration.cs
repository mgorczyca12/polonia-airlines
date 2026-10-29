using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace seating_service.Infrastructure.Persistence.Configuration;

public sealed class InventoryMutationLockConfiguration : IEntityTypeConfiguration<InventoryMutationLock>
{
    public void Configure(EntityTypeBuilder<InventoryMutationLock> builder)
    {
        builder.ToTable("InventoryMutationLock");
        builder.HasKey(value => value.Id);
        builder.HasData(new InventoryMutationLock { Id = 1 });
    }
}
