using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Oschterhas.Api.Http;

namespace Oschterhas.Api.Tests.Http;

public class SecurityHeadersTests
{
    [Fact]
    public void Csp_AllowsOnlyOwnScriptsAndTheMapTiles()
    {
        var csp = SecurityHeaders.ContentSecurityPolicy("https://identity.peakcode.dev", upgradeInsecureRequests: true);

        Assert.Contains("script-src 'self';", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", csp, StringComparison.Ordinal);
        Assert.Contains("img-src 'self' data: https://wmts.geo.admin.ch https://tile.openstreetmap.org", csp, StringComparison.Ordinal);
        Assert.Contains("connect-src 'self' https://wmts.geo.admin.ch https://tile.openstreetmap.org;", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("object-src 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("form-action 'self' https://identity.peakcode.dev", csp, StringComparison.Ordinal);
        Assert.EndsWith("upgrade-insecure-requests", csp, StringComparison.Ordinal);
    }

    [Fact]
    public void Csp_WithoutIdentity_AllowsFormsToSelfOnly_AndNoUpgradeInDevelopment()
    {
        var csp = SecurityHeaders.ContentSecurityPolicy(null, upgradeInsecureRequests: false);

        Assert.Contains("form-action 'self'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("upgrade-insecure-requests", csp, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Middleware_SetsAllHeadersOnTheResponse()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        app.UseSecurityHeaders("http://localhost:8180", isDevelopment: true);
        app.Run(context => context.Response.WriteAsync("ok"));
        var context = new DefaultHttpContext();
        var responseFeature = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);

        await app.Build()(context);
        await responseFeature.StartAsync();

        var headers = context.Response.Headers;
        Assert.StartsWith("default-src 'self'", headers.ContentSecurityPolicy.ToString(), StringComparison.Ordinal);
        Assert.Equal("nosniff", headers.XContentTypeOptions.ToString());
        Assert.Equal("DENY", headers.XFrameOptions.ToString());
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"].ToString());
        Assert.StartsWith("geolocation=(self)", headers["Permissions-Policy"].ToString(), StringComparison.Ordinal);
        Assert.Equal("same-origin", headers["Cross-Origin-Opener-Policy"].ToString());
        Assert.Equal("same-origin", headers["Cross-Origin-Resource-Policy"].ToString());
    }

    /// <summary>DefaultHttpContext never "starts" the response; this feature runs the OnStarting callbacks.</summary>
    private sealed class StartableResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public override void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));

        public async Task StartAsync()
        {
            foreach (var (callback, state) in _onStarting)
            {
                await callback(state);
            }
        }
    }
}
