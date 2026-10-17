using flight_service.Infrastructure;
using flight_service.Infrastructure.Persistence;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Application.Abstractions;
using Shared.Infrastructure.Messaging;

namespace flight_service.integration.test;

public sealed class FlightInfrastructureTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AddInfrastructure_MissingRabbitMqPassword_RejectsOptions(string? password)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RabbitMq:User"] = "unused",
            ["RabbitMq:Pass"] = password
        });
        builder.AddInfrastructure();
        await using var provider = builder.Services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<RabbitMqTransportOptions>>().Value);

        Assert.Contains("RabbitMq:Pass is required.", exception.Failures);
    }

    [Fact]
    public async Task AddInfrastructure_ConfiguresPostgresOutboxAndRabbitMqWithoutOpeningConnections()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:FlightDatabase"] = "Host=localhost;Database=flight_tests;Username=unused",
            ["RabbitMq:Host"] = "localhost",
            ["RabbitMq:User"] = "unused",
            ["RabbitMq:Pass"] = "unused"
        });
        builder.AddInfrastructure();
        await using var provider = builder.Services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FlightServiceDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.NotNull(context.Model.FindEntityType(typeof(InboxState)));
        Assert.NotNull(context.Model.FindEntityType(typeof(OutboxMessage)));
        Assert.NotNull(context.Model.FindEntityType(typeof(OutboxState)));
        var schema = context.Database.GenerateCreateScript();
        Assert.Contains("CREATE TABLE \"OutboxMessage\"", schema);
        Assert.Contains("CREATE TABLE \"OutboxState\"", schema);
        Assert.Contains("CREATE TABLE \"SeatMaps\"", schema);
        Assert.Contains("CREATE UNIQUE INDEX \"IX_SeatMaps_AircraftConfigurationId\"", schema);
        Assert.Contains("FOREIGN KEY (\"SeatMapId\") REFERENCES \"SeatMaps\" (\"Id\") ON DELETE CASCADE", schema);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        Assert.IsType<MassTransitIntegrationEventPublisher>(
            scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>());
        var rabbit = provider.GetRequiredService<IOptions<RabbitMqTransportOptions>>().Value;
        Assert.Equal("unused", rabbit.User);
        Assert.Equal("unused", rabbit.Pass);
    }
}
