using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Oschterhas.Common.Results;
using Oschterhas.Data;

namespace Oschterhas.Core.Instances;

/// <summary>Rules for instance slugs (the subdomain in <c>{slug}.oschterhas.li</c>).</summary>
public static partial class SlugRules
{
    public const int MinLength = 3;

    /// <summary>Subdomains used by the platform itself; never available as an instance slug.</summary>
    public static readonly FrozenSet<string> Reserved = new[]
    {
        "admin", "www", "api", "identity", "auth", "mail", "static", "cdn", "test", "staging", "dev", "custom",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Trims and lower-cases <paramref name="slug"/> and checks format and reserved names.</summary>
    public static Result<string> Normalize(string? slug)
    {
        var normalized = slug?.Trim().ToLowerInvariant() ?? string.Empty;

        if (normalized.Length == 0)
        {
            return Error.Validation("instance.slug_required", "Slug is required.");
        }

        if (normalized.Length is < MinLength or > FieldLengths.Slug || !SlugPattern().IsMatch(normalized))
        {
            return Error.Validation(
                "instance.slug_invalid",
                $"Slug must be {MinLength}-{FieldLengths.Slug} characters: a-z, 0-9 and '-', not at the start or end.");
        }

        if (Reserved.Contains(normalized))
        {
            return Error.Validation("instance.slug_reserved", $"Slug '{normalized}' is reserved.");
        }

        return normalized;
    }

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
