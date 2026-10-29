namespace seating_service.Application.Features.InventoryFeatures.Errors;

public enum InventoryRejectionCode
{
    InvalidSelectionOrDeadline,
    InventoryNotProvisioned,
    InventoryUnavailable,
    HoldExpiredOrLost,
    HoldNotFound,
    Cancelled
}
