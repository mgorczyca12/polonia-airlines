using DispatchR.Abstractions.Send;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions;
using Shared.Application.Commands;

namespace Shared.Application.Pipelines;

public sealed class TransactionBehavior<TCommand, TResponse>(IServiceProvider services)
    : IPipelineBehavior<TCommand, ValueTask<TResponse>>
    where TCommand : class, IRequest<TCommand, ValueTask<TResponse>>
{
    public required IRequestHandler<TCommand, ValueTask<TResponse>> NextPipeline { get; set; }

    public async ValueTask<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        if (command is not ITransactionalCommand)
        {
            return await NextPipeline.Handle(command, cancellationToken);
        }

        var state = services.GetRequiredService<CommandExecutionState>();
        if (state.TransactionActive)
        {
            throw new InvalidOperationException("Command transactions cannot be nested.");
        }

        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        state.TransactionActive = true;
        state.CommitAttempted = false;
        state.RollbackFailed = false;

        TResponse response;
        try
        {
            response = await NextPipeline.Handle(command, cancellationToken);

            if (response is not IResultBase { IsFailed: true })
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                state.CommitAttempted = true;
                await unitOfWork.CommitTransactionAsync(cancellationToken);
            }
        }
        catch (Exception exception)
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                state.RollbackFailed = true;
                throw new AggregateException(
                    "The command failed and its transaction could not be rolled back.",
                    exception,
                    rollbackException);
            }

            throw;
        }
        finally
        {
            state.TransactionActive = false;
        }

        if (response is IResultBase { IsFailed: true })
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            catch
            {
                state.RollbackFailed = true;
                throw;
            }
        }

        return response;
    }
}
