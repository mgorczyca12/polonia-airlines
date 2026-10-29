namespace seating_service.Domain.ValueObjects;

public sealed class HeldSelections
{
    public static readonly HeldSelections Empty = new([]);

    public IReadOnlyList<HeldSelection> Items { get; }

    public static HeldSelections From(IEnumerable<HeldSelection> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);

        return new HeldSelections(selections.ToArray());
    }

    private HeldSelections(HeldSelection[] items)
    {
        Items = items;
    }
}
