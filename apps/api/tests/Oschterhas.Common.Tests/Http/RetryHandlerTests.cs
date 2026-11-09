using System.Net;
using Oschterhas.Common.Http;
using Oschterhas.Common.Resilience;

namespace Oschterhas.Common.Tests.Http;

public class RetryHandlerTests
{
    private static readonly RetryOptions Options = new() { MaxAttempts = 3, BaseDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero };

    [Fact]
    public async Task RetriesGetOnServerError()
    {
        var inner = new ScriptedHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway, HttpStatusCode.OK);
        using var client = CreateClient(inner);

        using var response = await client.GetAsync(new Uri("http://test/users"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, inner.Calls);
    }

    [Fact]
    public async Task RetriesPutWithBody()
    {
        var inner = new ScriptedHandler(HttpStatusCode.InternalServerError, HttpStatusCode.NoContent);
        using var client = CreateClient(inner);

        using var response = await client.PutAsync(
            new Uri("http://test/x"), new StringContent("[\"UPDATE_PASSWORD\"]"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["[\"UPDATE_PASSWORD\"]", "[\"UPDATE_PASSWORD\"]"], inner.Bodies);
    }

    [Fact]
    public async Task NeverRetriesPost()
    {
        var inner = new ScriptedHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var client = CreateClient(inner);

        using var response = await client.PostAsync(new Uri("http://test/users"), new StringContent("{}"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task DoesNotRetryClientErrors()
    {
        var inner = new ScriptedHandler(HttpStatusCode.NotFound, HttpStatusCode.OK);
        using var client = CreateClient(inner);

        using var response = await client.GetAsync(new Uri("http://test/users"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task ThrowsAfterLastAttempt()
    {
        var inner = new ScriptedHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable);
        using var client = CreateClient(inner);

        var ex = await Assert.ThrowsAsync<TransientHttpException>(() => client.GetAsync(new Uri("http://test/users"), TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.Equal(3, inner.Calls);
    }

    [Fact]
    public async Task RetriesNetworkErrors()
    {
        var inner = new ScriptedHandler(null, HttpStatusCode.OK);
        using var client = CreateClient(inner);

        using var response = await client.GetAsync(new Uri("http://test/users"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpClient CreateClient(ScriptedHandler inner) =>
        new(new RetryHandler(Options, TimeProvider.System) { InnerHandler = inner });

    /// <summary>Returns the given status codes in order; null throws a network error.</summary>
    private sealed class ScriptedHandler(params HttpStatusCode?[] script) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var status = script[Calls++];
            if (request.Content is not null)
            {
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return status is { } code ? new HttpResponseMessage(code) : throw new HttpRequestException("network down");
        }
    }
}
