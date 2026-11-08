using System.Collections.Concurrent;
using System.Diagnostics;
using MassTransit.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shared.Infrastructure.DependencyInjection;

namespace shared.integration.test;

public sealed class ObservabilityTests
{
    [Fact]
    public void AddSharedObservability_Disabled_DoesNotRegisterExportProviders()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "false"
        });
        builder.AddSharedObservability("test-service");

        Assert.DoesNotContain(builder.Services, service => service.ServiceType == typeof(TracerProvider));
        Assert.DoesNotContain(builder.Services, service => service.ServiceType == typeof(MeterProvider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("file:///collector")]
    public void AddSharedObservability_EnabledWithoutHttpEndpoint_FailsExplicitly(string? endpoint)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint
        });

        Assert.Throws<InvalidOperationException>(() => builder.AddSharedObservability("test-service"));
    }

    [Fact]
    public async Task AddSharedObservability_Enabled_CapturesCorrelatedHttpMessagingMetricsAndLogs()
    {
        var activities = new ConcurrentQueue<Activity>();
        var logs = new ConcurrentQueue<(string? Category, string? TraceId, LogLevel Level)>();
        var metrics = new ConcurrentQueue<string>();
        var metricReader = new BaseExportingMetricReader(new MetricExporter(metrics));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:1"
        });
        builder.AddSharedObservability("test-service");
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddProcessor(
                new SimpleActivityExportProcessor(new ActivityExporter(activities))))
            .WithMetrics(meter => meter.AddReader(metricReader));
        builder.Logging.AddOpenTelemetry(options => options.AddProcessor(new LogProcessor(logs)));

        await using var app = builder.Build();
        using var source = new ActivitySource(DiagnosticHeaders.DefaultListenerName);
        app.MapGet("/test", (ILogger<ObservabilityTests> logger) =>
        {
            using var consume = source.StartActivity("synthetic consume", ActivityKind.Consumer);
            logger.LogInformation("Synthetic low-severity diagnostic");
            logger.LogWarning("Synthetic telemetry validation");
            return "ok";
        });
        app.MapGet("/health", () => "healthy");
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        const string traceId = "12345678901234567890123456789012";
        request.Headers.Add("traceparent", $"00-{traceId}-1234567890123456-01");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var health = await client.GetAsync("/health");
        health.EnsureSuccessStatusCode();
        Assert.True(metricReader.Collect());

        Assert.Contains(activities, activity => activity.Kind == ActivityKind.Server
            && activity.TraceId.ToString() == traceId);
        Assert.Contains(activities, activity => activity.Source.Name == DiagnosticHeaders.DefaultListenerName
            && activity.TraceId.ToString() == traceId);
        Assert.DoesNotContain(activities, activity => activity.GetTagItem("url.path")?.ToString() == "/health");
        Assert.Contains(logs, log => log.Category == typeof(ObservabilityTests).FullName
            && log.TraceId == traceId && log.Level == LogLevel.Warning);
        Assert.DoesNotContain(logs, log => log.Category == typeof(ObservabilityTests).FullName
            && log.Level == LogLevel.Information);
        Assert.Contains(metrics, name => name == "http.server.request.duration");
        var resource = app.Services.GetRequiredService<TracerProvider>().GetResource();
        Assert.Contains(resource.Attributes, attribute => attribute.Key == "service.name"
            && attribute.Value.ToString() == "test-service");
    }

    private sealed class ActivityExporter(ConcurrentQueue<Activity> activities) : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
            {
                activities.Enqueue(activity);
            }

            return ExportResult.Success;
        }
    }

    private sealed class MetricExporter(ConcurrentQueue<string> metrics) : BaseExporter<Metric>
    {
        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
            {
                metrics.Enqueue(metric.Name);
            }

            return ExportResult.Success;
        }
    }

    private sealed class LogProcessor(
        ConcurrentQueue<(string? Category, string? TraceId, LogLevel Level)> logs) : BaseProcessor<LogRecord>
    {
        public override void OnEnd(LogRecord record)
        {
            logs.Enqueue((record.CategoryName, record.TraceId.ToString(), record.LogLevel));
        }
    }
}
