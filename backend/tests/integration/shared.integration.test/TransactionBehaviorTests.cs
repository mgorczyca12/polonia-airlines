using DispatchR.Abstractions.Send;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions;
using Shared.Application.Commands;
using Shared.Application.DependencyInjection;
using Shared.Application.Pipelines;

namespace Shared.IntegrationTests;

public sealed class TransactionBehaviorTests
{
    [Theory]
    [InlineData("handler")]
    [InlineData("save")]
    [InlineData("commit")]
    public async Task Handle_HandlerSaveOrCommitFailure_RollsBackOnceWithoutRetrying(string failurePoint)
    {
        var failure = new InvalidOperationException(failurePoint);
        var unitOfWork = new RecordingUnitOfWork
        {
            SaveFailure = failurePoint == "save" ? failure : null,
            CommitFailure = failurePoint == "commit" ? failure : null
        };
        var handler = new TestHandler
        {
            Failure = failurePoint == "handler" ? failure : null
        };
        using var services = CreateServices(unitOfWork);
        var behavior = new TransactionBehavior<TestCommand, Result>(services)
        {
            NextPipeline = handler
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.Handle(new TestCommand(), CancellationToken.None));

        Assert.Same(failure, exception);
        Assert.Equal(1, handler.Calls);
        Assert.Equal(1, unitOfWork.Rollbacks);
        Assert.Equal(CancellationToken.None, unitOfWork.RollbackToken);
    }

    [Fact]
    public async Task Handle_HandlerCancellation_PropagatesCancellationAndRollsBackWithUncancelledToken()
    {
        using var cancellation = new CancellationTokenSource();
        var failure = new OperationCanceledException(cancellation.Token);
        var handler = new TestHandler
        {
            Failure = failure,
            BeforeFailure = cancellation.Cancel
        };
        var unitOfWork = new RecordingUnitOfWork();
        using var services = CreateServices(unitOfWork);
        var behavior = new TransactionBehavior<TestCommand, Result>(services)
        {
            NextPipeline = handler
        };

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await behavior.Handle(new TestCommand(), cancellation.Token));

        Assert.Same(failure, exception);
        Assert.Equal(cancellation.Token, handler.Token);
        Assert.Equal(1, unitOfWork.Rollbacks);
        Assert.Equal(CancellationToken.None, unitOfWork.RollbackToken);
    }

    [Fact]
    public async Task Handle_ExecutionAndRollbackFailures_PreservesBothExceptions()
    {
        var failure = new InvalidOperationException("Handler failed.");
        var rollbackFailure = new InvalidOperationException("Rollback failed.");
        var unitOfWork = new RecordingUnitOfWork { RollbackFailure = rollbackFailure };
        using var services = CreateServices(unitOfWork);
        var behavior = new TransactionBehavior<TestCommand, Result>(services)
        {
            NextPipeline = new TestHandler { Failure = failure }
        };

        var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            await behavior.Handle(new TestCommand(), CancellationToken.None));

        Assert.Equal(new[] { failure, rollbackFailure }, exception.InnerExceptions);
        Assert.Equal(1, unitOfWork.Rollbacks);
    }

    [Fact]
    public async Task Handle_FailedResultAndRollbackFailure_PropagatesRollbackExceptionWithoutRetrying()
    {
        var rollbackFailure = new InvalidOperationException("Rollback failed.");
        var unitOfWork = new RecordingUnitOfWork { RollbackFailure = rollbackFailure };
        using var services = CreateServices(unitOfWork);
        var behavior = new TransactionBehavior<TestCommand, Result>(services)
        {
            NextPipeline = new TestHandler { Response = Result.Fail("Rejected.") }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.Handle(new TestCommand(), CancellationToken.None));

        Assert.Same(rollbackFailure, exception);
        Assert.Equal(1, unitOfWork.Rollbacks);
    }

    private static ServiceProvider CreateServices(IUnitOfWork unitOfWork)
    {
        var services = new ServiceCollection();
        services.AddSharedApplication(typeof(TransactionBehaviorTests).Assembly);
        services.AddSingleton(unitOfWork);
        return services.BuildServiceProvider();
    }

    public sealed record TestCommand : ITransactionalCommand<TestCommand, Result>;

    public sealed class TestHandler : IRequestHandler<TestCommand, ValueTask<Result>>
    {
        public Exception? Failure { get; init; }
        public Action? BeforeFailure { get; init; }
        public Result Response { get; init; } = Result.Ok();
        public int Calls { get; private set; }
        public CancellationToken Token { get; private set; }

        public ValueTask<Result> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            Calls++;
            Token = cancellationToken;
            BeforeFailure?.Invoke();
            if (Failure is not null)
            {
                throw Failure;
            }

            return ValueTask.FromResult(Response);
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public Exception? SaveFailure { get; init; }
        public Exception? CommitFailure { get; init; }
        public Exception? RollbackFailure { get; init; }
        public int Rollbacks { get; private set; }
        public CancellationToken RollbackToken { get; private set; }

        public Task BeginTransactionAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (SaveFailure is not null)
            {
                throw SaveFailure;
            }

            return Task.FromResult(1);
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken)
        {
            if (CommitFailure is not null)
            {
                throw CommitFailure;
            }

            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken)
        {
            Rollbacks++;
            RollbackToken = cancellationToken;
            if (RollbackFailure is not null)
            {
                throw RollbackFailure;
            }

            return Task.CompletedTask;
        }
    }
}
