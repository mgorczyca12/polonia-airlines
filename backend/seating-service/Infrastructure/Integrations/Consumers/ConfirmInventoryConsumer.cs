using DispatchR;
using MassTransit;
using seating_service.Application.Features.InventoryFeatures.Commands.ConfirmInventory;
using Shared.Integration.Contracts;

namespace seating_service.Infrastructure.Integrations.Consumers;

public sealed class ConfirmInventoryConsumer(IMediator mediator) : IConsumer<ConfirmReservationInventory>
{
    public async Task Consume(ConsumeContext<ConfirmReservationInventory> context)
    {
        var command = new ConfirmInventoryCommand(context.Message.CorrelationId);

        var result = await mediator.Send(command, context.CancellationToken);
        result.ThrowIfFailed();
    }
}
