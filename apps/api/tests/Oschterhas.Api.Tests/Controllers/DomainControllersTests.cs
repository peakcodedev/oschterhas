using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Controllers;
using Oschterhas.Core.Domains;

namespace Oschterhas.Api.Tests.Controllers;

public class DomainControllersTests
{
    private readonly ICustomDomainService _service = Substitute.For<ICustomDomainService>();

    [Fact]
    public async Task Verification_AnswersTokenOfTheRequestHost()
    {
        _service.GetVerificationTokenAsync("ostern.ceviduernten.ch", Arg.Any<CancellationToken>()).Returns("token-1");
        var controller = Create(new DomainVerificationController(_service), "ostern.ceviduernten.ch");

        var result = await controller.Get(TestContext.Current.CancellationToken);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("token-1", content.Content);
        Assert.Equal("text/plain", content.ContentType);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Verification_UnknownHost_IsNotFound()
    {
        _service.GetVerificationTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var controller = Create(new DomainVerificationController(_service), "fremd.example.ch");

        Assert.IsType<NotFoundResult>(await controller.Get(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true, typeof(OkResult))]
    [InlineData(false, typeof(NotFoundResult))]
    public async Task TlsAsk_AnswersFromTheService(bool allowed, Type expected)
    {
        _service.IsTlsAllowedAsync("ostern.ceviduernten.ch", Arg.Any<CancellationToken>()).Returns(allowed);
        var controller = Create(new InternalTlsController(_service), "localhost");

        var result = await controller.Allowed("ostern.ceviduernten.ch", TestContext.Current.CancellationToken);

        Assert.IsType(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task TlsAsk_WithoutDomain_IsNotFound(string? domain)
    {
        var controller = Create(new InternalTlsController(_service), "localhost");

        Assert.IsType<NotFoundResult>(await controller.Allowed(domain, TestContext.Current.CancellationToken));
        await _service.DidNotReceive().IsTlsAllowedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static T Create<T>(T controller, string host)
        where T : ControllerBase
    {
        var http = new DefaultHttpContext();
        http.Request.Host = new HostString(host);
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }
}
