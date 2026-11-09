using Oschterhas.Common.Results;

namespace Oschterhas.Core.Identity;

/// <param name="Subject">Keycloak user id (the <c>sub</c> claim).</param>
/// <param name="Created">True if the account was just created (then an invitation is due).</param>
public sealed record IdentityAccount(string Subject, bool Created);

/// <summary>User administration at Peakcode Identity (Keycloak admin REST API).</summary>
public interface IIdentityAdminClient
{
    /// <summary>Finds the account with exactly this e-mail or creates it (enabled, without password).</summary>
    Task<Result<IdentityAccount>> FindOrCreateUserAsync(string email, string displayName, CancellationToken cancellationToken);

    /// <summary>Lets Keycloak send the invitation e-mail with a link to set the password.</summary>
    Task<Result> SendInvitationAsync(string subject, CancellationToken cancellationToken);
}
