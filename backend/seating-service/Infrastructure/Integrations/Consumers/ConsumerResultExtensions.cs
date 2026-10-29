using FluentResults;

namespace seating_service.Infrastructure.Integrations.Consumers;

internal static class ConsumerResultExtensions
{
    public static void ThrowIfFailed(this Result result)
    {
        if (result.IsFailed)
        {
            throw new InvalidOperationException(
                "Inventory command failed: " + string.Join("; ", result.Errors.Select(error => error.Message)));
        }
    }
}
