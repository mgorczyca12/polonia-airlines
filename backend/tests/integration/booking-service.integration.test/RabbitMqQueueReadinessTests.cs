using System.Text.Json;

namespace booking_service.integration.test;

public sealed class RabbitMqQueueReadinessTests
{
    [Theory]
    [InlineData("[]", false)]
    [InlineData("""[{"name":"HoldInventory"},{"name":"ConfirmInventory","consumers":1}]""", false)]
    [InlineData("""[{"name":"HoldInventory","consumers":1},{"name":"ConfirmInventory"}]""", false)]
    [InlineData("""[{"name":"HoldInventory","consumers":0},{"name":"ConfirmInventory","consumers":1}]""", false)]
    [InlineData("""[{"name":"HoldInventory","consumers":1}]""", false)]
    [InlineData("""[{"name":"HoldInventory","consumers":1},{"name":"ConfirmInventory","consumers":1}]""", true)]
    public void HasSeatingConsumers_QueueMetrics_RequiresBothActiveConsumers(string json, bool expected)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Equal(expected,
            ReservationBrokerWorkflowTests.Fixture.HasSeatingConsumers(document.RootElement));
    }
}
