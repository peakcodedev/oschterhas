using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute.ExceptionExtensions;
using Oschterhas.Core.Admin;
using Oschterhas.Core.Events;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Admin;

public class HareAdminServiceTests
{
    private static readonly DateTimeOffset Now = new(2027, 3, 28, 12, 0, 0, TimeSpan.Zero);
    private readonly IInstanceRepository _instances = Substitute.For<IInstanceRepository>();
    private readonly IHareRepository _hares = Substitute.For<IHareRepository>();
    private readonly IInstanceEventBroadcaster _events = Substitute.For<IInstanceEventBroadcaster>();
    private readonly HareAdminService _service;
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly Hare _hare;

    public HareAdminServiceTests()
    {
        _instances.IsOrganizerAsync(_instanceId, TestData.Otto.Id, Arg.Any<CancellationToken>()).Returns(true);
        _instances.ExistsAsync(_instanceId, Arg.Any<CancellationToken>()).Returns(true);
        _hare = new Hare { Id = Guid.NewGuid(), InstanceId = _instanceId, Number = 4, Lat = 47, Lon = 8, FoundAt = Now.AddHours(-1), FoundBy = "Lina" };
        _hares.FindForUpdateAsync(_instanceId, _hare.Id, Arg.Any<CancellationToken>()).Returns(_hare);
        _hares.ListForUpdateAsync(_instanceId, Arg.Any<CancellationToken>()).Returns([_hare]);
        _service = new HareAdminService(new InstanceAccess(_instances), _hares, _events, new FakeTimeProvider(Now), NullLogger<HareAdminService>.Instance);
    }

    [Fact]
    public async Task Stranger_CannotSeeOrChangeHares()
    {
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal("instance.not_found", (await _service.ListAsync(TestData.Stranger, _instanceId, ct)).Error!.Code);
        Assert.Equal("instance.not_found", (await _service.ResetAsync(TestData.Stranger, _instanceId, _hare.Id, ct)).Error!.Code);
        Assert.Equal("Lina", _hare.FoundBy);
    }

    [Fact]
    public async Task PlatformAdmin_CanManageEveryExistingInstance()
    {
        var result = await _service.ListAsync(TestData.Admin, _instanceId, TestContext.Current.CancellationToken);

        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Create_GetsNextNumber_AndNotifiesSearchers()
    {
        var dto = (await _service.CreateAsync(TestData.Otto, _instanceId, new(" Neu ", null, 47.1, 8.1), TestContext.Current.CancellationToken)).Value;

        Assert.Equal(5, dto.Number);
        Assert.Equal("Neu", dto.Name);
        _events.Received(1).Publish(_instanceId, Arg.Is<InstanceEvent>(e => e.Type == InstanceEvent.HaresChanged));
    }

    [Fact]
    public async Task Update_MovesAndRenames_EmptyNameMeansDefaultName()
    {
        var dto = (await _service.UpdateAsync(TestData.Otto, _instanceId, _hare.Id, new("  ", "Beim Brunnen", 47.5, 8.5), TestContext.Current.CancellationToken)).Value;

        Assert.Null(dto.Name);
        Assert.Equal("Beim Brunnen", dto.Description);
        Assert.Equal((47.5, 8.5), (_hare.Lat, _hare.Lon));
    }

    [Theory]
    [InlineData(91, 8)]
    [InlineData(47, 181)]
    [InlineData(double.NaN, 8)]
    public async Task Update_RejectsInvalidCoordinates(double lat, double lon)
    {
        var result = await _service.UpdateAsync(TestData.Otto, _instanceId, _hare.Id, new(null, null, lat, lon), TestContext.Current.CancellationToken);

        Assert.Equal("hare.coordinates_invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Update_ConcurrentChange_IsAConflict()
    {
        _hares.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new ConcurrencyConflictException(new InvalidOperationException()));

        var result = await _service.UpdateAsync(TestData.Otto, _instanceId, _hare.Id, new(null, null, 47, 8), TestContext.Current.CancellationToken);

        Assert.Equal("hare.changed", result.Error!.Code);
        _events.DidNotReceiveWithAnyArgs().Publish(default, default!);
    }

    [Fact]
    public async Task Reset_ClearsFound_RecordsWhoDidIt_AndNotifiesSearchers()
    {
        var dto = (await _service.ResetAsync(TestData.Otto, _instanceId, _hare.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Null(dto.FoundAt);
        Assert.Null(dto.FoundBy);
        _hares.Received(1).AddFoundEvent(Arg.Is<FoundEvent>(e => e.Type == FoundEventType.Reset && e.UserId == TestData.Otto.Id));
        _events.Received(1).Publish(_instanceId, Arg.Is<InstanceEvent>(e => e.Type == InstanceEvent.HareReset));
    }

    [Fact]
    public async Task Delete_RemovesHare()
    {
        var result = await _service.DeleteAsync(TestData.Otto, _instanceId, _hare.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        _hares.Received(1).Remove(_hare);
    }
}
