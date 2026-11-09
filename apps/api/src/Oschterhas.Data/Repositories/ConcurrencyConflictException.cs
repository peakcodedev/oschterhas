namespace Oschterhas.Data.Repositories;

/// <summary>Someone else changed the same row in the meantime (optimistic concurrency via <c>xmin</c>).</summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The data was changed by someone else in the meantime.", innerException);
