using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using seating_service.Domain.ValueObjects;

namespace seating_service.Infrastructure.Persistence.Converters;

public sealed class HeldSelectionsConverter()
    : ValueConverter<HeldSelections, string>(
        selections => JsonSerializer.Serialize(selections.Items, JsonSerializerOptions.Default),
        json => Restore(json))
{
    public static ValueComparer<HeldSelections> Comparer { get; } = new(
        (left, right) => left == null ? right == null : right != null && left.Items.SequenceEqual(right.Items),
        selections => selections.Items.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
        selections => selections);

    private static HeldSelections Restore(string json)
    {
        var items = JsonSerializer.Deserialize<HeldSelection[]>(json, JsonSerializerOptions.Default);

        return HeldSelections.From(items ?? []);
    }
}
