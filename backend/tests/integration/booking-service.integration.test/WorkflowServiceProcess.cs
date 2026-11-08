using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace booking_service.integration.test;

internal sealed class WorkflowServiceProcess : IAsyncDisposable
{
    private readonly Process process;
    private readonly ConcurrentQueue<string> output = new();
    private bool started;
    public HttpClient Client { get; }

    public WorkflowServiceProcess(string assembly, Dictionary<string, string?> settings)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        Client = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(assembly)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(assembly);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_URLS"] = Client.BaseAddress.ToString();
        start.Environment["RabbitMq__UseSsl"] = "false";
        start.Environment["MASSTRANSIT_USAGE_TELEMETRY"] = "false";
        foreach (var setting in settings)
        {
            start.Environment[setting.Key.Replace(":", "__", StringComparison.Ordinal)] = setting.Value;
        }

        process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, data) => Capture(data.Data);
        process.ErrorDataReceived += (_, data) => Capture(data.Data);
    }

    private void Capture(string? line)
    {
        if (line is not null)
        {
            output.Enqueue(line);
            while (output.Count > 60)
            {
                output.TryDequeue(out _);
            }
        }
    }

    public async Task Start(string readinessPath)
    {
        Assert.True(process.Start());
        started = true;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await ReservationBrokerWorkflowTests.WaitUntil(async () =>
        {
            Assert.False(process.HasExited, $"Service exited: {string.Join(Environment.NewLine, output)}");
            try
            {
                using var response = await Client.GetAsync(readinessPath);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                // The child process has not bound its HTTP listener yet.
                return false;
            }
        }, () => $"Service did not become ready: {string.Join(Environment.NewLine, output)}");
    }

    public async Task Kill()
    {
        Assert.False(process.HasExited, $"Service already exited: {string.Join(Environment.NewLine, output)}");
        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        Assert.True(process.HasExited);
    }

    public async ValueTask DisposeAsync()
    {
        if (started && !process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        Client.Dispose();
        process.Dispose();
    }
}
