using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Oschterhas.Api.Controllers;

namespace Oschterhas.Api.Tests.Controllers;

public class AuthControllerTests
{
    [Theory]
    [InlineData("/instanzen", "/instanzen")]
    [InlineData("/instanzen/42?tab=hasen", "/instanzen/42?tab=hasen")]
    [InlineData(null, "/")]
    [InlineData("https://evil.example/phish", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    public void Login_OnlyRedirectsToLocalPaths(string? returnUrl, string expected)
    {
        var controller = new AuthController(Substitute.For<IAntiforgery>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.Url = new UrlHelper(new ActionContext(controller.HttpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));

        var result = Assert.IsType<ChallengeResult>(controller.Login(returnUrl));

        Assert.Equal(expected, result.Properties!.RedirectUri);
        Assert.Equal(["OpenIdConnect"], result.AuthenticationSchemes);
    }
}
