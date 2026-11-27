using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Oschterhas.Api.Http;
using Oschterhas.Core.Hosting;

namespace Oschterhas.Api.Tests.Http;

public class RequireAdminHostAttributeTests
{
    [Fact]
    public void AllowsAdminHost()
    {
        var context = CreateContext(HostResolution.Admin);

        new RequireAdminHostAttribute().OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [MemberData(nameof(OtherHosts))]
    public void BlocksAllOtherHosts(HostResolution resolution)
    {
        var context = CreateContext(resolution);

        new RequireAdminHostAttribute().OnActionExecuting(context);

        Assert.Equal(404, Assert.IsType<ObjectResult>(context.Result).StatusCode);
    }

    public static TheoryData<HostResolution> OtherHosts =>
        [HostResolution.Unknown, HostResolution.Landing, HostResolution.ForInstance(Guid.NewGuid())];

    private static ActionExecutingContext CreateContext(HostResolution resolution)
    {
        var http = new DefaultHttpContext();
        http.Features.Set(new HostResolutionFeature(resolution));
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(action, [], new Dictionary<string, object?>(), controller: new object());
    }
}
