using DispatchR;
using MassTransit;
using seating_service.Application.Features.InventoryFeatures.Commands.ReleaseInventory;
using Shared.Integration.Contracts;

namespace seating_service.Infrastructure.Integrations.Consumers;

public sealed class ReleaseInventoryConsumer(IMediator mediator) : IConsumer<ReleaseReservationInventory>
{
    public async Task Consume(ConsumeContext<ReleaseReservationInventory> context)
    {
        var command = new ReleaseInventoryCommand(context.Message.CorrelationId);

        var result = await mediator.Send(command, context.CancellationToken);
        result.ThrowIfFailed();
    }
}
