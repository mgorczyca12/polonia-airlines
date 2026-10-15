using System;
using System.Collections.Generic;
using System.Linq;

namespace flight_service.Domain.ValueObjects;

// Bitwise-backed so instances can be combined, e.g. a 737 door-1 row that is both Galley and ExitRow.
public sealed class SeatMapRowType : IEquatable<SeatMapRowType>
{
    public static readonly SeatMapRowType Seats = new SeatMapRowType("Seats", 1 << 0);
    public static readonly SeatMapRowType ExitRow = new SeatMapRowType("ExitRow", 1 << 1);
    public static readonly SeatMapRowType Bulkhead = new SeatMapRowType("Bulkhead", 1 << 2);
    public static readonly SeatMapRowType Galley = new SeatMapRowType("Galley", 1 << 3);
    public static readonly SeatMapRowType Lavatory = new SeatMapRowType("Lavatory", 1 << 4);

    private static readonly SeatMapRowType[] All =
    {
        Seats,
        ExitRow,
        Bulkhead,
        Galley,
        Lavatory
    };

    public int Value { get; }

    private SeatMapRowType(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public bool HasFlag(SeatMapRowType flag)
    {
        return (Value & flag.Value) == flag.Value;
    }

    public static SeatMapRowType operator |(SeatMapRowType left, SeatMapRowType right)
    {
        var value = left.Value | right.Value;
        var name = string.Join(", ", CombinedNames(value));

        return new SeatMapRowType(name, value);
    }

    private static IEnumerable<string> CombinedNames(int value)
    {
        return All
            .Where(rowType => (value & rowType.Value) == rowType.Value)
            .Select(rowType => rowType.Name);
    }

    public static SeatMapRowType FromValue(int value)
    {
        return new SeatMapRowType(string.Join(", ", CombinedNames(value)), value);
    }

    public override string ToString()
    {
        return Name;
    }

    public bool Equals(SeatMapRowType? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SeatMapRowType);
    }

    public override int GetHashCode()
    {
        return Value;
    }
}
