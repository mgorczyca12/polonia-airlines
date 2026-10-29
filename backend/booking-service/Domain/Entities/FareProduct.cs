using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

/// <summary>A sellable fare product: cabin class and brand with its baggage terms. New terms need a new version.</summary>
public sealed class FareProduct : AggregateRoot<Guid>
{
    public string Code { get; private set; } = null!;
    public int Version { get; private set; }
    public string CabinClass { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public bool Active { get; private set; }
    public BaggageAllowance BaggageAllowance { get; private set; } = null!;

    private FareProduct()
    {
    }

    private FareProduct(Guid id) : base(id)
    {
    }

    public static FareProduct Create(
        string code,
        int version,
        string cabinClass,
        string brand,
        BaggageAllowance baggageAllowance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinClass);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentNullException.ThrowIfNull(baggageAllowance);

        return new FareProduct(Guid.NewGuid())
        {
            Code = code,
            Version = version,
            CabinClass = cabinClass,
            Brand = brand,
            Active = true,
            BaggageAllowance = BaggageAllowance.Create(
                baggageAllowance.IncludedPieces,
                baggageAllowance.MaxWeightPerPieceKg,
                baggageAllowance.MaxExtraPieces,
                baggageAllowance.ExtraPiecePrice)
        };
    }

    public void Retire()
    {
        Active = false;
    }
}
