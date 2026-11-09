namespace Oschterhas.Api.Auth;

/// <summary>Login at Peakcode Identity (Keycloak realm "oschterhas", client "oschterhas-bff").</summary>
public sealed class OidcOptions
{
    public const string SectionName = "Oidc";

    /// <summary>Issuer, e.g. <c>http://localhost:8180/realms/oschterhas</c>.</summary>
    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = "oschterhas-bff";

    public string ClientSecret { get; set; } = string.Empty;
}
