using DispatchR;
using DispatchR.Abstractions.Send;
using FluentResults;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions;
using Shared.Application.Commands;
using Shared.Application.Events;
using Shared.Application.DependencyInjection;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.Persistence;

namespace Shared.IntegrationTests;

public sealed class CommandTransactionTests
{
    [Fact]
    public async Task Send_OrdinaryCommand_SucceedsWithoutUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddSharedApplication(typeof(CommandTransactionTests).Assembly);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new OrdinaryCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(scope.ServiceProvider.GetService<IUnitOfWork>());
    }

    [Theory]
    [InlineData("success", 1)]
    [InlineData("failure", 0)]
    [InlineData("exception", 0)]
    public async Task Send_TransactionalCommand_CommitsOnlyOnSuccess(string outcome, int expectedCount)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = CreateServices(connection);
        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await context.Database.EnsureCreatedAsync();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var command = new WriteCommand("flight", outcome);

            if (outcome == "exception")
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await mediator.Send(command, CancellationToken.None));
            }
            else
            {
                var result = await mediator.Send(command, CancellationToken.None);
                Assert.Equal(outcome == "success", result.IsSuccess);
            }

            Assert.Null(context.Database.CurrentTransaction);
        }

        await using var readScope = provider.CreateAsyncScope();
        var readContext = readScope.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.Equal(expectedCount, await readContext.Records.CountAsync());
    }

    [Fact]
    public async Task Send_TransactionalOnlyCommand_DoesNotInvokeExecutionStrategy()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = CreateServices(connection);
        services.AddScoped<ICommandExecutionStrategy, UnexpectedExecutionStrategy>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

        var result = await scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new WriteCommand("flight", "success"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<TestDbContext>().Records.CountAsync());
    }

    [Fact]
    public async Task Send_InvalidTransactionalCommand_ThrowsValidationExceptionBeforeStartingTransaction()
    {
        var services = new ServiceCollection();
        services.AddSharedApplication(typeof(CommandTransactionTests).Assembly);
        services.AddSingleton<IUnitOfWork, UnexpectedUnitOfWork>();
        services.AddDbContext<TestDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await mediator.Send(new WriteCommand("", "success"), CancellationToken.None));
    }

    [Fact]
    public async Task AddSharedUnitOfWork_ScopedContext_SharesContextAndReleasesCompletedTransactions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var provider = CreateServices(connection).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.IsType<EfUnitOfWork<TestDbContext>>(unitOfWork);
        Assert.Same(unitOfWork, scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        await context.Database.EnsureCreatedAsync();

        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        Assert.NotNull(context.Database.CurrentTransaction);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.BeginTransactionAsync(CancellationToken.None));

        context.Records.Add(new TestRecord { Name = "committed" });
        Assert.Equal(1, await unitOfWork.SaveChangesAsync(CancellationToken.None));
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);
        Assert.Null(context.Database.CurrentTransaction);

        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        context.Records.Add(new TestRecord { Name = "rolled back" });
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        Assert.Null(context.Database.CurrentTransaction);
        await unitOfWork.RollbackTransactionAsync(CancellationToken.None);

        context.ChangeTracker.Clear();
        Assert.Equal("committed", (await context.Records.SingleAsync()).Name);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.CommitTransactionAsync(CancellationToken.None));
    }

    [Fact]
    public async Task BeginTransactionAsync_ExistingExternalTransaction_ThrowsWithoutTakingOwnership()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new TestDbContext(
            new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var unitOfWork = new EfUnitOfWork<TestDbContext>(context, new DomainEventDispatcher([]));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.BeginTransactionAsync(CancellationToken.None));
        await unitOfWork.RollbackTransactionAsync(CancellationToken.None);

        Assert.Same(transaction, context.Database.CurrentTransaction);
    }

    private static ServiceCollection CreateServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddSharedApplication(typeof(CommandTransactionTests).Assembly);
        services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection));
        services.AddSharedUnitOfWork<TestDbContext>();
        return services;
    }

    public sealed record OrdinaryCommand : ICommand<OrdinaryCommand, Result>;

    public sealed class OrdinaryCommandHandler : IRequestHandler<OrdinaryCommand, ValueTask<Result>>
    {
        public ValueTask<Result> Handle(OrdinaryCommand request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(Result.Ok());
        }
    }

    public sealed record WriteCommand(string Name, string Outcome)
        : ITransactionalCommand<WriteCommand, Result>;

    public sealed class WriteCommandValidator : AbstractValidator<WriteCommand>
    {
        public WriteCommandValidator()
        {
            RuleFor(command => command.Name).NotEmpty();
        }
    }

    public sealed class WriteCommandHandler(TestDbContext context)
        : IRequestHandler<WriteCommand, ValueTask<Result>>
    {
        public async ValueTask<Result> Handle(WriteCommand request, CancellationToken cancellationToken)
        {
            Assert.NotNull(context.Database.CurrentTransaction);
            context.Records.Add(new TestRecord { Name = request.Name });

            // Flush before failing to verify database rollback, not just discarded tracking.
            if (request.Outcome != "success")
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            if (request.Outcome == "exception")
            {
                throw new InvalidOperationException("Handler failed.");
            }

            return request.Outcome == "failure" ? Result.Fail("Rejected.") : Result.Ok();
        }
    }

    public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestRecord> Records => Set<TestRecord>();
    }

    public sealed class TestRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class UnexpectedExecutionStrategy : ICommandExecutionStrategy
    {
        public Task<TResponse> ExecuteAsync<TResponse>(
            Func<CancellationToken, Task<TResponse>> operation,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Transactional-only commands should not use an execution strategy.");
        }
    }

    private sealed class UnexpectedUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Validation should prevent a transaction.");
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Validation should prevent saving.");
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Validation should prevent committing.");
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Validation should prevent rollback.");
        }
    }
}
