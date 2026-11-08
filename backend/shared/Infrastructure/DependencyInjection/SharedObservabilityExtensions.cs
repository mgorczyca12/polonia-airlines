using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Infrastructure.DependencyInjection;

public static class SharedObservabilityExtensions
{
    public static WebApplicationBuilder AddSharedObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        if (!builder.Configuration.GetValue<bool>("Telemetry:Enabled"))
        {
            return builder;
        }

        var endpointValue = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Telemetry:Enabled requires an HTTP(S) OTEL_EXPORTER_OTLP_ENDPOINT for the collector.");
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName)
                .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = false;
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation(options => options.RecordException = false)
                .AddSource(DiagnosticHeaders.DefaultListenerName, "Npgsql"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(InstrumentationOptions.MeterName, "Microsoft.AspNetCore.Server.Kestrel"))
            .UseOtlpExporter(OtlpExportProtocol.Grpc, endpoint);

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = false;
            options.IncludeScopes = false;
        });
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>(null, LogLevel.Warning);

        return builder;
    }
}
