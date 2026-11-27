using System.Globalization;
using Oschterhas.Common.Results;
using Oschterhas.Data;

namespace Oschterhas.Core.Domains;

/// <summary>Validation and normalization of custom domains like <c>ostern.ceviduernten.ch</c>.</summary>
public static class CustomDomainRules
{
    private static readonly IdnMapping Idn = new() { UseStd3AsciiRules = true };

    /// <summary>
    /// Lower case, without scheme, path, port or trailing dot, IDN as punycode (e.g. <c>dürnten.ch</c> → <c>xn--drnten-3ya.ch</c>).
    /// Rejects IP addresses, single labels, <c>localhost</c> and the platform's own domain and its subdomains.
    /// </summary>
    public static Result<string> Normalize(string? input, string baseDomain, bool allowLocalhost)
    {
        var value = input?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return Error.Validation("customdomain.required", "Please enter a domain.");
        }

        // Accept a pasted URL like https://ostern.ceviduernten.ch/ and keep only the host.
        if (value.Contains("://", StringComparison.Ordinal))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return Invalid();
            }

            value = uri.Host;
        }

        value = value.TrimEnd('.').ToLowerInvariant();
        if (value.Contains(':', StringComparison.Ordinal) || value.Contains('/', StringComparison.Ordinal))
        {
            return Invalid();
        }

        string ascii;
        try
        {
            ascii = Idn.GetAscii(value);
        }
        catch (ArgumentException)
        {
            return Invalid();
        }

        if (ascii.Length > FieldLengths.HostName || Uri.CheckHostName(ascii) != UriHostNameType.Dns || !ascii.Contains('.', StringComparison.Ordinal))
        {
            return Invalid();
        }

        var isLocalhost = ascii == "localhost" || ascii.EndsWith(".localhost", StringComparison.Ordinal);
        if (isLocalhost && !allowLocalhost)
        {
            return Invalid();
        }

        var platform = baseDomain.ToLowerInvariant();
        if (ascii == platform || ascii.EndsWith($".{platform}", StringComparison.Ordinal))
        {
            return Error.Validation("customdomain.platform_domain", "Addresses under the platform domain are already available as subdomain.");
        }

        return ascii;
    }

    private static Error Invalid() =>
        Error.Validation("customdomain.invalid", "Please enter a domain like ostern.example.ch (no IP address).");
}
