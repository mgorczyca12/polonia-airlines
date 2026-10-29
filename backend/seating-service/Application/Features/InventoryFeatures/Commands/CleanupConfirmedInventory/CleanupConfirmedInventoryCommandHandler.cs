using DispatchR.Abstractions.Send;
using FluentResults;
using seating_service.Application.Abstractions;
using seating_service.Domain.ValueObjects;

namespace seating_service.Application.Features.InventoryFeatures.Commands.CleanupConfirmedInventory;

public sealed class CleanupConfirmedInventoryCommandHandler(
    IInventoryMutationLock mutationLock,
    IInventoryOperationRepository operations,
    IInventoryHolds holds) : IRequestHandler<CleanupConfirmedInventoryCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(
        CleanupConfirmedInventoryCommand request,
        CancellationToken cancellationToken)
    {
        await mutationLock.AcquireAsync(cancellationToken);

        var operation = await operations.GetByIdAsync(request.CorrelationId, cancellationToken);
        if (operation?.State != InventoryOperationState.Confirmed)
        {
            return Result.Ok();
        }

        await holds.Release(request.CorrelationId, cancellationToken);

        return Result.Ok();
    }
}
