using FluentValidation;

namespace flight_service.Application.Features.ScheduleFeatures.Commands.CreateSchedule;

public sealed class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator()
    {
        RuleFor(command => command.DaysOfWeek).NotEmpty();
        RuleForEach(command => command.DaysOfWeek).IsInEnum();
    }
}
