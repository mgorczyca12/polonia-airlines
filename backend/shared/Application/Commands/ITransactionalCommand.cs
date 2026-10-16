namespace Shared.Application.Commands;

public interface ITransactionalCommand
{
}

public interface ITransactionalCommand<TCommand, TResponse>
    : ICommand<TCommand, TResponse>, ITransactionalCommand
    where TCommand : class, ITransactionalCommand<TCommand, TResponse>
{
}
