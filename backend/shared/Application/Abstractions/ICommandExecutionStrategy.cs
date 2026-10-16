namespace Shared.Application.Abstractions;

public interface ICommandExecutionStrategy
{
    Task<TResponse> ExecuteAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken);
}
