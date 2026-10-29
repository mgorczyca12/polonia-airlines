namespace seating_service.Application.Features.Inventory;

public enum InventoryRejectionCode
{
    InvalidSelectionOrDeadline,
    InventoryNotProvisioned,
    InventoryUnavailable,
    HoldExpiredOrLost,
    HoldNotFound,
    Cancelled
}

public static class InventoryRejectionCodes
{
    public static string ToContractCode(this InventoryRejectionCode code)
    {
        return code switch
        {
            InventoryRejectionCode.InvalidSelectionOrDeadline => "invalid_selection_or_deadline",
            InventoryRejectionCode.InventoryNotProvisioned => "inventory_not_provisioned",
            InventoryRejectionCode.InventoryUnavailable => "inventory_unavailable",
            InventoryRejectionCode.HoldExpiredOrLost => "hold_expired_or_lost",
            InventoryRejectionCode.HoldNotFound => "hold_not_found",
            InventoryRejectionCode.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
