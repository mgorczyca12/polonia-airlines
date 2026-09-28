namespace Shared.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}