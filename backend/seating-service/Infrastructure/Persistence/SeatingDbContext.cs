using MassTransit;
using Microsoft.EntityFrameworkCore;
using seating_service.Domain.Entities;
using seating_service.Infrastructure.Persistence.Configuration;

namespace seating_service.Infrastructure.Persistence;

public sealed class SeatingDbContext(DbContextOptions<SeatingDbContext> options) : DbContext(options)
{
    public DbSet<SeatInventory> Inventories => Set<SeatInventory>();
    public DbSet<SeatAssignment> Assignments => Set<SeatAssignment>();
    public DbSet<InventoryOperation> Operations => Set<InventoryOperation>();
    public DbSet<InventorySeat> Seats => Set<InventorySeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SeatInventoryConfiguration).Assembly,
            type => type.Namespace == typeof(SeatInventoryConfiguration).Namespace);
    }
}
