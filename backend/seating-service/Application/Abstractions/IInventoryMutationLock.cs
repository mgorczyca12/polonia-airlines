namespace seating_service.Application.Abstractions;

public interface IInventoryMutationLock
{
    Task AcquireAsync(CancellationToken cancellationToken);
}
