using System.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using seating_service.Application.Abstractions;
using seating_service.Infrastructure.Integrations.Consumers;
using seating_service.Infrastructure.Persistence;
using seating_service.Infrastructure.Persistence.Redis;
using seating_service.Infrastructure.Repositories;
using Shared.Infrastructure.DependencyInjection;
using StackExchange.Redis;

namespace seating_service.Infrastructure;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<SeatingDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("SeatingDatabase");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:SeatingDatabase is required.");
            }

            options.UseNpgsql(connectionString);
        });
        builder.Services.AddSharedUnitOfWork<SeatingDbContext>();
        builder.Services.AddSharedInfrastructure();
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddOptions<RedisInventoryOptions>()
            .Bind(builder.Configuration.GetSection("Inventory"))
            .Configure(options =>
                options.ConnectionString = builder.Configuration.GetConnectionString("InventoryRedis") ?? string.Empty)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "ConnectionStrings:InventoryRedis is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RedisKey), "Inventory:RedisKey must not be blank.")
            .ValidateOnStart();
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisOptions = ConfigurationOptions.Parse(
                provider.GetRequiredService<IOptions<RedisInventoryOptions>>().Value.ConnectionString);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.BacklogPolicy = BacklogPolicy.FailFast;
            return ConnectionMultiplexer.Connect(redisOptions);
        });
        builder.Services.AddSingleton<IInventoryHolds, RedisInventoryHolds>();

        builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
        builder.Services.AddScoped<IInventoryOperationRepository, InventoryOperationRepository>();
        builder.Services.AddScoped<IInventoryMutationLock, SqlInventoryMutationLock>();
        builder.Services.AddScoped<IInventoryReleaseFence, InventoryReleaseFence>();

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
            bus.AddConsumer<HoldInventoryConsumer>();
            bus.AddConsumer<ConfirmInventoryConsumer>();
            bus.AddConsumer<ReleaseInventoryConsumer>();
            bus.AddConsumer<ConfirmedInventoryCleanupConsumer>();
            bus.AddEntityFrameworkOutbox<SeatingDbContext>(outbox =>
            {
                outbox.UsePostgres();
                // Acquire the durable mutation lock before any inventory reads under READ COMMITTED.
                outbox.IsolationLevel = IsolationLevel.ReadCommitted;
            });
            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                endpoint.UseMessageRetry(retry =>
                {
                    retry.Handle<DbUpdateConcurrencyException>();
                    retry.Handle<DbUpdateException>(exception =>
                        exception.InnerException is PostgresException postgres && IsConcurrencyError(postgres));
                    retry.Handle<PostgresException>(IsConcurrencyError);
                    retry.Handle<NpgsqlException>(exception => exception.IsTransient);
                    retry.Handle<RedisException>();
                    retry.Handle<TimeoutException>();
                    retry.Interval(5, TimeSpan.FromSeconds(2));
                });
                endpoint.UseEntityFrameworkOutbox<SeatingDbContext>(context);
            });
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

        return builder;
    }

    private static bool IsConcurrencyError(PostgresException exception)
    {
        return exception.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected or
            PostgresErrorCodes.UniqueViolation;
    }
}
