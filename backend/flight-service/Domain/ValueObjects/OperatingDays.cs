using System.Collections.Generic;
using System.Linq;

namespace flight_service.Domain.ValueObjects;

public sealed class OperatingDays : IEquatable<OperatingDays>
{
    public static readonly OperatingDays None = new("None", 0);
    public static readonly OperatingDays Monday = new("Monday", 1 << 0);
    public static readonly OperatingDays Tuesday = new("Tuesday", 1 << 1);
    public static readonly OperatingDays Wednesday = new("Wednesday", 1 << 2);
    public static readonly OperatingDays Thursday = new("Thursday", 1 << 3);
    public static readonly OperatingDays Friday = new("Friday", 1 << 4);
    public static readonly OperatingDays Saturday = new("Saturday", 1 << 5);
    public static readonly OperatingDays Sunday = new("Sunday", 1 << 6);

    private static readonly OperatingDays[] All =
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    };

    private OperatingDays(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public int Value { get; }

    public bool HasDay(OperatingDays day) => (Value & day.Value) == day.Value;

    public static OperatingDays operator |(OperatingDays left, OperatingDays right)
    {
        var value = left.Value | right.Value;
        return new OperatingDays(string.Join(", ", GetNames(value)), value);
    }

    public static bool operator ==(OperatingDays? left, OperatingDays? right) =>
        ReferenceEquals(left, right) || left is not null && left.Equals(right);

    public static bool operator !=(OperatingDays? left, OperatingDays? right) => !(left == right);

    public static OperatingDays FromValue(int value) =>
        value == 0
            ? None
            : new OperatingDays(string.Join(", ", GetNames(value)), value);

    private static IEnumerable<string> GetNames(int value) =>
        All.Where(day => (value & day.Value) == day.Value).Select(day => day.Name);

    public bool Equals(OperatingDays? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as OperatingDays);

    public override int GetHashCode() => Value;

    public override string ToString() => Name;
}