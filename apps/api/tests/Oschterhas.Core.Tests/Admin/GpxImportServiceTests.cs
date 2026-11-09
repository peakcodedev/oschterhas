using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Oschterhas.Core.Admin;
using Oschterhas.Core.Events;
using Oschterhas.Core.Gpx;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Tests.Admin;

public class GpxImportServiceTests
{
    private const string Gpx = """
        <gpx version="1.1" xmlns="http://www.topografix.com/GPX/1/1" xmlns:swisstopo="https://prod-static.swisstopo-app.ch/xmlschemas/SwisstopoExtensions">
          <wpt lat="47.1" lon="8.1"><name>Brunnen neu</name><extensions><swisstopo:waypoint_id>101</swisstopo:waypoint_id></extensions></wpt>
          <wpt lat="47.2" lon="8.2"><name>Wegpunkt</name><extensions><swisstopo:waypoint_id>200</swisstopo:waypoint_id></extensions></wpt>
          <wpt lat="47.3" lon="8.3"><name>Ohne Id</name></wpt>
        </gpx>
        """;

    private readonly IInstanceRepository _instances = Substitute.For<IInstanceRepository>();
    private readonly IHareRepository _hares = Substitute.For<IHareRepository>();
    private readonly IInstanceEventBroadcaster _events = Substitute.For<IInstanceEventBroadcaster>();
    private readonly GpxImportService _service;
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly List<Hare> _existing;
    private readonly List<Hare> _added = [];
    private readonly List<Hare> _removed = [];

    public GpxImportServiceTests()
    {
        _instances.IsOrganizerAsync(_instanceId, TestData.Otto.Id, Arg.Any<CancellationToken>()).Returns(true);
        _existing =
        [
            new Hare { Id = Guid.NewGuid(), InstanceId = _instanceId, Number = 1, Name = "Brunnen", Lat = 47, Lon = 8, SourceWaypointId = "101", FoundAt = DateTimeOffset.UnixEpoch, FoundBy = "Lina" },
            new Hare { Id = Guid.NewGuid(), InstanceId = _instanceId, Number = 2, Name = "Alt", Lat = 47, Lon = 8, SourceWaypointId = "102" },
        ];
        _hares.ListForUpdateAsync(_instanceId, Arg.Any<CancellationToken>()).Returns(_existing);
        _hares.When(h => h.Add(Arg.Any<Hare>())).Do(c => _added.Add(c.Arg<Hare>()));
        _hares.When(h => h.Remove(Arg.Any<Hare>())).Do(c => _removed.Add(c.Arg<Hare>()));
        _service = new GpxImportService(new InstanceAccess(_instances), _hares, _events, Options.Create(new GpxParserOptions()), new FakeTimeProvider());
    }

    [Fact]
    public async Task Preview_ComparesWithExistingHares_AndSavesNothing()
    {
        var preview = (await _service.PreviewAsync(TestData.Otto, _instanceId, Stream(Gpx), TestContext.Current.CancellationToken)).Value;

        Assert.Equal(3, preview.Waypoints.Count);
        Assert.Equal(2, preview.ExistingHares);
        Assert.Equal(1, preview.ExistingFound);
        Assert.Equal(1, preview.MatchingWaypoints);
        await _hares.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replace_DeletesAllAndNumbersFromOne()
    {
        var result = (await Import(GpxImportMode.Replace)).Value;

        Assert.Equal((3, 0, 2), (result.Created, result.Updated, result.Deleted));
        Assert.Equal(_existing, _removed);
        Assert.Equal([1, 2, 3], _added.Select(h => h.Number));
        Assert.Null(_added[1].Name);
        _events.Received(1).Publish(_instanceId, Arg.Is<InstanceEvent>(e => e.Type == InstanceEvent.HaresChanged));
    }

    [Fact]
    public async Task Merge_UpdatesByWaypointId_KeepsFoundState_AndAppendsNewOnes()
    {
        var result = (await Import(GpxImportMode.Merge)).Value;

        Assert.Equal((2, 1, 0), (result.Created, result.Updated, result.Deleted));
        var brunnen = _existing[0];
        Assert.Equal(("Brunnen neu", 47.1, 8.1), (brunnen.Name, brunnen.Lat, brunnen.Lon));
        Assert.Equal("Lina", brunnen.FoundBy);
        Assert.Equal("Alt", _existing[1].Name);
        Assert.Empty(_removed);
        Assert.Equal([3, 4], _added.Select(h => h.Number));
    }

    [Fact]
    public async Task InvalidFile_ChangesNothing()
    {
        var result = await _service.ImportAsync(TestData.Otto, _instanceId, Stream("<kml/>"), GpxImportMode.Replace, TestContext.Current.CancellationToken);

        Assert.Equal("gpx.not_gpx", result.Error!.Code);
        Assert.Empty(_removed);
        await _hares.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stranger_CannotImport()
    {
        var result = await _service.ImportAsync(TestData.Stranger, _instanceId, Stream(Gpx), GpxImportMode.Replace, TestContext.Current.CancellationToken);

        Assert.Equal("instance.not_found", result.Error!.Code);
    }

    private Task<Common.Results.Result<GpxImportResultDto>> Import(GpxImportMode mode) =>
        _service.ImportAsync(TestData.Otto, _instanceId, Stream(Gpx), mode, TestContext.Current.CancellationToken);

    private static MemoryStream Stream(string xml) => new(Encoding.UTF8.GetBytes(xml));
}
