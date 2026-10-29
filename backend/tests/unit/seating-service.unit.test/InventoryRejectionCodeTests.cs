using seating_service.Application.Features.InventoryFeatures.Errors;

namespace seating_service.unit.test;

public class InventoryRejectionCodeTests
{
    [Theory]
    [InlineData(InventoryRejectionCode.InvalidSelectionOrDeadline, "invalid_selection_or_deadline")]
    [InlineData(InventoryRejectionCode.InventoryNotProvisioned, "inventory_not_provisioned")]
    [InlineData(InventoryRejectionCode.InventoryUnavailable, "inventory_unavailable")]
    [InlineData(InventoryRejectionCode.HoldExpiredOrLost, "hold_expired_or_lost")]
    [InlineData(InventoryRejectionCode.HoldNotFound, "hold_not_found")]
    [InlineData(InventoryRejectionCode.Cancelled, "cancelled")]
    public void ToContractCode_MapsToStableContractStrings(InventoryRejectionCode code, string expected)
    {
        Assert.Equal(expected, code.ToContractCode());
    }

    [Fact]
    public void ToContractCode_UnknownValueThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((InventoryRejectionCode)999).ToContractCode());
    }
}
