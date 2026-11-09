using Oschterhas.Core.Hosting;
using Oschterhas.Data.Entities;

namespace Oschterhas.Core.Admin;

internal static class AdminMapping
{
    public static string SubdomainUrl(this Instance instance, HostingOptions hosting) =>
        hosting.PublicUrl($"{instance.Slug}.{hosting.BaseDomain}");

    /// <summary>The address shown to searchers: the verified custom domain if there is one.</summary>
    public static string PublicUrl(this Instance instance, HostingOptions hosting) =>
        instance is { CustomDomainStatus: CustomDomainStatus.Verified, CustomDomain: { } domain }
            ? hosting.PublicUrl(domain)
            : instance.SubdomainUrl(hosting);

    public static InstanceSummaryDto ToSummaryDto(this Instance instance, HostingOptions hosting) => new(
        instance.Id,
        instance.Slug,
        instance.Name,
        instance.Status,
        instance.EventDate,
        instance.Hares.Count,
        instance.Hares.Count(h => h.FoundAt is not null),
        instance.PublicUrl(hosting));

    public static InstanceDetailDto ToDetailDto(this Instance instance, HostingOptions hosting) => new(
        instance.Id,
        instance.Slug,
        instance.Name,
        instance.Description,
        instance.EventDate,
        instance.Status,
        instance.Hares.Count,
        instance.Hares.Count(h => h.FoundAt is not null),
        instance.PublicUrl(hosting),
        instance.SubdomainUrl(hosting),
        instance.CustomDomain,
        instance.CustomDomainStatus,
        instance.Organizers
            .OrderBy(o => o.User.DisplayName)
            .Select(o => o.User.ToOrganizerDto())
            .ToList());

    public static OrganizerDto ToOrganizerDto(this AppUser user) =>
        new(user.Id, user.Email, user.DisplayName, user.LastLoginAt is null);

    public static AdminHareDto ToAdminDto(this Hare hare) => new(
        hare.Id,
        hare.Number,
        hare.Name,
        hare.Description,
        hare.Lat,
        hare.Lon,
        hare.Elevation,
        hare.SourceWaypointId,
        hare.FoundAt,
        hare.FoundBy);
}
