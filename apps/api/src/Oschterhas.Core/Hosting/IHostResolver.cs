namespace Oschterhas.Core.Hosting;

/// <summary>Maps the request host name to admin area, landing page or an instance.</summary>
public interface IHostResolver
{
    /// <param name="host">Host name without port, as sent by the client.</param>
    ValueTask<HostResolution> ResolveAsync(string host, CancellationToken cancellationToken);
}
