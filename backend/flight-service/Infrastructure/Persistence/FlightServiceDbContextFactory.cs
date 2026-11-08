using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace flight_service.Infrastructure.Persistence;

public sealed class FlightServiceDbContextFactory : IDesignTimeDbContextFactory<FlightServiceDbContext>
{
    public FlightServiceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FlightDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__FlightDatabase for EF Core design-time operations.");
        }

        var options = new DbContextOptionsBuilder<FlightServiceDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FlightServiceDbContext(options);
    }
}
