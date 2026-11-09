using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oschterhas.Common.Http;
using Oschterhas.Common.Resilience;
using Oschterhas.Core.Admin;
using Oschterhas.Core.Events;
using Oschterhas.Core.Gpx;
using Oschterhas.Core.Hares;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Identity;
using Oschterhas.Core.Public;
using Oschterhas.Core.Users;

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
        services.AddSingleton<IHostCacheInvalidator, HostCacheInvalidator>();

        services.AddOptions<AuthOptions>();
        services.AddOptions<IdentityAdminOptions>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<InstanceAccess>();
        services.AddScoped<IInstanceAdminService, InstanceAdminService>();
        services.AddScoped<IOrganizerService, OrganizerService>();
        services.AddScoped<IHareAdminService, HareAdminService>();
        services.AddScoped<IGpxImportService, GpxImportService>();
        services.AddHttpClient<IIdentityAdminClient, KeycloakAdminClient>(client => client.Timeout = TimeSpan.FromSeconds(15))
            .AddHttpMessageHandler(sp => new RetryHandler(
                new RetryOptions { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(300) },
                sp.GetRequiredService<TimeProvider>()));
        return services;
    }
}
