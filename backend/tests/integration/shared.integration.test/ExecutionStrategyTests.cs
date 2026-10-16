using System.Data.Common;
using DispatchR;
using DispatchR.Abstractions.Send;
using FluentResults;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Commands;
using Shared.Application.DependencyInjection;
using Shared.Infrastructure.DependencyInjection;

namespace Shared.IntegrationTests;

public sealed class ExecutionStrategyTests
{
    [Theory]
    [InlineData("handler-transient")]
    [InlineData("save-transient")]
    public async Task Send_TransientFailure_RetriesWholeTransactionWithFreshHandlerAndContext(string outcome)
    {
        await using var harness = await Harness.Create();

        var result = await harness.Send(new RetryCommand("flight", outcome));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, harness.Probe.Contexts.Count);
        Assert.Equal(2, harness.Probe.HandlerIds.Distinct().Count());
        Assert.Equal(2, harness.Probe.Contexts.Select(context => context.ContextId.InstanceId).Distinct().Count());
        Assert.All(harness.Probe.InitialTrackedCounts, count => Assert.Equal(0, count));
        Assert.All(harness.Probe.Contexts, context =>
            Assert.Throws<ObjectDisposedException>(() => context.ChangeTracker.Entries().ToList()));
        Assert.Equal(1, await harness.CountRecords());
        Assert.Equal(1, harness.Probe.RetryChecks);
    }

    [Theory]
    [InlineData("failure")]
    [InlineData("non-transient")]
    public async Task Send_BusinessFailureOrNonTransientException_DoesNotRetry(string outcome)
    {
        await using var harness = await Harness.Create();

        if (outcome == "failure")
        {
            var result = await harness.Send(new RetryCommand("flight", outcome));
            Assert.True(result.IsFailed);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                harness.Send(new RetryCommand("flight", outcome)));
        }

        Assert.Single(harness.Probe.Contexts);
        Assert.Equal(0, await harness.CountRecords());
    }

    [Fact]
    public async Task Send_TransientBeginTransactionFailure_RetriesBeforeInvokingHandler()
    {
        await using var harness = await Harness.Create();
        harness.Probe.FailBegin = true;

        var result = await harness.Send(new RetryCommand("flight", "success"));

        Assert.True(result.IsSuccess);
        Assert.Single(harness.Probe.Contexts);
        Assert.Equal(1, harness.Probe.RetryChecks);
        Assert.Equal(1, await harness.CountRecords());
    }

    [Fact]
    public async Task Send_TransientFailuresExhaustRetryLimit_ThrowsRetryLimitExceededException()
    {
        await using var harness = await Harness.Create();

        await Assert.ThrowsAsync<RetryLimitExceededException>(() =>
            harness.Send(new RetryCommand("flight", "always-transient")));

        Assert.Equal(3, harness.Probe.Contexts.Count);
        Assert.Equal(0, await harness.CountRecords());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Send_CommitFailure_DoesNotReplayPotentiallyCommittedCommand(bool commitCompleted)
    {
        await using var harness = await Harness.Create();
        harness.Probe.FailBeforeCommit = !commitCompleted;
        harness.Probe.FailAfterCommit = commitCompleted;

        var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            harness.Send(new RetryCommand("flight", "success")));

        Assert.Contains("commit", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Single(harness.Probe.Contexts);
        Assert.Equal(0, harness.Probe.RetryChecks);
        Assert.Equal(commitCompleted ? 1 : 0, await harness.CountRecords());
    }

    [Theory]
    [InlineData("handler-transient")]
    [InlineData("failure")]
    public async Task Send_RollbackFailure_SurfacesFailureWithoutRetrying(string outcome)
    {
        await using var harness = await Harness.Create();
        harness.Probe.FailRollback = true;

        var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            harness.Send(new RetryCommand("flight", outcome)));

        Assert.Contains("rollback", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Single(harness.Probe.Contexts);
        Assert.Equal(0, harness.Probe.RetryChecks);
        Assert.Equal(0, await harness.CountRecords());
    }

    [Fact]
    public async Task Send_CancelledHandler_PropagatesCancellationWithoutRetrying()
    {
        await using var harness = await Harness.Create();
        using var cancellation = new CancellationTokenSource();
        harness.Probe.Cancel = cancellation.Cancel;

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.Send(new RetryCommand("flight", "cancel"), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Single(harness.Probe.Contexts);
        Assert.Equal(0, harness.Probe.RetryChecks);
        Assert.Equal(0, await harness.CountRecords());
    }

    [Fact]
    public async Task Send_CancelledBeforeExecution_DoesNotInvokeHandler()
    {
        await using var harness = await Harness.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.Send(new RetryCommand("flight", "success"), cancellation.Token));

        Assert.Empty(harness.Probe.Contexts);
        Assert.Equal(0, await harness.CountRecords());
    }

    [Fact]
    public async Task Send_InvalidRetryableCommand_ValidatesBeforeExecutingHandler()
    {
        await using var harness = await Harness.Create();

        await Assert.ThrowsAsync<ValidationException>(() =>
            harness.Send(new RetryCommand("", "success")));

        Assert.Empty(harness.Probe.Contexts);
        Assert.Equal(0, harness.Probe.RetryChecks);
    }

    [Fact]
    public async Task Send_NonRetryingProviderStrategy_PropagatesTransientFailureWithoutRetrying()
    {
        await using var harness = await Harness.Create(retriesEnabled: false);

        await Assert.ThrowsAsync<TestTransientException>(() =>
            harness.Send(new RetryCommand("flight", "handler-transient")));

        Assert.Single(harness.Probe.Contexts);
        Assert.Equal(0, await harness.CountRecords());
    }

    [Fact]
    public async Task Send_NestedRetryableCommand_RejectsIndependentRetryBoundary()
    {
        await using var harness = await Harness.Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Send(new RetryCommand("flight", "nested")));

        Assert.Single(harness.Probe.Contexts);
        Assert.Equal(0, await harness.CountRecords());
    }

    [Fact]
    public async Task Send_RetryableCommandInsideTransactionalCommand_RejectsNewRetryBoundary()
    {
        await using var harness = await Harness.Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.SendOuterCommand());

        Assert.Empty(harness.Probe.Contexts);
        Assert.Equal(0, await harness.CountRecords());
    }

    public sealed record OuterCommand : ITransactionalCommand<OuterCommand, Result>;

    public sealed class OuterCommandHandler(IMediator mediator)
        : IRequestHandler<OuterCommand, ValueTask<Result>>
    {
        public async ValueTask<Result> Handle(OuterCommand request, CancellationToken cancellationToken)
        {
            return await mediator.Send(new RetryCommand("nested", "success"), cancellationToken);
        }
    }

    public sealed record RetryCommand(string Name, string Outcome)
        : IRetryableCommand<RetryCommand, Result>;

    public sealed class RetryCommandValidator : AbstractValidator<RetryCommand>
    {
        public RetryCommandValidator()
        {
            RuleFor(command => command.Name).NotEmpty();
        }
    }

    public sealed class RetryCommandHandler(
        RetryDbContext context,
        AttemptProbe probe,
        IServiceProvider services) : IRequestHandler<RetryCommand, ValueTask<Result>>
    {
        private readonly Guid id = Guid.NewGuid();

        public async ValueTask<Result> Handle(RetryCommand request, CancellationToken cancellationToken)
        {
            Assert.NotNull(context.Database.CurrentTransaction);
            probe.Contexts.Add(context);
            probe.HandlerIds.Add(id);
            probe.InitialTrackedCounts.Add(context.ChangeTracker.Entries().Count());
            context.Records.Add(new RetryRecord { Name = request.Name });

            if (request.Outcome == "nested")
            {
                return await services.GetRequiredService<IMediator>()
                    .Send(new RetryCommand("nested", "success"), cancellationToken);
            }

            probe.FailSave = request.Outcome == "save-transient" && probe.Contexts.Count == 1;
            if (request.Outcome is "success" or "save-transient")
            {
                return Result.Ok();
            }

            await context.SaveChangesAsync(cancellationToken);

            if (request.Outcome == "cancel")
            {
                probe.Cancel!();
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (request.Outcome == "non-transient")
            {
                throw new InvalidOperationException("Non-transient handler failure.");
            }

            if (request.Outcome == "always-transient"
                || request.Outcome == "handler-transient" && probe.Contexts.Count == 1)
            {
                throw new TestTransientException("Transient handler failure.");
            }

            return request.Outcome == "failure" ? Result.Fail("Business rejection.") : Result.Ok();
        }
    }

    public sealed class RetryDbContext(DbContextOptions<RetryDbContext> options, AttemptProbe probe)
        : DbContext(options)
    {
        public DbSet<RetryRecord> Records => Set<RetryRecord>();

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var count = await base.SaveChangesAsync(cancellationToken);
            if (probe.FailSave)
            {
                probe.FailSave = false;
                throw new TestTransientException("Transient save failure.");
            }

            return count;
        }
    }

    public sealed class RetryRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class AttemptProbe
    {
        public List<RetryDbContext> Contexts { get; } = [];
        public List<Guid> HandlerIds { get; } = [];
        public List<int> InitialTrackedCounts { get; } = [];
        public int RetryChecks { get; set; }
        public bool FailSave { get; set; }
        public bool FailBegin { get; set; }
        public bool FailBeforeCommit { get; set; }
        public bool FailAfterCommit { get; set; }
        public bool FailRollback { get; set; }
        public Action? Cancel { get; set; }
    }

    private sealed class TestTransientException(string message) : Exception(message);

    private sealed class TestExecutionStrategy(ExecutionStrategyDependencies dependencies, AttemptProbe probe)
        : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception)
        {
            if (exception is TestTransientException)
            {
                probe.RetryChecks++;
                return true;
            }

            return false;
        }
    }

    private sealed class FailureInterceptor(AttemptProbe probe) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            if (probe.FailBegin)
            {
                probe.FailBegin = false;
                throw new TestTransientException("Transient begin transaction failure.");
            }

            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (probe.FailBeforeCommit)
            {
                throw new TestTransientException("Transient commit failure.");
            }

            return ValueTask.FromResult(result);
        }

        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (probe.FailAfterCommit)
            {
                throw new TestTransientException("Lost commit acknowledgement.");
            }

            return Task.CompletedTask;
        }

        public override ValueTask<InterceptionResult> TransactionRollingBackAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(CancellationToken.None, cancellationToken);
            if (probe.FailRollback)
            {
                throw new TestTransientException("Transient rollback failure.");
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class Harness(SqliteConnection connection, ServiceProvider provider, AttemptProbe probe)
        : IAsyncDisposable
    {
        public AttemptProbe Probe => probe;

        public static async Task<Harness> Create(bool retriesEnabled = true)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var probe = new AttemptProbe();
            var services = new ServiceCollection();
            services.AddSingleton(probe);
            services.AddSharedApplication(typeof(ExecutionStrategyTests).Assembly);
            services.AddDbContext<RetryDbContext>(options =>
            {
                options.UseSqlite(connection, sqlite =>
                {
                    if (retriesEnabled)
                    {
                        sqlite.ExecutionStrategy(dependencies => new TestExecutionStrategy(dependencies, probe));
                    }
                });
                options.AddInterceptors(new FailureInterceptor(probe));
            });
            services.AddSharedUnitOfWork<RetryDbContext>();
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<RetryDbContext>().Database.EnsureCreatedAsync();
            return new Harness(connection, provider, probe);
        }

        public async Task<Result> Send(RetryCommand command, CancellationToken cancellationToken = default)
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command, cancellationToken);
        }

        public async Task<Result> SendOuterCommand()
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new OuterCommand(), CancellationToken.None);
        }

        public async Task<int> CountRecords()
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<RetryDbContext>().Records.CountAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
