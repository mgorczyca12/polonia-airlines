using flight_service.Application.Features.ScheduleFeatures.Commands.CreateSchedule;

namespace flight_service.unit.test;

public sealed class CreateScheduleCommandValidatorTests
{
    [Fact]
    public void Validate_ValidDaysIncludingSunday_Succeeds()
    {
        var result = new CreateScheduleCommandValidator()
            .Validate(CreateCommand([DayOfWeek.Monday, DayOfWeek.Sunday, DayOfWeek.Monday]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullOrEmptyDays_Fails()
    {
        var validator = new CreateScheduleCommandValidator();

        Assert.False(validator.Validate(CreateCommand(null!)).IsValid);
        Assert.False(validator.Validate(CreateCommand([])).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Validate_UndefinedDay_Fails(int day)
    {
        var result = new CreateScheduleCommandValidator()
            .Validate(CreateCommand([DayOfWeek.Monday, (DayOfWeek)day]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.StartsWith("DaysOfWeek", StringComparison.Ordinal));
    }

    private static CreateScheduleCommand CreateCommand(IReadOnlyCollection<DayOfWeek> days)
    {
        return new CreateScheduleCommand(
            "PA123",
            "WAW",
            "KRK",
            new TimeOnly(10, 0),
            new TimeOnly(12, 0),
            new DateOnly(2026, 10, 16),
            new DateOnly(2026, 12, 31),
            days);
    }
}
