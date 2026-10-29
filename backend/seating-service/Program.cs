using seating_service.Infrastructure;
using seating_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddInfrastructure();
builder.Services.AddHealthChecks();
var app = builder.Build();

if (args.Contains("--initialize-database", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<SeatingDbContext>();
    await database.Database.EnsureCreatedAsync();
    return;
}

app.MapHealthChecks("/health");
app.Run();

public partial class Program;
