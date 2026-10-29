namespace seating_service.Application.Abstractions;

public interface IInventoryReleaseFence
{
    Task EnsureReleased(Guid correlationId, CancellationToken cancellationToken);
}
