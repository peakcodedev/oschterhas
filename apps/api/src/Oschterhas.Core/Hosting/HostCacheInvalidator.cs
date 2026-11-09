using Microsoft.Extensions.Primitives;

namespace Oschterhas.Core.Hosting;

/// <summary>Lets changes to slugs, custom domains or deleted instances take effect immediately in <see cref="HostResolver"/>.</summary>
public interface IHostCacheInvalidator
{
    IChangeToken ChangeToken { get; }

    void Invalidate();
}

internal sealed class HostCacheInvalidator : IHostCacheInvalidator, IDisposable
{
    private CancellationTokenSource _source = new();

    public IChangeToken ChangeToken => new CancellationChangeToken(_source.Token);

    public void Invalidate()
    {
        var previous = Interlocked.Exchange(ref _source, new CancellationTokenSource());
        previous.Cancel();
        previous.Dispose();
    }

    public void Dispose() => _source.Dispose();
}
