using Oschterhas.Core.Public;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Public;

public class PublicInstanceServiceTests
{
    private readonly IInstanceRepository _instances = Substitute.For<IInstanceRepository>();
    private readonly PublicInstanceService _service;

    public PublicInstanceServiceTests() => _service = new PublicInstanceService(_instances);

    [Fact]
    public async Task UnknownInstance_IsNotFound()
    {
        var result = await _service.GetAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal("instance.not_found", result.Error!.Code);
    }

    [Theory]
    [InlineData(InstanceStatus.Active)]
    [InlineData(InstanceStatus.Ended)]
    public async Task ActiveOrEnded_ReturnsHaresOrderedByNumberWithProgress(InstanceStatus status)
    {
        var instance = CreateInstance(status);
        _instances.GetWithHaresAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var dto = (await _service.GetAsync(instance.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Equal("Ostersuche Tann", dto.Name);
        Assert.Equal(status, dto.Status);
        Assert.Equal(3, dto.TotalHares);
        Assert.Equal(1, dto.FoundHares);
        Assert.Equal([1, 2, 3], dto.Hares!.Select(h => h.Number));
        var found = dto.Hares!.Single(h => h.Number == 2);
        Assert.Equal("Lina", found.FoundBy);
        Assert.NotNull(found.FoundAt);
        Assert.Null(dto.Hares![0].Name);
    }

    [Fact]
    public async Task Draft_HidesHares_ButKeepsCounts()
    {
        var instance = CreateInstance(InstanceStatus.Draft);
        _instances.GetWithHaresAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var dto = (await _service.GetAsync(instance.Id, TestContext.Current.CancellationToken)).Value;

        Assert.Null(dto.Hares);
        Assert.Equal(3, dto.TotalHares);
    }

    private static Instance CreateInstance(InstanceStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Slug = "tann",
        Name = "Ostersuche Tann",
        Status = status,
        Hares =
        [
            new Hare { Id = Guid.NewGuid(), Number = 3, Name = "Loch", Lat = 47.3, Lon = 8.3 },
            new Hare { Id = Guid.NewGuid(), Number = 1, Name = null, Lat = 47.1, Lon = 8.1 },
            new Hare { Id = Guid.NewGuid(), Number = 2, Name = "Brunnen", Lat = 47.2, Lon = 8.2, FoundAt = DateTimeOffset.UnixEpoch, FoundBy = "Lina" },
        ],
    };
}
