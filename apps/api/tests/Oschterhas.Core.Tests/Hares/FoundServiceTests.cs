using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute.ExceptionExtensions;
using Oschterhas.Core.Events;
using Oschterhas.Core.Hares;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Hares;

public class FoundServiceTests
{
    private static readonly DateTimeOffset Now = new(2027, 3, 28, 10, 15, 0, TimeSpan.Zero);

    private readonly IHareRepository _hares = Substitute.For<IHareRepository>();
    private readonly IInstanceEventBroadcaster _events = Substitute.For<IInstanceEventBroadcaster>();
    private readonly FoundService _service;
    private readonly Instance _instance = new() { Id = Guid.NewGuid(), Slug = "tann", Name = "Tann", Status = InstanceStatus.Active };
    private readonly Hare _hare;

    public FoundServiceTests()
    {
        _hare = new Hare { Id = Guid.NewGuid(), InstanceId = _instance.Id, Instance = _instance, Number = 1, Lat = 47, Lon = 8 };
        _hares.FindForUpdateAsync(_instance.Id, _hare.Id, Arg.Any<CancellationToken>()).Returns(_hare);
        _service = new FoundService(_hares, _events, new FakeTimeProvider(Now), NullLogger<FoundService>.Instance);
    }

    [Fact]
    public async Task MarksHareAsFound_SavesHistory_AndNotifiesWatchers()
    {
        var result = await MarkFound("  Lina ");

        Assert.Equal(new HareFoundDto(_hare.Id, Now, "Lina"), result.Value);
        Assert.Equal(Now, _hare.FoundAt);
        Assert.Equal("Lina", _hare.FoundBy);
        _hares.Received(1).AddFoundEvent(Arg.Is<FoundEvent>(e =>
            e.HareId == _hare.Id && e.Type == FoundEventType.Found && e.Nickname == "Lina" && e.CreatedAt == Now));
        await _hares.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _events.Received(1).Publish(_instance.Id, Arg.Is<InstanceEvent>(e =>
            e.Type == InstanceEvent.HareFound && Equals(e.Data, new HareFoundEventData(_hare.Id, Now, "Lina"))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NicknameIsOptional(string? nickname)
    {
        var result = await MarkFound(nickname);

        Assert.Null(result.Value.FoundBy);
        Assert.Null(_hare.FoundBy);
    }

    [Fact]
    public async Task UnknownHareOrHareOfAnotherInstance_IsNotFound()
    {
        var result = await _service.MarkFoundAsync(_instance.Id, Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        Assert.Equal("hare.not_found", result.Error!.Code);
        await _hares.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(InstanceStatus.Draft)]
    [InlineData(InstanceStatus.Ended)]
    public async Task OnlyActiveSearchesAcceptFinds(InstanceStatus status)
    {
        _instance.Status = status;

        var result = await MarkFound(null);

        Assert.Equal("instance.not_active", result.Error!.Code);
        Assert.Null(_hare.FoundAt);
    }

    [Fact]
    public async Task AlreadyFoundHare_IsAConflict()
    {
        _hare.FoundAt = Now.AddMinutes(-5);
        _hare.FoundBy = "Max";

        var result = await MarkFound("Lina");

        Assert.Equal("hare.already_found", result.Error!.Code);
        Assert.Equal("Max", _hare.FoundBy);
        _events.DidNotReceiveWithAnyArgs().Publish(default, default!);
    }

    [Fact]
    public async Task ConcurrentFind_IsAConflict_WithoutNotification()
    {
        _hares.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new ConcurrencyConflictException(new InvalidOperationException()));

        var result = await MarkFound("Lina");

        Assert.Equal("hare.already_found", result.Error!.Code);
        _events.DidNotReceiveWithAnyArgs().Publish(default, default!);
    }

    [Theory]
    [InlineData("12345678901234567890123456789012345678901", "hare.nickname_too_long")]
    [InlineData("Li\u0000na", "hare.nickname_invalid")]
    [InlineData("Li\nna", "hare.nickname_invalid")]
    public async Task InvalidNickname_IsRejectedBeforeLoading(string nickname, string expectedCode)
    {
        var result = await MarkFound(nickname);

        Assert.Equal(expectedCode, result.Error!.Code);
        await _hares.DidNotReceive().FindForUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NicknameWithMaximumLength_IsAccepted()
    {
        var result = await MarkFound(new string('x', 40));

        Assert.True(result.IsSuccess);
    }

    private Task<Common.Results.Result<HareFoundDto>> MarkFound(string? nickname) =>
        _service.MarkFoundAsync(_instance.Id, _hare.Id, nickname, TestContext.Current.CancellationToken);
}
