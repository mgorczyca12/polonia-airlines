using System.Runtime.ExceptionServices;
using DispatchR;
using DispatchR.Abstractions.Send;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions;
using Shared.Application.Commands;

namespace Shared.Application.Pipelines;

public sealed class ExecutionStrategyBehavior<TCommand, TResponse>(IServiceProvider services)
    : IPipelineBehavior<TCommand, ValueTask<TResponse>>
    where TCommand : class, IRequest<TCommand, ValueTask<TResponse>>
{
    public required IRequestHandler<TCommand, ValueTask<TResponse>> NextPipeline { get; set; }

    public async ValueTask<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        if (command is not IRetryableCommand)
        {
            return await NextPipeline.Handle(command, cancellationToken);
        }

        var state = services.GetRequiredService<CommandExecutionState>();
        if (state.TransactionActive)
        {
            throw new InvalidOperationException("A retryable command cannot run inside an active command transaction.");
        }

        if (state.RetryCommand is not null)
        {
            if (!ReferenceEquals(state.RetryCommand, command))
            {
                throw new InvalidOperationException("Retryable commands cannot be nested.");
            }

            return await NextPipeline.Handle(command, cancellationToken);
        }

        var strategy = services.GetRequiredService<ICommandExecutionStrategy>();
        var outcome = await strategy.ExecuteAsync(
            token => ExecuteAttempt(command, token),
            cancellationToken);

        return outcome.GetResponse();
    }

    private async Task<AttemptOutcome> ExecuteAttempt(TCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = services.CreateAsyncScope();
        var state = scope.ServiceProvider.GetRequiredService<CommandExecutionState>();
        state.RetryCommand = command;

        try
        {
            await using (scope)
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var response = await mediator.Send(command, cancellationToken);
                return new AttemptOutcome(response, null);
            }
        }
        catch (Exception exception) when (
            state.CommitAttempted
            || state.RollbackFailed
            || exception is OperationCanceledException or ValidationException)
        {
            // Return unsafe-to-replay failures through the strategy, then rethrow outside its retry loop.
            return new AttemptOutcome(default, ExceptionDispatchInfo.Capture(exception));
        }
    }

    private sealed record AttemptOutcome(TResponse? Response, ExceptionDispatchInfo? Failure)
    {
        public TResponse GetResponse()
        {
            Failure?.Throw();
            return Response!;
        }
    }
}
