using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Oschterhas.Core.Instances;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Hosting;

internal sealed class HostResolver(
    IOptions<HostingOptions> options,
    IInstanceRepository instances,
    IMemoryCache cache,
    IHostCacheInvalidator invalidator) : IHostResolver
{
    /// <summary>Short, so new instances and removed custom domains take effect quickly.</summary>
    internal static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly HostingOptions _options = options.Value;

    public async ValueTask<HostResolution> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        var name = Normalize(host);
        if (name is null)
        {
            return HostResolution.Unknown;
        }

        var baseDomain = _options.BaseDomain;
        if (name == baseDomain || name == $"www.{baseDomain}")
        {
            return HostResolution.Landing;
        }

        if (name == _options.AdminHost)
        {
            return HostResolution.Admin;
        }

        var cacheKey = $"host:{name}";
        if (cache.TryGetValue(cacheKey, out HostResolution? cached) && cached is not null)
        {
            return cached;
        }

        var resolution = await ResolveInstanceAsync(name, baseDomain, cancellationToken).ConfigureAwait(false);
        cache.Set(
            cacheKey,
            resolution,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }.AddExpirationToken(invalidator.ChangeToken));
        return resolution;
    }

    private async Task<HostResolution> ResolveInstanceAsync(string name, string baseDomain, CancellationToken cancellationToken)
    {
        Guid? id;
        if (name.EndsWith($".{baseDomain}", StringComparison.Ordinal))
        {
            var label = name[..^(baseDomain.Length + 1)];
            var slug = SlugRules.Normalize(label);
            if (slug.IsFailure || slug.Value != label)
            {
                return HostResolution.Unknown;
            }

            id = await instances.FindIdBySlugAsync(label, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            id = await instances.FindIdByVerifiedCustomDomainAsync(name, cancellationToken).ConfigureAwait(false);
        }

        return id is { } instanceId ? HostResolution.ForInstance(instanceId) : HostResolution.Unknown;
    }

    /// <summary>Lower-case DNS name without trailing dot; null for IP addresses or invalid names.</summary>
    internal static string? Normalize(string? host)
    {
        var name = host?.Trim().TrimEnd('.').ToLowerInvariant();
        if (string.IsNullOrEmpty(name) || name.Length > Data.FieldLengths.HostName)
        {
            return null;
        }

        return Uri.CheckHostName(name) == UriHostNameType.Dns ? name : null;
    }
}
