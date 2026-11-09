namespace Oschterhas.Data.Entities;

/// <summary>Platform admin or organizer. Signs in via Peakcode Identity; matched by issuer + subject.</summary>
public class AppUser
{
    public Guid Id { get; set; }

    public required string IdentityIssuer { get; set; }

    public required string IdentitySubject { get; set; }

    /// <summary>For display and invitations only, never for matching a login.</summary>
    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public bool IsPlatformAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public string Locale { get; set; } = "de";

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null until the invited person has signed in for the first time.</summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    public List<InstanceOrganizer> Instances { get; set; } = [];
}
