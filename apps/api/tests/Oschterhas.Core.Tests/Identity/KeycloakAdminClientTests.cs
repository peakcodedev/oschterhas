using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Oschterhas.Core.Identity;

namespace Oschterhas.Core.Tests.Identity;

public class KeycloakAdminClientTests
{
    private readonly FakeKeycloak _keycloak = new();
    private readonly FakeTimeProvider _time = new();
    private readonly KeycloakAdminClient _client;

    public KeycloakAdminClientTests() =>
        _client = new KeycloakAdminClient(
            new HttpClient(_keycloak),
            Options.Create(new IdentityAdminOptions
            {
                BaseUrl = "http://kc:8080/",
                Realm = "oschterhas",
                ClientId = "oschterhas-user-admin",
                ClientSecret = "secret",
                InvitationClientId = "oschterhas-bff",
                InvitationRedirectUri = "http://admin.oschterhas.localhost:4300/",
                InvitationLifespan = TimeSpan.FromDays(3),
            }),
            _time);

    [Fact]
    public async Task FindsExistingUserByExactEmail()
    {
        _keycloak.Users = """[{"id":"u-1","email":"otto@test","username":"otto@test"}]""";

        var account = await _client.FindOrCreateUserAsync("otto@test", "Otto", TestContext.Current.CancellationToken);

        Assert.Equal(new IdentityAccount("u-1", Created: false), account.Value);
        var search = _keycloak.Requests.Single(r => r.Method == HttpMethod.Get);
        Assert.Equal("http://kc:8080/admin/realms/oschterhas/users?exact=true&email=otto%40test", search.Url);
        Assert.Equal("Bearer token-1", search.Authorization);
        Assert.Contains("grant_type=client_credentials", _keycloak.Requests[0].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreatesUserWhenNoneExists()
    {
        var account = await _client.FindOrCreateUserAsync("new@test", "Neu Organisator", TestContext.Current.CancellationToken);

        Assert.Equal(new IdentityAccount("new-id", Created: true), account.Value);
        var create = _keycloak.Requests.Single(r => r.Method == HttpMethod.Post && r.Url.EndsWith("/users", StringComparison.Ordinal));
        Assert.Contains("\"email\":\"new@test\"", create.Body, StringComparison.Ordinal);
        Assert.Contains("\"enabled\":true", create.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"id\"", create.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendsInvitationWithUpdatePasswordAction()
    {
        var result = await _client.SendInvitationAsync("u-1", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var put = _keycloak.Requests.Single(r => r.Method == HttpMethod.Put);
        Assert.Equal(
            "http://kc:8080/admin/realms/oschterhas/users/u-1/execute-actions-email?client_id=oschterhas-bff&redirect_uri=http%3A%2F%2Fadmin.oschterhas.localhost%3A4300%2F&lifespan=259200",
            put.Url);
        Assert.Equal("[\"UPDATE_PASSWORD\"]", put.Body);
    }

    [Fact]
    public async Task ReusesTokenUntilShortlyBeforeExpiry()
    {
        var ct = TestContext.Current.CancellationToken;
        await _client.SendInvitationAsync("a", ct);
        _time.Advance(TimeSpan.FromSeconds(200));
        await _client.SendInvitationAsync("b", ct);
        _time.Advance(TimeSpan.FromSeconds(80));
        await _client.SendInvitationAsync("c", ct);

        Assert.Equal(2, _keycloak.TokenRequests);
    }

    [Fact]
    public async Task RenewsTokenOnceWhenRejected()
    {
        _keycloak.RejectFirstAdminCall = true;

        var result = await _client.SendInvitationAsync("u-1", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _keycloak.TokenRequests);
    }

    [Fact]
    public async Task ReportsUnavailableKeycloak()
    {
        _keycloak.Down = true;

        var result = await _client.FindOrCreateUserAsync("x@test", "X", TestContext.Current.CancellationToken);

        Assert.Equal("identity.unavailable", result.Error!.Code);
    }

    [Fact]
    public async Task ReportsConflictOnCreate()
    {
        _keycloak.CreateStatus = HttpStatusCode.Conflict;

        var result = await _client.FindOrCreateUserAsync("x@test", "X", TestContext.Current.CancellationToken);

        Assert.Equal("identity.user_conflict", result.Error!.Code);
    }

    private sealed record RecordedRequest(HttpMethod Method, string Url, string? Authorization, string Body);

    /// <summary>Scripted stand-in for the Keycloak endpoints the client uses.</summary>
    private sealed class FakeKeycloak : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        public string Users { get; set; } = "[]";

        public bool Down { get; set; }

        public bool RejectFirstAdminCall { get; set; }

        public HttpStatusCode CreateStatus { get; set; } = HttpStatusCode.Created;

        public int TokenRequests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Down)
            {
                throw new HttpRequestException("connection refused");
            }

            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            var url = request.RequestUri!.ToString();
            Requests.Add(new RecordedRequest(request.Method, url, request.Headers.Authorization?.ToString(), body));

            if (url.EndsWith("/protocol/openid-connect/token", StringComparison.Ordinal))
            {
                TokenRequests++;
                return Json($$"""{"access_token":"token-{{TokenRequests}}","expires_in":300}""");
            }

            if (RejectFirstAdminCall)
            {
                RejectFirstAdminCall = false;
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            if (request.Method == HttpMethod.Get)
            {
                return Json(Users);
            }

            if (request.Method == HttpMethod.Post)
            {
                var response = new HttpResponseMessage(CreateStatus);
                response.Headers.Location = new Uri("http://kc:8080/admin/realms/oschterhas/users/new-id");
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private static HttpResponseMessage Json(string json) =>
            new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
