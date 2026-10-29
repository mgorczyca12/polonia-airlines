using seating_service.Application.Features.InventoryFeatures.Errors;
using seating_service.Domain.Entities;
using Shared.Infrastructure.Messaging;
using Shared.Integration.Contracts;

namespace seating_service.Application.Features.InventoryFeatures;

internal static class InventoryOutcomePublisherExtensions
{
    public static Task PublishRejectionAsync(
        this IIntegrationEventPublisher publisher,
        InventoryOperation operation,
        CancellationToken cancellationToken)
    {
        var code = operation.RejectionCode ?? InventoryRejectionCode.Cancelled.ToContractCode();

        return publisher.PublishAsync(
            new ReservationInventoryRejected(operation.CorrelationId, code),
            cancellationToken);
    }
}
