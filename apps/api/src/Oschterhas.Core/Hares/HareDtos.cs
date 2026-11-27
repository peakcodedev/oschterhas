namespace Oschterhas.Core.Hares;

public sealed record MarkFoundRequest(string? Nickname);

public sealed record HareFoundDto(Guid HareId, DateTimeOffset FoundAt, string? FoundBy);
