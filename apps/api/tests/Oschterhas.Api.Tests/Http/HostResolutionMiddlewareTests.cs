using Microsoft.AspNetCore.Http;
using Oschterhas.Api.Http;
using Oschterhas.Core.Hosting;

namespace Oschterhas.Api.Tests.Http;

public class HostResolutionMiddlewareTests
{
    [Fact]
    public async Task StoresResolutionOfHostWithoutPort_AndCallsNext()
    {
        var instanceId = Guid.NewGuid();
        var resolver = Substitute.For<IHostResolver>();
        resolver.ResolveAsync("tann.oschterhas.localhost", Arg.Any<CancellationToken>())
            .Returns(HostResolution.ForInstance(instanceId));
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("tann.oschterhas.localhost", 4200);
        HostResolution? seenByNext = null;
        var middleware = new HostResolutionMiddleware(ctx =>
        {
            seenByNext = ctx.GetHostResolution();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, resolver);

        Assert.Equal(HostResolution.ForInstance(instanceId), seenByNext);
    }

    [Fact]
    public void GetHostResolution_IsUnknown_WhenMiddlewareDidNotRun()
    {
        Assert.Equal(HostResolution.Unknown, new DefaultHttpContext().GetHostResolution());
    }
}
