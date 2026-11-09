namespace Oschterhas.Data.Repositories;

/// <summary>A unique index was violated, e.g. a slug or custom domain that is already taken.</summary>
public sealed class UniqueConstraintException(string? constraintName, Exception innerException)
    : Exception($"Unique constraint '{constraintName}' violated.", innerException)
{
    public string? ConstraintName { get; } = constraintName;
}
