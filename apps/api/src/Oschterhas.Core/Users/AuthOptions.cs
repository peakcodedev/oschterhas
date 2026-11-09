namespace Oschterhas.Core.Users;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Subjects (Keycloak user ids) that become platform admin on their first login if no user exists yet.
    /// Used to bootstrap production; in development the seed creates the admin.
    /// </summary>
    public IList<string> InitialPlatformAdmins { get; set; } = [];
}
