using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Controllers;
using Oschterhas.Api.Http;
using Oschterhas.Common.Results;
using Oschterhas.Core.Events;
using Oschterhas.Core.Hares;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Public;
using Oschterhas.Data.Entities;

namespace Oschterhas.Api.Tests.Controllers;

public class PublicControllerTests
{
    private readonly IPublicInstanceService _service = Substitute.For<IPublicInstanceService>();
    private readonly IFoundService _found = Substitute.For<IFoundService>();
    private readonly IInstanceEventBroadcaster _events = Substitute.For<IInstanceEventBroadcaster>();

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

    [Fact]
    public async Task MarkFound_PassesInstanceOfHostAndNickname()
    {
        var instanceId = Guid.NewGuid();
        var hareId = Guid.NewGuid();
        var dto = new HareFoundDto(hareId, DateTimeOffset.UnixEpoch, "Lina");
        _found.MarkFoundAsync(instanceId, hareId, "Lina", Arg.Any<CancellationToken>())
            .Returns(Result<HareFoundDto>.Success(dto));

        var response = await CreateController(HostResolution.ForInstance(instanceId))
            .MarkFound(hareId, new MarkFoundRequest("Lina"), TestContext.Current.CancellationToken);

        Assert.Same(dto, response.Value);
    }

    [Fact]
    public async Task MarkFound_MapsConflictTo409()
    {
        _found.MarkFoundAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<HareFoundDto>.Failure(Error.Conflict("hare.already_found", "found")));

        var response = await CreateController(HostResolution.ForInstance(Guid.NewGuid()))
            .MarkFound(Guid.NewGuid(), new MarkFoundRequest(null), TestContext.Current.CancellationToken);

        Assert.Equal(409, Assert.IsType<ObjectResult>(response.Result).StatusCode);
    }

    [Theory]
    [MemberData(nameof(NonInstanceHosts))]
    public async Task MarkFound_ReturnsNotFound_ForHostsWithoutInstance(HostKind kind)
    {
        var response = await CreateController(new HostResolution(kind))
            .MarkFound(Guid.NewGuid(), new MarkFoundRequest(null), TestContext.Current.CancellationToken);

        Assert.Equal(404, Assert.IsType<ObjectResult>(response.Result).StatusCode);
        await _found.DidNotReceive().MarkFoundAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(NonInstanceHosts))]
    public void Events_ReturnsNotFound_ForHostsWithoutInstance(HostKind kind)
    {
        var result = CreateController(new HostResolution(kind)).Events(TestContext.Current.CancellationToken);

        var json = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(404, json.StatusCode);
        _events.DidNotReceive().SubscribeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Events_SubscribesToInstanceOfHost()
    {
        var instanceId = Guid.NewGuid();

        var result = CreateController(HostResolution.ForInstance(instanceId)).Events(TestContext.Current.CancellationToken);

        Assert.Equal("ServerSentEventsResult`1", result.GetType().Name);
        _events.Received(1).SubscribeAsync(instanceId, Arg.Any<CancellationToken>());
    }

    public static TheoryData<HostKind> NonInstanceHosts => [HostKind.Unknown, HostKind.Admin, HostKind.Landing];

    private PublicController CreateController(HostResolution resolution)
    {
        var context = new DefaultHttpContext();
        context.Features.Set(new HostResolutionFeature(resolution));
        return new PublicController(_service, _found, _events) { ControllerContext = new ControllerContext { HttpContext = context } };
    }
}
