using booking_service.Application.Abstractions;
using booking_service.Application.Features.Catalog;
using booking_service.Infrastructure.Persistence;
using booking_service.Infrastructure.Sagas;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.DependencyInjection;

namespace booking_service.Infrastructure;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddDbContext<BookingDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("BookingDatabase");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:BookingDatabase is required.");
            }

            options.UseNpgsql(connectionString);
        });
        builder.Services.AddSharedUnitOfWork<BookingDbContext>();
        builder.Services.AddSharedInfrastructure();
        builder.Services.AddScoped<BookingService>();
        builder.Services.AddScoped<ICatalogAdministration, CatalogAdministration>();
        builder.Services.AddScoped<IBookingService>(provider => provider.GetRequiredService<BookingService>());
        builder.Services.AddScoped<IBookingQueries>(provider => provider.GetRequiredService<BookingService>());
        builder.Services.AddScoped<CompleteReservationActivity>();
        builder.Services.AddScoped<CancelReservationActivity>();
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
            bus.AddSharedEntityFrameworkOutbox<BookingDbContext>(outbox => outbox.UsePostgres());
            bus.AddReservationWorkflow();
            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
                endpoint.UseEntityFrameworkOutbox<BookingDbContext>(context));
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
        builder.Services.AddHostedService<ReservationDeadlineService>();
        return builder;
    }
}
