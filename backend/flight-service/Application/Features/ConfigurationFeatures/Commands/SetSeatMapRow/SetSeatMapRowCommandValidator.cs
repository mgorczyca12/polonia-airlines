using flight_service.Application.Contracts.Requests;
using FluentValidation;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.SetSeatMapRow;

public sealed class SetSeatMapRowCommandValidator : AbstractValidator<SetSeatMapRowCommand>
{
    public SetSeatMapRowCommandValidator()
    {
        RuleFor(command => command.ConfigurationId).GreaterThan(0);
        RuleFor(command => command.RowNumber).GreaterThan(0);
        RuleFor(command => command.Row).NotNull().SetValidator(new SeatMapRowRequestValidator());
    }
}

public sealed class SeatMapRowRequestValidator : AbstractValidator<SeatMapRowRequest>
{
    public SeatMapRowRequestValidator()
    {
        RuleFor(row => row.CabinId).GreaterThan(0);
        RuleFor(row => row.Type)
            .Must(type => (int)type > 0 && ((int)type & ~31) == 0)
            .WithMessage("Select one or more defined row types.");
        RuleFor(row => row.Positions).NotNull();
        When(row => row.Positions is not null, () =>
        {
            RuleForEach(row => row.Positions).NotNull().SetValidator(new SeatMapPositionRequestValidator());
            RuleFor(row => row.Positions)
                .Must(positions => positions
                    .Where(position => position is not null)
                    .Select(position => position.Column)
                    .Distinct(StringComparer.Ordinal)
                    .Count()
                    == positions.Count(position => position is not null))
                .WithMessage("Position columns must be unique within a row.");
        });
    }
}

public sealed class SeatMapPositionRequestValidator : AbstractValidator<SeatMapPositionRequest>
{
    public SeatMapPositionRequestValidator()
    {
        RuleFor(position => position.Column).NotEmpty().MaximumLength(16);
        RuleFor(position => position.AisleGroup).GreaterThanOrEqualTo(0);
    }
}
