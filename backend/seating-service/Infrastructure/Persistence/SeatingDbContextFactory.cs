using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace seating_service.Infrastructure.Persistence;

public sealed class SeatingDbContextFactory : IDesignTimeDbContextFactory<SeatingDbContext>
{
    public SeatingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SeatingDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__SeatingDatabase for EF Core design-time operations.");
        }

        var options = new DbContextOptionsBuilder<SeatingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SeatingDbContext(options);
    }
}
