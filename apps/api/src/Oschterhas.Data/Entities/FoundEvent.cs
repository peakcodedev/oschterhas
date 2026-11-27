namespace Oschterhas.Data.Entities;

/// <summary>History of found and reset actions on a hare.</summary>
public class FoundEvent
{
    public Guid Id { get; set; }

    public Guid HareId { get; set; }

    public Hare Hare { get; set; } = null!;

    public FoundEventType Type { get; set; }

    public string? Nickname { get; set; }

    /// <summary>Organizer who reset the hare; null for searchers.</summary>
    public Guid? UserId { get; set; }

    public AppUser? User { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
