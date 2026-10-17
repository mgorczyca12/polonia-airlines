namespace flight_service.Domain.ValueObjects;

public sealed class OperatingDays : IEquatable<OperatingDays>
{
    public static readonly OperatingDays None = new(0);

    private static readonly DayOfWeek[] All =
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

    private OperatingDays(int value)
    {
        Value = value;
    }

    public string Name => Value == 0 ? "None" : string.Join(", ", All.Where(HasDay));
    public int Value { get; }

    public bool HasDay(DayOfWeek day)
    {
        return (Value & GetMask(day)) != 0;
    }

    public static OperatingDays FromDays(IEnumerable<DayOfWeek> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        var value = 0;
        foreach (var day in days)
        {
            value |= GetMask(day);
        }

        if (value == 0)
        {
            throw new ArgumentException("At least one operating day is required.", nameof(days));
        }

        return new OperatingDays(value);
    }

    public static bool operator ==(OperatingDays? left, OperatingDays? right)
    {
        return ReferenceEquals(left, right) || left is not null && left.Equals(right);
    }

    public static bool operator !=(OperatingDays? left, OperatingDays? right)
    {
        return !(left == right);
    }

    public static OperatingDays FromValue(int value)
    {
        if (value < 0 || value > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Operating days must use only the seven day bits.");
        }

        return value == 0 ? None : new OperatingDays(value);
    }

    private static int GetMask(DayOfWeek day)
    {
        if (!Enum.IsDefined(day))
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, "A valid day of the week is required.");
        }

        // Preserve the persisted layout: Monday is bit 0 and Sunday is bit 6.
        return 1 << (((int)day + 6) % 7);
    }

    public bool Equals(OperatingDays? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as OperatingDays);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public override string ToString()
    {
        return Name;
    }
}
