using DispatchR.Abstractions.Send;
using FluentResults;
using Shared.Application.Abstractions;
using Shared.Application.Commands;

namespace Shared.Application.Pipelines;

public sealed class TransactionBehavior<TCommand, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TCommand, ValueTask<TResponse>>
    where TCommand : class, ICommand<TCommand, TResponse>
{
    public required IRequestHandler<TCommand, ValueTask<TResponse>> NextPipeline { get; set; }

    public async ValueTask<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await NextPipeline.Handle(command, cancellationToken);

            if (response is IResultBase { IsFailed: true })
            {
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                return response;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }
}