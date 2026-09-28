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

    private static readonly SeatMapRowType[] All = { Seats, ExitRow, Bulkhead, Galley, Lavatory };

    public int Value { get; }

    private SeatMapRowType(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public bool HasFlag(SeatMapRowType flag) => (Value & flag.Value) == flag.Value;

    public static SeatMapRowType operator |(SeatMapRowType left, SeatMapRowType right) =>
        new SeatMapRowType(string.Join(", ", CombinedNames(left.Value | right.Value)), left.Value | right.Value);

    private static IEnumerable<string> CombinedNames(int value) =>
        All.Where(t => (value & t.Value) == t.Value).Select(t => t.Name);

    public static SeatMapRowType FromValue(int value) =>
        new SeatMapRowType(string.Join(", ", CombinedNames(value)), value);

    public override string ToString() => Name;

    public bool Equals(SeatMapRowType? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as SeatMapRowType);

    public override int GetHashCode() => Value;
}