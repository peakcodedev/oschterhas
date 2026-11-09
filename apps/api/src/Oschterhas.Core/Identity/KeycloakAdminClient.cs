using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Oschterhas.Common.Results;

namespace Oschterhas.Core.Identity;

/// <summary>
/// Minimal client for the Keycloak admin REST API (no Keycloak SDK, see docs/lizenzen.md).
/// Authenticates with the client-credentials grant of the service account and caches the token.
/// </summary>
internal sealed class KeycloakAdminClient(
    HttpClient http,
    IOptions<IdentityAdminOptions> options,
    TimeProvider timeProvider) : IIdentityAdminClient
{
    private static readonly Error Unavailable = Error.Unexpected("identity.unavailable", "Peakcode Identity is not reachable.");

    private readonly IdentityAdminOptions _options = options.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _token;
    private DateTimeOffset _tokenExpiresAt;

    private string AdminBase => $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{Uri.EscapeDataString(_options.Realm)}";

    public async Task<Result<IdentityAccount>> FindOrCreateUserAsync(string email, string displayName, CancellationToken cancellationToken)
    {
        var existing = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"{AdminBase}/users?exact=true&email={Uri.EscapeDataString(email)}"),
            cancellationToken).ConfigureAwait(false);
        if (existing is null || !existing.IsSuccessStatusCode)
        {
            existing?.Dispose();
            return Unavailable;
        }

        using (existing)
        {
            var found = await existing.Content.ReadFromJsonAsync<List<KeycloakUser>>(cancellationToken).ConfigureAwait(false);
            var match = found?.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (match?.Id is { } id)
            {
                return new IdentityAccount(id, Created: false);
            }
        }

        var newUser = new KeycloakUser { Username = email, Email = email, FirstName = displayName, Enabled = true };
        using var created = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Post, $"{AdminBase}/users") { Content = JsonContent.Create(newUser) },
            cancellationToken).ConfigureAwait(false);
        if (created is null)
        {
            return Unavailable;
        }

        if (created.StatusCode == HttpStatusCode.Conflict)
        {
            return Error.Conflict("identity.user_conflict", "An account with this e-mail or username already exists.");
        }

        // Keycloak returns the new user's URL in the Location header: .../users/{id}
        var location = created.Headers.Location?.ToString();
        if (created.StatusCode != HttpStatusCode.Created || string.IsNullOrEmpty(location))
        {
            return Unavailable;
        }

        return new IdentityAccount(location[(location.LastIndexOf('/') + 1)..], Created: true);
    }

    public async Task<Result> SendInvitationAsync(string subject, CancellationToken cancellationToken)
    {
        var query = $"client_id={Uri.EscapeDataString(_options.InvitationClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_options.InvitationRedirectUri)}"
            + $"&lifespan={(int)_options.InvitationLifespan.TotalSeconds}";
        using var response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Put, $"{AdminBase}/users/{Uri.EscapeDataString(subject)}/execute-actions-email?{query}")
            {
                Content = JsonContent.Create(new[] { "UPDATE_PASSWORD" }),
            },
            cancellationToken).ConfigureAwait(false);

        return response is { IsSuccessStatusCode: true } ? Result.Success() : Unavailable;
    }

    /// <summary>Sends with a bearer token; on 401 the token is renewed once. Null if Keycloak is unreachable.</summary>
    private async Task<HttpResponseMessage?> SendAsync(Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
    {
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                var token = await GetTokenAsync(forceRefresh: attempt > 0, cancellationToken).ConfigureAwait(false);
                if (token is null)
                {
                    return null;
                }

                using var request = createRequest();
                request.Headers.Authorization = new("Bearer", token);
                var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.Unauthorized || attempt > 0)
                {
                    return response;
                }

                response.Dispose();
            }
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task<string?> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && _token is not null && timeProvider.GetUtcNow() < _tokenExpiresAt)
            {
                return _token;
            }

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
            });
            var tokenUrl = $"{_options.BaseUrl.TrimEnd('/')}/realms/{Uri.EscapeDataString(_options.Realm)}/protocol/openid-connect/token";
            using var response = await http.PostAsync(new Uri(tokenUrl), content, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken).ConfigureAwait(false);
            if (token?.AccessToken is null)
            {
                return null;
            }

            _token = token.AccessToken;
            // Renew 30 seconds early to avoid using a token that expires on the way.
            _tokenExpiresAt = timeProvider.GetUtcNow().AddSeconds(Math.Max(0, token.ExpiresIn - 30));
            return _token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class KeycloakUser
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }
}
