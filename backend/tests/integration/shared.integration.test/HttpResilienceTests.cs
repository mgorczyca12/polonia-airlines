using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.DependencyInjection;

namespace shared.integration.test;

public sealed class HttpResilienceTests
{
    [Theory]
    [InlineData("GET", 2, HttpStatusCode.OK)]
    [InlineData("POST", 1, HttpStatusCode.ServiceUnavailable)]
    [InlineData("DELETE", 1, HttpStatusCode.ServiceUnavailable)]
    public async Task ResilientClient_RetriesSafeReadsButNotWrites(
        string method,
        int expectedAttempts,
        HttpStatusCode expectedStatus)
    {
        var handler = new TransientFailureHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient("test", client => client.BaseAddress = new Uri("http://localhost"))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddSharedHttpResilience();

        using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("test");
        using var request = new HttpRequestMessage(new HttpMethod(method), "/");
        using var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(expectedAttempts, handler.Attempts);
    }

    private sealed class TransientFailureHandler : HttpMessageHandler
    {
        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Attempts++;
            var status = Attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
