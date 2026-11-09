using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Auth;
using Oschterhas.Api.Controllers;
using Oschterhas.Common.Results;
using Oschterhas.Core.Admin;
using Oschterhas.Core.Users;
using Oschterhas.Data.Entities;

namespace Oschterhas.Api.Tests.Controllers;

public class InstancesControllerTests
{
    private static readonly CurrentUser Pia = new(Guid.NewGuid(), "pia@test", "Pia", IsPlatformAdmin: true);
    private readonly IInstanceAdminService _service = Substitute.For<IInstanceAdminService>();

    [Fact]
    public async Task PassesTheSignedInUserToTheService()
    {
        _service.ListAsync(Pia, Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<InstanceSummaryDto>>.Success([]));

        var response = await CreateController().List(TestContext.Current.CancellationToken);

        Assert.NotNull(response.Value);
        await _service.Received(1).ListAsync(Pia, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Returns201WithLocation()
    {
        var dto = new InstanceDetailDto(Guid.NewGuid(), "neu", "Neu", null, null, InstanceStatus.Draft, 0, 0, "u", "u", null, CustomDomainStatus.None, []);
        _service.CreateAsync(Pia, Arg.Any<CreateInstanceRequest>(), Arg.Any<CancellationToken>()).Returns(Result<InstanceDetailDto>.Success(dto));

        var response = await CreateController().Create(new("neu", "Neu", null, null), TestContext.Current.CancellationToken);

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Same(dto, created.Value);
        Assert.Equal(dto.Id, created.RouteValues!["instanceId"]);
    }

    [Theory]
    [InlineData(ErrorKind.Forbidden, 403)]
    [InlineData(ErrorKind.Conflict, 409)]
    [InlineData(ErrorKind.Validation, 422)]
    public async Task MapsServiceErrors(ErrorKind kind, int status)
    {
        _service.CreateAsync(Pia, Arg.Any<CreateInstanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<InstanceDetailDto>.Failure(new Error("x", "y", kind)));

        var response = await CreateController().Create(new("neu", "Neu", null, null), TestContext.Current.CancellationToken);

        Assert.Equal(status, Assert.IsType<ObjectResult>(response.Result).StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204()
    {
        _service.DeleteAsync(Pia, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var response = await CreateController().Delete(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(response);
    }

    private InstancesController CreateController()
    {
        var http = new DefaultHttpContext();
        http.Features.Set(new CurrentUserFeature(Pia));
        return new InstancesController(_service) { ControllerContext = new ControllerContext { HttpContext = http } };
    }
}
