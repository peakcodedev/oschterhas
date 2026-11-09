using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Controllers;
using Oschterhas.Api.Http;
using Oschterhas.Common.Results;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Public;
using Oschterhas.Data.Entities;

namespace Oschterhas.Api.Tests.Controllers;

public class PublicControllerTests
{
    private readonly IPublicInstanceService _service = Substitute.For<IPublicInstanceService>();

    [Fact]
    public async Task ReturnsInstanceOfResolvedHost()
    {
        var instanceId = Guid.NewGuid();
        var dto = new PublicInstanceDto("Tann", null, null, InstanceStatus.Active, 0, 0, []);
        _service.GetAsync(instanceId, Arg.Any<CancellationToken>()).Returns(Result<PublicInstanceDto>.Success(dto));

        var response = await CreateController(HostResolution.ForInstance(instanceId))
            .GetInstance(TestContext.Current.CancellationToken);

        Assert.Same(dto, response.Value);
    }

    [Theory]
    [MemberData(nameof(NonInstanceHosts))]
    public async Task ReturnsNotFound_ForHostsWithoutInstance(HostKind kind)
    {
        var response = await CreateController(new HostResolution(kind))
            .GetInstance(TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(404, problem.StatusCode);
        await _service.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PassesServiceErrorsOn()
    {
        _service.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<PublicInstanceDto>.Failure(Error.NotFound("instance.not_found", "gone")));

        var response = await CreateController(HostResolution.ForInstance(Guid.NewGuid()))
            .GetInstance(TestContext.Current.CancellationToken);

        Assert.Equal(404, Assert.IsType<ObjectResult>(response.Result).StatusCode);
    }

    public static TheoryData<HostKind> NonInstanceHosts => [HostKind.Unknown, HostKind.Admin, HostKind.Landing];

    private PublicController CreateController(HostResolution resolution)
    {
        var context = new DefaultHttpContext();
        context.Features.Set(new HostResolutionFeature(resolution));
        return new PublicController(_service) { ControllerContext = new ControllerContext { HttpContext = context } };
    }
}
