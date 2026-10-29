using FluentValidation;

namespace seating_service.Application.Features.InventoryFeatures.Commands.ReleaseInventory;

public sealed class ReleaseInventoryCommandValidator : AbstractValidator<ReleaseInventoryCommand>
{
    public ReleaseInventoryCommandValidator()
    {
        RuleFor(command => command.CorrelationId).NotEqual(Guid.Empty);
    }
}
