using System.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using seating_service.Application.Abstractions;
using seating_service.Infrastructure.Consumers;
using seating_service.Infrastructure.Persistence;
using seating_service.Infrastructure.Redis;
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
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = builder.Configuration.GetConnectionString("InventoryRedis");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:InventoryRedis is required.");
            }

            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.BacklogPolicy = BacklogPolicy.FailFast;
            return ConnectionMultiplexer.Connect(options);
        });
        builder.Services.AddSingleton<IInventoryHolds, RedisInventoryHolds>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<InventoryParticipant>();
        builder.Services.AddScoped<ConfirmedInventoryCleanup>();
        builder.Services.AddScoped<IInventoryReleaseFence, InventoryReleaseFence>();
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
                var host = builder.Configuration["RabbitMq:Host"];
                var user = builder.Configuration["RabbitMq:User"];
                var password = builder.Configuration["RabbitMq:Pass"];
                if (string.IsNullOrWhiteSpace(host) ||
                    string.IsNullOrWhiteSpace(user) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException("RabbitMq:Host, User and Pass are required.");
                }

                var port = builder.Configuration.GetValue<ushort?>("RabbitMq:Port") ?? 5672;
                var virtualHost = builder.Configuration["RabbitMq:VHost"] ?? "/";
                rabbit.Host(host, port, virtualHost, transport =>
                {
                    transport.Username(user);
                    transport.Password(password);
                    if (builder.Configuration.GetValue<bool>("RabbitMq:UseSsl"))
                    {
                        transport.UseSsl();
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
