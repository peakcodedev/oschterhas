using System.Net;
using Microsoft.Extensions.Options;
using Oschterhas.Core.Domains;

namespace Oschterhas.Core.Tests.Domains;

public class HttpDomainVerifierTests
{
    [Fact]
    public async Task MatchingToken_IsVerified_AndRequestGoesToWellKnownPathOverHttp()
    {
        var handler = Respond(HttpStatusCode.OK, " token-123\n");

        var outcome = await Check(handler, port: null);

        Assert.Equal(DomainCheckOutcome.Verified, outcome);
        Assert.Equal("http://ostern.ceviduernten.ch/.well-known/oschterhas-verification", handler.LastUrl);
    }

    [Fact]
    public async Task UsesConfiguredPort()
    {
        var handler = Respond(HttpStatusCode.OK, "token-123");

        await Check(handler, port: 4300);

        Assert.Equal("http://ostern.ceviduernten.ch:4300/.well-known/oschterhas-verification", handler.LastUrl);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "other-token", DomainCheckOutcome.WrongResponse)]
    [InlineData(HttpStatusCode.NotFound, "", DomainCheckOutcome.WrongResponse)]
    [InlineData(HttpStatusCode.MovedPermanently, "", DomainCheckOutcome.Redirect)]
    [InlineData(HttpStatusCode.Found, "", DomainCheckOutcome.Redirect)]
    public async Task OtherAnswers_AreNotVerified(HttpStatusCode status, string body, DomainCheckOutcome expected)
    {
        var handler = Respond(status, body);

        Assert.Equal(expected, await Check(handler, port: null));
    }

    [Fact]
    public async Task LongBody_IsCutAndDoesNotMatch()
    {
        var handler = Respond(HttpStatusCode.OK, "token-123" + new string('x', 5000));

        Assert.Equal(DomainCheckOutcome.WrongResponse, await Check(handler, port: null));
    }

    [Fact]
    public async Task NetworkError_IsUnreachable()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("no route"));

        Assert.Equal(DomainCheckOutcome.Unreachable, await Check(handler, port: null));
    }

    [Fact]
    public async Task PrivateAddress_IsReported()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("blocked", new PrivateAddressException("ostern.ceviduernten.ch")));

        Assert.Equal(DomainCheckOutcome.PrivateAddress, await Check(handler, port: null));
    }

    [Fact]
    public async Task SlowServer_TimesOut()
    {
        var handler = new StubHandler(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var outcome = await Check(handler, port: null, timeout: TimeSpan.FromMilliseconds(50));

        Assert.Equal(DomainCheckOutcome.Timeout, outcome);
    }

    [Fact]
    public void Handler_FollowsNoRedirects_AndUsesNoProxy()
    {
        using var handler = HttpDomainVerifier.CreateHandler(new CustomDomainOptions());

        Assert.False(handler.AllowAutoRedirect);
        Assert.False(handler.UseProxy);
        Assert.NotNull(handler.ConnectCallback);
    }

    private static Task<DomainCheckOutcome> Check(StubHandler handler, int? port, TimeSpan? timeout = null)
    {
        var options = Options.Create(new CustomDomainOptions { VerificationPort = port, RequestTimeout = timeout ?? TimeSpan.FromSeconds(5) });
        var verifier = new HttpDomainVerifier(new HttpClient(handler), options);
        return verifier.CheckAsync("ostern.ceviduernten.ch", "token-123", TestContext.Current.CancellationToken);
    }

    private static StubHandler Respond(HttpStatusCode status, string body) =>
        new(_ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) }));

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, Task<HttpResponseMessage>> _respond;

        public StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) => _respond = respond;

        public string? LastUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUrl = request.RequestUri!.ToString();
            return _respond(cancellationToken);
        }
    }
}
