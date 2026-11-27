using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.ExceptionExtensions;
using Oschterhas.Api.Http;

namespace Oschterhas.Api.Tests.Http;

public class ValidateCsrfTokenAttributeTests
{
    private readonly IAntiforgery _antiforgery = Substitute.For<IAntiforgery>();

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task SafeMethods_AreNotChecked(string method)
    {
        var context = CreateContext(method);

        await new ValidateCsrfTokenAttribute().OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        await _antiforgery.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task ChangingMethods_WithValidToken_Pass(string method)
    {
        var context = CreateContext(method);

        await new ValidateCsrfTokenAttribute().OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        await _antiforgery.Received(1).ValidateRequestAsync(context.HttpContext);
    }

    [Fact]
    public async Task InvalidToken_IsRejectedWith403()
    {
        _antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).ThrowsAsync(new AntiforgeryValidationException("missing"));
        var context = CreateContext("POST");

        await new ValidateCsrfTokenAttribute().OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("csrf_invalid", Assert.IsType<ProblemDetails>(result.Value).Extensions["code"]);
    }

    private AuthorizationFilterContext CreateContext(string method)
    {
        var services = new ServiceCollection().AddSingleton(_antiforgery).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = method;
        return new AuthorizationFilterContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), []);
    }
}
