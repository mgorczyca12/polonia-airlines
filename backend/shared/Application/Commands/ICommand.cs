using DispatchR.Abstractions.Send;

namespace Shared.Application.Commands;

public interface ICommand<TCommand, TResponse> : IRequest<TCommand, ValueTask<TResponse>>
    where TCommand : class, ICommand<TCommand, TResponse>
{
}