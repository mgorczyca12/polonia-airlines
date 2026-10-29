namespace seating_service.Domain.ValueObjects;

public sealed record InventoryOperationState
{
    public static readonly InventoryOperationState Held = new("Held");
    public static readonly InventoryOperationState Confirmed = new("Confirmed");
    public static readonly InventoryOperationState Rejected = new("Rejected");
    public static readonly InventoryOperationState Released = new("Released");

    public string Code { get; }

    public static InventoryOperationState From(string code)
    {
        return code switch
        {
            "Held" => Held,
            "Confirmed" => Confirmed,
            "Rejected" => Rejected,
            "Released" => Released,
            _ => throw new ArgumentException("Unknown inventory operation state.", nameof(code))
        };
    }

    private InventoryOperationState(string code)
    {
        Code = code;
    }
}
