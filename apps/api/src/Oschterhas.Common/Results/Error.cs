namespace Oschterhas.Common.Results;

/// <summary>Kind of failure; the API maps it to an HTTP status code.</summary>
public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unexpected,
}

/// <summary>
/// A failure with a stable <paramref name="Code"/> (e.g. <c>instance.slug_taken</c>).
/// The frontend translates the code; <paramref name="Message"/> is for logs and developers.
/// </summary>
public sealed record Error(string Code, string Message, ErrorKind Kind)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorKind.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorKind.Forbidden);

    public static Error Unexpected(string code, string message) => new(code, message, ErrorKind.Unexpected);
}
