using Oschterhas.Data.Entities;

namespace Oschterhas.Core.Public;

internal static class PublicMapping
{
    public static PublicInstanceDto ToPublicDto(this Instance instance)
    {
        var visible = instance.Status is not InstanceStatus.Draft;
        return new PublicInstanceDto(
            instance.Name,
            instance.Description,
            instance.EventDate,
            instance.Status,
            instance.Hares.Count,
            instance.Hares.Count(h => h.FoundAt is not null),
            visible ? instance.Hares.OrderBy(h => h.Number).Select(h => h.ToPublicDto()).ToList() : null);
    }

    public static PublicHareDto ToPublicDto(this Hare hare) => new(
        hare.Id,
        hare.Number,
        hare.Name,
        hare.Description,
        hare.Lat,
        hare.Lon,
        hare.FoundAt,
        hare.FoundBy);
}
