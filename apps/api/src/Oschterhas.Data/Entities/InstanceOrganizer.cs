namespace Oschterhas.Data.Entities;

public class InstanceOrganizer
{
    public Guid InstanceId { get; set; }

    public Instance Instance { get; set; } = null!;

    public Guid UserId { get; set; }

    public AppUser User { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
