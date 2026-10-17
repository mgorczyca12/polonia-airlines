using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using Shared.Application.Events;
using Shared.Infrastructure.Persistence;
using Shared.Infrastructure.Repositories;

namespace Shared.IntegrationTests;

public sealed class RepositoryBaseTests
{
    [Fact]
    public async Task AddAndAddRange_RegisterAggregatesWithoutSaving()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = CreateOptions(connection);
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new TestRepository(context);
        var first = new TestAggregate("first");
        var others = new[] { new TestAggregate("second"), new TestAggregate("third") };

        repository.Add(first);
        repository.AddRange(others);

        Assert.Equal(EntityState.Added, context.Entry(first).State);
        Assert.All(others, aggregate => Assert.Equal(EntityState.Added, context.Entry(aggregate).State));
        await using var verificationContext = new TestDbContext(options);
        Assert.Equal(0, await verificationContext.Aggregates.CountAsync());

        var unitOfWork = new EfUnitOfWork<TestDbContext>(context, new DomainEventDispatcher([]));
        Assert.Equal(3, await unitOfWork.SaveChangesAsync(CancellationToken.None));
        Assert.Equal(3, await verificationContext.Aggregates.CountAsync());
        Assert.True(first.Id > 0);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTrackedAggregateWhoseChangesAreSavedByUnitOfWork()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = CreateOptions(connection);
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var aggregate = new TestAggregate("original");
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new TestRepository(context);

        var loaded = await repository.GetByIdAsync(aggregate.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("original", loaded.Name);
        Assert.Equal(EntityState.Unchanged, context.Entry(loaded).State);
        Assert.Same(loaded, await repository.GetByIdAsync(aggregate.Id, CancellationToken.None));

        loaded.Rename("changed");
        await using var verificationContext = new TestDbContext(options);
        Assert.Equal("original", (await verificationContext.Aggregates.AsNoTracking().SingleAsync()).Name);

        var unitOfWork = new EfUnitOfWork<TestDbContext>(context, new DomainEventDispatcher([]));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal("changed", (await verificationContext.Aggregates.AsNoTracking().SingleAsync()).Name);
    }

    [Fact]
    public async Task GetByIdAsync_MissingAggregate_ReturnsNull()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new TestDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var repository = new TestRepository(context);

        Assert.Null(await repository.GetByIdAsync(42, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetByIdAsync_CanceledToken_ThrowsEvenForTrackedAggregate(bool tracked)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new TestDbContext(CreateOptions(connection));
        await context.Database.EnsureCreatedAsync();
        var aggregate = new TestAggregate("tracked");
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();
        if (!tracked)
        {
            context.ChangeTracker.Clear();
        }

        var repository = new TestRepository(context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetByIdAsync(aggregate.Id, cancellation.Token));
    }

    [Fact]
    public async Task RemoveAndRemoveRange_MarkAggregatesWithoutSaving()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = CreateOptions(connection);
        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var first = new TestAggregate("first");
        var others = new[] { new TestAggregate("second"), new TestAggregate("third") };
        context.Aggregates.Add(first);
        context.Aggregates.AddRange(others);
        await context.SaveChangesAsync();
        var repository = new TestRepository(context);

        repository.Remove(first);
        repository.RemoveRange(others);

        Assert.Equal(EntityState.Deleted, context.Entry(first).State);
        Assert.All(others, aggregate => Assert.Equal(EntityState.Deleted, context.Entry(aggregate).State));
        await using var verificationContext = new TestDbContext(options);
        Assert.Equal(3, await verificationContext.Aggregates.CountAsync());

        var unitOfWork = new EfUnitOfWork<TestDbContext>(context, new DomainEventDispatcher([]));
        Assert.Equal(3, await unitOfWork.SaveChangesAsync(CancellationToken.None));
        Assert.Equal(0, await verificationContext.Aggregates.CountAsync());
    }

    private static DbContextOptions<TestDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private sealed class TestRepository(TestDbContext context)
        : RepositoryBase<TestAggregate, int>(context)
    {
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>().HasKey(aggregate => aggregate.Id);
            modelBuilder.Entity<TestAggregate>().Ignore(aggregate => aggregate.DomainEvents);
        }
    }

    private sealed class TestAggregate : AggregateRoot<int>
    {
        private TestAggregate()
        {
        }

        public TestAggregate(string name)
        {
            Name = name;
        }

        public string Name { get; private set; } = null!;

        public void Rename(string name)
        {
            Name = name;
        }
    }
}
