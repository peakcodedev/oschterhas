namespace Oschterhas.Core.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsPlatformAdmin,
    bool IsActive,
    bool InvitationPending,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<Guid> InstanceIds);

/// <summary>Null fields stay unchanged.</summary>
public sealed record UpdateUserRequest(bool? IsActive, bool? IsPlatformAdmin);

public sealed record MeDto(Guid Id, string Email, string DisplayName, bool IsPlatformAdmin);
