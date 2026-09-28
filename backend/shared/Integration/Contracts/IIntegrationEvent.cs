namespace Shared.Integration.Contracts;

public interface IIntegrationEvent
{
    DateTimeOffset OccurredAt { get; }
}