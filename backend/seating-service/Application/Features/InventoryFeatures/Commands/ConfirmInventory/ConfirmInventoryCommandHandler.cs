using DispatchR.Abstractions.Send;
using FluentResults;
using seating_service.Application.Abstractions;
using seating_service.Application.Features.InventoryFeatures.Errors;
using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;
using Shared.Application.Abstractions;
using Shared.Infrastructure.Messaging;
using Shared.Integration.Contracts;

namespace seating_service.Application.Features.InventoryFeatures.Commands.ConfirmInventory;

public sealed class ConfirmInventoryCommandHandler(
    IInventoryMutationLock mutationLock,
    IInventoryOperationRepository operations,
    IInventoryRepository inventories,
    IInventoryHolds holds,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmInventoryCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(ConfirmInventoryCommand request, CancellationToken cancellationToken)
    {
        await mutationLock.AcquireAsync(cancellationToken);

        var operation = await operations.GetByIdAsync(request.CorrelationId, cancellationToken);
        if (operation is null)
        {
            operation = InventoryOperation.Open(request.CorrelationId);
            operations.Add(operation);
            await RejectAsync(operation, InventoryRejectionCode.HoldNotFound, cancellationToken);
            return Result.Ok();
        }

        if (operation.State == InventoryOperationState.Confirmed)
        {
            await publisher.PublishAsync(
                new ReservationInventoryConfirmed(request.CorrelationId),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }

        if (operation.State != InventoryOperationState.Held)
        {
            await publisher.PublishRejectionAsync(operation, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }

        var selections = operation.Selections;
        var hold = InventoryKeys.Hold(operation.CorrelationId, operation.ExpiresAt, selections.Items);
        if (!await holds.IsValid(hold, cancellationToken))
        {
            await RejectAsync(operation, InventoryRejectionCode.HoldExpiredOrLost, cancellationToken);
            return Result.Ok();
        }

        var loaded = await inventories.GetLockedAsync(selections.Items, cancellationToken);
        if (!await InventoryProvisioning.AreProvisionedAsync(inventories, selections, loaded, cancellationToken) ||
            !loaded.All(inventory => inventory.CanConfirm(selections.Items)))
        {
            await RejectAsync(operation, InventoryRejectionCode.InventoryUnavailable, cancellationToken);
            return Result.Ok();
        }

        foreach (var selection in selections.Items)
        {
            var inventory = loaded.FindFor(selection);
            inventory.ConfirmAllocation(operation.ReservationId, selection.PassengerId, selection.SeatId);
        }

        operation.Confirm();
        await publisher.PublishAsync(
            new ReservationInventoryConfirmed(request.CorrelationId),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The confirmation outbox event triggers Redis cleanup only after this SQL transaction commits.
        return Result.Ok();
    }

    private async Task RejectAsync(
        InventoryOperation operation,
        InventoryRejectionCode code,
        CancellationToken cancellationToken)
    {
        operation.Reject(code.ToContractCode());
        await publisher.PublishRejectionAsync(operation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
