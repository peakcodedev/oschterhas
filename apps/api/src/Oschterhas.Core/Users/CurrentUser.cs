namespace Oschterhas.Core.Users;

/// <summary>The signed-in, active user, loaded from the database on every request.</summary>
public sealed record CurrentUser(Guid Id, string Email, string DisplayName, bool IsPlatformAdmin);

/// <summary>Identity as delivered by Peakcode Identity after login.</summary>
public sealed record ExternalIdentity(string Issuer, string Subject, string? Email, string? DisplayName);
