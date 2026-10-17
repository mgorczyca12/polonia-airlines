using flight_service.Domain.ValueObjects;

namespace flight_service.unit.test;

public sealed class OperatingDaysTests
{
    [Theory]
    [InlineData(DayOfWeek.Monday, 1)]
    [InlineData(DayOfWeek.Tuesday, 2)]
    [InlineData(DayOfWeek.Wednesday, 4)]
    [InlineData(DayOfWeek.Thursday, 8)]
    [InlineData(DayOfWeek.Friday, 16)]
    [InlineData(DayOfWeek.Saturday, 32)]
    [InlineData(DayOfWeek.Sunday, 64)]
    public void FromDays_SingleDay_PreservesPersistedBitAndMembership(DayOfWeek day, int expected)
    {
        var days = OperatingDays.FromDays([day]);

        Assert.Equal(expected, days.Value);
        Assert.Equal(day.ToString(), days.Name);
        foreach (var candidate in Enum.GetValues<DayOfWeek>())
        {
            Assert.Equal(candidate == day, days.HasDay(candidate));
        }
    }

    [Fact]
    public void FromDays_DuplicatesAndDifferentOrdering_ProducesEqualValuesAndStableNames()
    {
        var days = OperatingDays.FromDays([DayOfWeek.Sunday, DayOfWeek.Friday, DayOfWeek.Monday, DayOfWeek.Friday]);
        var equivalent = OperatingDays.FromDays([DayOfWeek.Monday, DayOfWeek.Friday, DayOfWeek.Sunday]);

        Assert.Equal(81, days.Value);
        Assert.Equal("Monday, Friday, Sunday", days.Name);
        Assert.Equal(days.Name, days.ToString());
        Assert.Equal(equivalent, days);
        Assert.True(days == equivalent);
        Assert.False(days != equivalent);
        Assert.Equal(equivalent.GetHashCode(), days.GetHashCode());
        Assert.False(days == OperatingDays.None);
        Assert.False(days.Equals(null));
    }

    [Fact]
    public void FromValue_AllValidMasks_RoundTripThroughDayOfWeekWithoutChangingStorage()
    {
        Assert.Same(OperatingDays.None, OperatingDays.FromValue(0));
        Assert.Equal("None", OperatingDays.None.Name);
        Assert.All(Enum.GetValues<DayOfWeek>(), day => Assert.False(OperatingDays.None.HasDay(day)));

        for (var mask = 1; mask <= 127; mask++)
        {
            var stored = OperatingDays.FromValue(mask);
            var reconstructed = OperatingDays.FromDays(Enum.GetValues<DayOfWeek>().Where(stored.HasDay));

            Assert.Equal(mask, reconstructed.Value);
            Assert.Equal(stored, reconstructed);
        }
    }

    [Fact]
    public void FromDays_NullOrEmpty_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => OperatingDays.FromDays(null!));
        Assert.Throws<ArgumentException>(() => OperatingDays.FromDays([]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(100)]
    public void FromDaysAndHasDay_UndefinedDay_Throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OperatingDays.FromDays([(DayOfWeek)value]));
        Assert.Throws<ArgumentOutOfRangeException>(() => OperatingDays.None.HasDay((DayOfWeek)value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(128)]
    [InlineData(255)]
    public void FromValue_InvalidMask_Throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OperatingDays.FromValue(value));
    }
}
