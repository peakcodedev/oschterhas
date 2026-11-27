using Microsoft.Extensions.Options;

namespace Oschterhas.Core.Hosting;

internal sealed class HostingOptionsValidator : IValidateOptions<HostingOptions>
{
    public ValidateOptionsResult Validate(string? name, HostingOptions options)
    {
        var failures = new List<string>();

        if (!IsLowerCaseHostName(options.BaseDomain) || !options.BaseDomain.Contains('.', StringComparison.Ordinal))
        {
            failures.Add($"{HostingOptions.SectionName}:BaseDomain must be a lower-case domain like 'oschterhas.li'.");
        }

        if (!IsLowerCaseHostName(options.AdminSubdomain) || options.AdminSubdomain.Contains('.', StringComparison.Ordinal))
        {
            failures.Add($"{HostingOptions.SectionName}:AdminSubdomain must be a single lower-case label like 'admin'.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsLowerCaseHostName(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && string.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal)
        && Uri.CheckHostName(value) == UriHostNameType.Dns;
}
