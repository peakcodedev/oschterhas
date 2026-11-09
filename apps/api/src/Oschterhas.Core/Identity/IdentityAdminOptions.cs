namespace Oschterhas.Core.Identity;

/// <summary>Access to the Keycloak admin REST API of the realm (service account oschterhas-user-admin).</summary>
public sealed class IdentityAdminOptions
{
    public const string SectionName = "IdentityAdmin";

    /// <summary>Keycloak base URL, e.g. <c>http://localhost:8180</c>.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string Realm { get; set; } = "oschterhas";

    public string ClientId { get; set; } = "oschterhas-user-admin";

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Issuer stored for invited users; must equal the <c>iss</c> claim of logins (Oidc:Authority).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Client and redirect of the "set your password" link in the invitation e-mail. The redirect must be a
    /// registered redirect URI of that client (Oschterhas: <c>{admin URL}/login</c>), otherwise Keycloak answers 400.
    /// </summary>
    public string InvitationClientId { get; set; } = "oschterhas-bff";

    public string InvitationRedirectUri { get; set; } = string.Empty;

    public TimeSpan InvitationLifespan { get; set; } = TimeSpan.FromDays(3);
}
