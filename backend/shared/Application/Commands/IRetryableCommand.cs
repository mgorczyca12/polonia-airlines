namespace Shared.Application.Commands;

public interface IRetryableCommand : ITransactionalCommand
{
}

public interface IRetryableCommand<TCommand, TResponse>
    : ITransactionalCommand<TCommand, TResponse>, IRetryableCommand
    where TCommand : class, IRetryableCommand<TCommand, TResponse>
{
}
