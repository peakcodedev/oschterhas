using Oschterhas.Core.Users;
using Oschterhas.Data.Entities;

namespace Oschterhas.Core.Tests;

internal static class TestData
{
    public static readonly CurrentUser Admin = new(Guid.NewGuid(), "pia@test", "Pia", IsPlatformAdmin: true);
    public static readonly CurrentUser Otto = new(Guid.NewGuid(), "otto@test", "Otto", IsPlatformAdmin: false);
    public static readonly CurrentUser Stranger = new(Guid.NewGuid(), "x@test", "X", IsPlatformAdmin: false);

    /// <summary>Instance "tann" organized by Otto.</summary>
    public static Instance Tann()
    {
        var instance = new Instance { Id = Guid.NewGuid(), Slug = "tann", Name = "Ostersuche Tann", Status = InstanceStatus.Draft };
        var otto = new AppUser
        {
            Id = Otto.Id,
            IdentityIssuer = "iss",
            IdentitySubject = "otto-sub",
            Email = Otto.Email,
            DisplayName = Otto.DisplayName,
            LastLoginAt = DateTimeOffset.UnixEpoch,
        };
        instance.Organizers.Add(new InstanceOrganizer { InstanceId = instance.Id, UserId = otto.Id, User = otto });
        return instance;
    }
}
