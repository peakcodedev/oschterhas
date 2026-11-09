using Microsoft.Extensions.DependencyInjection;

namespace Oschterhas.Core;

public static class CoreServiceCollectionExtensions
{
    /// <summary>Registers the business services of Oschterhas.Core. Features add their services here.</summary>
    public static IServiceCollection AddOschterhasCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
