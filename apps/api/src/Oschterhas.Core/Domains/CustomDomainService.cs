using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oschterhas.Common.Results;
using Oschterhas.Core.Admin;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Users;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Domains;

public interface ICustomDomainService
{
    Task<Result<InstanceDetailDto>> SetAsync(CurrentUser actor, Guid instanceId, SetCustomDomainRequest request, CancellationToken cancellationToken);

    Task<Result<CustomDomainCheckDto>> VerifyAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken);

    Task<Result<InstanceDetailDto>> RemoveAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken);

    /// <summary>Token the verification endpoint answers for this host; null if the host is no (pending) custom domain.</summary>
    Task<string?> GetVerificationTokenAsync(string host, CancellationToken cancellationToken);

    /// <summary>Whether Caddy may get a TLS certificate for this host (our domains and verified custom domains only).</summary>
    Task<bool> IsTlsAllowedAsync(string host, CancellationToken cancellationToken);

    /// <summary>Re-checks all custom domains that are due (called periodically by a background service).</summary>
    Task<int> CheckDueDomainsAsync(CancellationToken cancellationToken);
}

internal sealed class CustomDomainService(
    IInstanceRepository instances,
    IDomainVerifier verifier,
    IHostResolver hostResolver,
    IHostCacheInvalidator hostCache,
    IOptions<CustomDomainOptions> options,
    IOptions<HostingOptions> hosting,
    TimeProvider timeProvider,
    ILogger<CustomDomainService> logger) : ICustomDomainService
{
    private static readonly Error Taken = Error.Conflict("customdomain.taken", "This domain is already used by another search.");
    private static readonly Error NotSet = Error.Validation("customdomain.not_set", "This search has no custom domain.");

    public async Task<Result<InstanceDetailDto>> SetAsync(
        CurrentUser actor,
        Guid instanceId,
        SetCustomDomainRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var instance = await LoadAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (instance.IsFailure)
        {
            return instance.Error!;
        }

        var domain = CustomDomainRules.Normalize(request.Domain, hosting.Value.BaseDomain, options.Value.AllowLocalhostDomains);
        if (domain.IsFailure)
        {
            return domain.Error!;
        }

        var current = instance.Value;
        if (current.CustomDomain == domain.Value)
        {
            return current.ToDetailDto(hosting.Value);
        }

        if (await instances.CustomDomainTakenAsync(domain.Value, current.Id, cancellationToken).ConfigureAwait(false))
        {
            return Taken;
        }

        current.CustomDomain = domain.Value;
        current.CustomDomainStatus = CustomDomainStatus.Pending;
        current.CustomDomainToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        current.CustomDomainVerifiedAt = null;
        current.CustomDomainLastCheckAt = null;
        current.CustomDomainFailedChecks = 0;
        current.UpdatedAt = timeProvider.GetUtcNow();

        try
        {
            await instances.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintException)
        {
            return Taken;
        }

        hostCache.Invalidate();
        logger.LogInformation("Custom domain {Domain} set for instance {InstanceId} by {ActorId}", current.CustomDomain, current.Id, actor.Id);
        return current.ToDetailDto(hosting.Value);
    }

    public async Task<Result<CustomDomainCheckDto>> VerifyAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var instance = loaded.Value;
        if (instance.CustomDomain is null || instance.CustomDomainToken is null)
        {
            return NotSet;
        }

        var outcome = await verifier.CheckAsync(instance.CustomDomain, instance.CustomDomainToken, cancellationToken).ConfigureAwait(false);
        var changed = Apply(instance, outcome);
        await instances.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (changed)
        {
            hostCache.Invalidate();
        }

        return new CustomDomainCheckDto(instance.ToDetailDto(hosting.Value), outcome);
    }

    public async Task<Result<InstanceDetailDto>> RemoveAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var instance = loaded.Value;
        logger.LogInformation("Custom domain {Domain} removed from instance {InstanceId} by {ActorId}", instance.CustomDomain, instance.Id, actor.Id);
        instance.CustomDomain = null;
        instance.CustomDomainStatus = CustomDomainStatus.None;
        instance.CustomDomainToken = null;
        instance.CustomDomainVerifiedAt = null;
        instance.CustomDomainLastCheckAt = null;
        instance.CustomDomainFailedChecks = 0;
        instance.UpdatedAt = timeProvider.GetUtcNow();
        await instances.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        hostCache.Invalidate();
        return instance.ToDetailDto(hosting.Value);
    }

    public async Task<string?> GetVerificationTokenAsync(string host, CancellationToken cancellationToken)
    {
        var name = HostResolver.Normalize(host);
        if (name is null)
        {
            return null;
        }

        var token = await instances.FindCustomDomainTokenAsync(name, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(token) ? null : token;
    }

    public async Task<bool> IsTlsAllowedAsync(string host, CancellationToken cancellationToken)
    {
        var resolution = await hostResolver.ResolveAsync(host, cancellationToken).ConfigureAwait(false);
        return resolution.Kind != HostKind.Unknown;
    }

    public async Task<int> CheckDueDomainsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var all = await instances.ListWithCustomDomainForUpdateAsync(cancellationToken).ConfigureAwait(false);
        var checkedCount = 0;
        var changed = false;

        foreach (var instance in all.Where(i => IsDue(i, now)))
        {
            var outcome = await verifier.CheckAsync(instance.CustomDomain!, instance.CustomDomainToken ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);
            changed |= Apply(instance, outcome);
            checkedCount++;
        }

        if (checkedCount > 0)
        {
            await instances.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (changed)
        {
            hostCache.Invalidate();
        }

        return checkedCount;
    }

    private bool IsDue(Instance instance, DateTimeOffset now)
    {
        if (instance.CustomDomainLastCheckAt is not { } last)
        {
            return true;
        }

        var interval = instance.CustomDomainStatus == CustomDomainStatus.Pending
            ? options.Value.PendingCheckInterval
            : options.Value.VerifiedCheckInterval;
        return now - last >= interval;
    }

    /// <summary>Updates status and counters after a check; returns true if the served state changed.</summary>
    private bool Apply(Instance instance, DomainCheckOutcome outcome)
    {
        var now = timeProvider.GetUtcNow();
        var before = instance.CustomDomainStatus;
        instance.CustomDomainLastCheckAt = now;

        if (outcome == DomainCheckOutcome.Verified)
        {
            instance.CustomDomainFailedChecks = 0;
            if (before != CustomDomainStatus.Verified)
            {
                instance.CustomDomainStatus = CustomDomainStatus.Verified;
                instance.CustomDomainVerifiedAt = now;
            }
        }
        else
        {
            instance.CustomDomainFailedChecks++;
            if (before == CustomDomainStatus.Verified && instance.CustomDomainFailedChecks >= options.Value.MaxFailedChecks)
            {
                instance.CustomDomainStatus = CustomDomainStatus.Failed;
            }
        }

        if (before != instance.CustomDomainStatus)
        {
            logger.LogWarning(
                "Custom domain {Domain} of instance {InstanceId}: {Before} → {After} ({Outcome})",
                instance.CustomDomain,
                instance.Id,
                before,
                instance.CustomDomainStatus,
                outcome);
        }

        return before != instance.CustomDomainStatus;
    }

    private async Task<Result<Instance>> LoadAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken)
    {
        if (!actor.IsPlatformAdmin)
        {
            return InstanceAccess.PlatformAdminRequired;
        }

        var instance = await instances.FindForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        return instance is null ? InstanceAccess.NotFound : instance;
    }
}
