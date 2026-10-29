using MassTransit;
using Microsoft.EntityFrameworkCore;
using seating_service.Domain.Entities;

namespace seating_service.Infrastructure.Persistence;

public sealed class SeatingDbContext(DbContextOptions<SeatingDbContext> options) : DbContext(options)
{
    public DbSet<SeatInventory> Inventories => Set<SeatInventory>();
    public DbSet<SeatAssignment> Assignments => Set<SeatAssignment>();
    public DbSet<InventoryOperation> Operations => Set<InventoryOperation>();
    public DbSet<InventorySeat> Seats => Set<InventorySeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var inventory = modelBuilder.Entity<SeatInventory>();
        inventory.ToTable("SeatInventories");
        inventory.HasKey(value => value.Id);
        inventory.Property(value => value.FlightId).UseCollation("C");
        inventory.Property(value => value.CabinId).UseCollation("C");
        inventory.HasIndex(value => new { value.FlightId, value.CabinId }).IsUnique();
        inventory.Ignore(value => value.DomainEvents);
        inventory.Ignore(value => value.BookingLimit);
        inventory.Ignore(value => value.RemainingBookingCapacity);
        inventory.Ignore(value => value.OverbookedSeats);
        inventory.HasMany(value => value.Assignments)
            .WithOne(value => value.SeatInventory)
            .HasForeignKey(value => value.SeatInventoryId)
            .IsRequired();
        inventory.Navigation(value => value.Assignments).UsePropertyAccessMode(PropertyAccessMode.Field);

        var assignment = modelBuilder.Entity<SeatAssignment>();
        assignment.HasKey(value => value.Id);
        assignment.Property(value => value.FlightId).UseCollation("C");
        assignment.Property(value => value.ReservationId).UseCollation("C");
        assignment.Property(value => value.PassengerId).UseCollation("C");
        assignment.Property(value => value.SeatId).UseCollation("C");
        assignment.HasIndex(value => new { value.SeatInventoryId, value.PassengerId }).IsUnique();
        assignment.HasIndex(value => new { value.FlightId, value.SeatId })
            .IsUnique()
            .HasFilter("\"SeatId\" IS NOT NULL");

        modelBuilder.Entity<InventoryOperation>().HasKey(value => value.CorrelationId);
        modelBuilder.Entity<InventoryMutationLock>().HasKey(value => value.Id);
        modelBuilder.Entity<InventoryMutationLock>().HasData(new InventoryMutationLock { Id = 1 });
        var seat = modelBuilder.Entity<InventorySeat>();
        seat.HasKey(value => new { value.FlightId, value.SeatId });
        seat.Property(value => value.FlightId).UseCollation("C");
        seat.Property(value => value.CabinId).UseCollation("C");
        seat.Property(value => value.SeatId).UseCollation("C");

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    public async Task LockMutations(CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Inventory mutations require the consumer outbox SQL transaction.");
        }

        var guard = await Set<InventoryMutationLock>()
            .FromSqlRaw("SELECT * FROM \"InventoryMutationLock\" WHERE \"Id\" = 1 FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (guard is null)
        {
            throw new InvalidOperationException("Inventory mutation lock is missing; provision the database first.");
        }
    }
}
