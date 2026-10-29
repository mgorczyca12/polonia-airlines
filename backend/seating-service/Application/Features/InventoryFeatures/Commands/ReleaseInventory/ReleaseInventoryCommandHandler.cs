using DispatchR.Abstractions.Send;
using FluentResults;
using seating_service.Application.Abstractions;
using seating_service.Application.Features.InventoryFeatures.Errors;
using Shared.Application.Abstractions;
using Shared.Infrastructure.Messaging;
using Shared.Integration.Contracts;

namespace seating_service.Application.Features.InventoryFeatures.Commands.ReleaseInventory;

public sealed class ReleaseInventoryCommandHandler(
    IInventoryReleaseFence releaseFence,
    IInventoryMutationLock mutationLock,
    IInventoryOperationRepository operations,
    IInventoryRepository inventories,
    IInventoryHolds holds,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork) : IRequestHandler<ReleaseInventoryCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(ReleaseInventoryCommand request, CancellationToken cancellationToken)
    {
        await releaseFence.EnsureReleased(request.CorrelationId, cancellationToken);
        await mutationLock.AcquireAsync(cancellationToken);

        var operation = await operations.GetByIdAsync(request.CorrelationId, cancellationToken)
            ?? throw new InvalidOperationException("The durable release fence is missing.");

        if (operation.AllocationsPendingRelease)
        {
            var selections = operation.Selections;
            var loaded = await inventories.GetLockedAsync(selections.Items, cancellationToken);

            foreach (var selection in selections.Items)
            {
                var inventory = loaded.FindFor(selection);
                var assignment = inventory.Assignments.Single(value =>
                    value.PassengerId == selection.PassengerId && value.ReservationId == operation.ReservationId);

                inventory.ReleaseAllocation(assignment);
                inventories.RemoveAssignment(assignment);
            }
        }

        await holds.Release(request.CorrelationId, cancellationToken);

        operation.CompleteRelease(InventoryRejectionCode.Cancelled.ToContractCode());
        await publisher.PublishAsync(new ReservationInventoryReleased(request.CorrelationId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
