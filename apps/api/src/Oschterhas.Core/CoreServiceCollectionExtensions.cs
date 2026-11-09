using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oschterhas.Core.Events;
using Oschterhas.Core.Gpx;
using Oschterhas.Core.Hares;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Public;

namespace Oschterhas.Core;

public static class CoreServiceCollectionExtensions
{
    /// <summary>Registers the business services of Oschterhas.Core. Configuration sections are bound by the caller.</summary>
    public static IServiceCollection AddOschterhasCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMemoryCache();
        services.AddSingleton<IValidateOptions<HostingOptions>, HostingOptionsValidator>();
        services.AddOptions<GpxParserOptions>();
        services.AddScoped<IHostResolver, HostResolver>();
        services.AddScoped<IPublicInstanceService, PublicInstanceService>();
        services.AddScoped<IFoundService, FoundService>();
        services.AddOptions<EventStreamOptions>();
        services.AddSingleton<IInstanceEventBroadcaster, InMemoryInstanceEventBroadcaster>();
        return services;
    }
}
