using Microsoft.EntityFrameworkCore;
using seating_service.Application.Abstractions;
using seating_service.Application.Features.InventoryFeatures.Errors;
using seating_service.Domain.Entities;
using seating_service.Infrastructure.Persistence;

namespace seating_service.Infrastructure.Repositories;

public sealed class InventoryReleaseFence(
    DbContextOptions<SeatingDbContext> options) : IInventoryReleaseFence
{
    public async Task EnsureReleased(Guid correlationId, CancellationToken cancellationToken)
    {
        // Independent commit fences late Hold/Confirm even if Redis cleanup or the consumer transaction fails.
        await using var database = new SeatingDbContext(options);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        await new SqlInventoryMutationLock(database).AcquireAsync(cancellationToken);

        var operations = new InventoryOperationRepository(database);
        var operation = await operations.GetByIdAsync(correlationId, cancellationToken);
        if (operation is null)
        {
            operation = InventoryOperation.Open(correlationId);
            operations.Add(operation);
        }

        operation.Fence(InventoryRejectionCode.Cancelled.ToContractCode());

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
