using flight_service.Application.Features.FlightFeatures.Queries;
using flight_service.Infrastructure.Persistence;
using flight_service.Infrastructure.Persistence.Queries;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.DependencyInjection;

namespace flight_service.Infrastructure;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<FlightServiceDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("FlightDatabase");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:FlightDatabase is required.");
            }

            options.UseNpgsql(connectionString);
        });
        builder.Services.AddSharedUnitOfWork<FlightServiceDbContext>();
        builder.Services.AddSharedInfrastructure();
        builder.Services.AddOptions<RabbitMqTransportOptions>()
            .Bind(builder.Configuration.GetSection("RabbitMq"))
            .Configure(options =>
            {
                options.User = builder.Configuration["RabbitMq:User"] ?? string.Empty;
                options.Pass = builder.Configuration["RabbitMq:Pass"] ?? string.Empty;
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "RabbitMq:Host is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.User), "RabbitMq:User is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Pass), "RabbitMq:Pass is required.")
            .ValidateOnStart();
        builder.Services.AddMassTransit(bus =>
        {
            bus.AddSharedEntityFrameworkOutbox<FlightServiceDbContext>(outbox => outbox.UsePostgres());
            bus.UsingRabbitMq((context, rabbit) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqTransportOptions>>().Value;
                rabbit.Host(options.Host, options.Port, options.VHost, host =>
                {
                    host.Username(options.User);
                    host.Password(options.Pass);
                    if (options.UseSsl)
                    {
                        host.UseSsl();
                    }
                });
                rabbit.ConfigureEndpoints(context);
            });
        });
        builder.Services.AddScoped<IFlightQueries, FlightQueries>();
        return builder;
    }
}
