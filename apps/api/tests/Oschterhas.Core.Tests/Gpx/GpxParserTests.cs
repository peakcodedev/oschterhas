using System.Text;
using Oschterhas.Core.Gpx;

namespace Oschterhas.Core.Tests.Gpx;

public class GpxParserTests
{
    private const string Gpx11 = "http://www.topografix.com/GPX/1/1";
    private const string Swisstopo = "https://prod-static.swisstopo-app.ch/xmlschemas/SwisstopoExtensions";

    private static readonly GpxParserOptions Options = new();

    [Fact]
    public void RealSwisstopoExport_IsParsedCompletely()
    {
        var result = ParseFile("sample_data_2.gpx");

        Assert.True(result.IsSuccess, result.Error?.Message);
        var waypoints = result.Value.Waypoints;
        Assert.Equal(65, waypoints.Count);
        Assert.Empty(result.Value.Warnings);

        // 63 waypoints carry the swisstopo default name "Wegpunkt" and get no name.
        Assert.Equal(63, waypoints.Count(w => w.Name is null));
        Assert.Equal(["8632 Tann", "Loch"], waypoints.Where(w => w.Name is not null).Select(w => w.Name));

        Assert.Equal(
            Enumerable.Range(101, 65).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            waypoints.Select(w => w.SourceWaypointId));

        var first = waypoints[0];
        Assert.Equal(47.2705102840, first.Lat);
        Assert.Equal(8.8527658385, first.Lon);
        Assert.Equal(514.0, first.Elevation);
        Assert.Equal("Hauptstrasse 64", first.Description);
        Assert.Equal("(ZH) - Dürnten", waypoints.Single(w => w.Name == "Loch").Description);
    }

    [Fact]
    public void ConstructedSample_CoversSpecialCases()
    {
        var result = ParseFile("beispiel-hasen.gpx");

        Assert.True(result.IsSuccess, result.Error?.Message);
        var waypoints = result.Value.Waypoints;
        Assert.Equal(5, waypoints.Count);
        Assert.Equal("Im hohlen Baumstrunk (cmt statt desc)", waypoints[2].Description);
        Assert.Null(waypoints[3].Name);
        Assert.Null(waypoints[4].SourceWaypointId);
        Assert.Equal("Goldhase", waypoints[4].Name);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(300)]
    [InlineData(1000)]
    public void AcceptsUpToTheLimit(int count)
    {
        var result = Parse(Document(Waypoints(count)));

        Assert.True(result.IsSuccess);
        Assert.Equal(count, result.Value.Waypoints.Count);
    }

    [Fact]
    public void RejectsMoreThanTheLimit()
    {
        var result = Parse(Document(Waypoints(1001)));

        Assert.Equal("gpx.too_many_waypoints", result.Error!.Code);
    }

    [Fact]
    public void LimitIsConfigurable()
    {
        var result = GpxParser.Parse(Stream(Document(Waypoints(3))), new GpxParserOptions { MaxWaypoints = 2 });

        Assert.Equal("gpx.too_many_waypoints", result.Error!.Code);
    }

    [Fact]
    public void TracksAndRoutes_AreIgnoredAndDoNotCountTowardsTheLimit()
    {
        var trackPoints = string.Concat(Enumerable.Repeat("<trkpt lat=\"47.1\" lon=\"8.1\"><ele>500</ele></trkpt>", 2000));
        var xml = Document($"""
            <trk><name>Weg</name><trkseg>{trackPoints}</trkseg></trk>
            <rte><rtept lat="47.2" lon="8.2"><name>Routenpunkt</name></rtept></rte>
            <wpt lat="47.3" lon="8.3"><name>Einziger Hase</name></wpt>
            """);

        var result = Parse(xml);

        Assert.True(result.IsSuccess);
        Assert.Equal("Einziger Hase", Assert.Single(result.Value.Waypoints).Name);
    }

    [Fact]
    public void RejectsDtd_XxeAttempt()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE gpx [ <!ENTITY xxe SYSTEM "file:///etc/passwd"> ]>
            <gpx version="1.1" xmlns="http://www.topografix.com/GPX/1/1">
              <wpt lat="47" lon="8"><name>&xxe;</name></wpt>
            </gpx>
            """;

        Assert.Equal("gpx.dtd_not_allowed", Parse(xml).Error!.Code);
    }

    [Fact]
    public void RejectsDtd_EntityExpansionAttempt()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE lolz [
              <!ENTITY lol "lol">
              <!ENTITY lol2 "&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;">
              <!ENTITY lol3 "&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;">
            ]>
            <gpx><wpt lat="47" lon="8"><name>&lol3;</name></wpt></gpx>
            """;

        Assert.Equal("gpx.dtd_not_allowed", Parse(xml).Error!.Code);
    }

    [Theory]
    [InlineData("not xml at all", "gpx.invalid_xml")]
    [InlineData("<gpx><wpt lat=\"47\" lon=\"8\">", "gpx.invalid_xml")]
    [InlineData("<kml><Placemark/></kml>", "gpx.not_gpx")]
    [InlineData("<gpx/>", "gpx.no_waypoints")]
    [InlineData("<gpx version=\"1.1\"><trk/></gpx>", "gpx.no_waypoints")]
    public void RejectsInvalidFiles(string xml, string expectedCode)
    {
        Assert.Equal(expectedCode, Parse(xml).Error!.Code);
    }

    [Theory]
    [InlineData("lon=\"8.5\"")]
    [InlineData("lat=\"47,27\" lon=\"8.5\"")]
    [InlineData("lat=\"91\" lon=\"8.5\"")]
    [InlineData("lat=\"47\" lon=\"-181\"")]
    [InlineData("lat=\"NaN\" lon=\"8.5\"")]
    [InlineData("lat=\"abc\" lon=\"8.5\"")]
    public void SkipsWaypointsWithInvalidCoordinates(string attributes)
    {
        var xml = Document($"""
            <wpt {attributes}><name>Kaputt</name></wpt>
            <wpt lat="47.1" lon="8.1"><name>Gut</name></wpt>
            """);

        var result = Parse(xml);

        Assert.True(result.IsSuccess);
        Assert.Equal("Gut", Assert.Single(result.Value.Waypoints).Name);
        var warning = Assert.Single(result.Value.Warnings);
        Assert.Equal("gpx.invalid_coordinates", warning.Code);
        Assert.Equal(1, warning.Position);
    }

    [Fact]
    public void ParsesGpx10()
    {
        const string xml = """
            <gpx version="1.0" xmlns="http://www.topografix.com/GPX/1/0">
              <wpt lat="46.9" lon="7.4"><ele>540</ele><name>Bern</name><desc>Bundesplatz</desc></wpt>
            </gpx>
            """;

        var waypoint = Assert.Single(Parse(xml).Value.Waypoints);

        Assert.Equal(new GpxWaypoint(46.9, 7.4, 540, "Bern", "Bundesplatz", null), waypoint);
    }

    [Theory]
    [InlineData($"xmlns:swisstopo=\"{Swisstopo}\"", "swisstopo")]
    [InlineData("xmlns:st=\"https://swisstopo-app.ch/xmlschemas/SwisstopoExtensions\"", "st")]
    [InlineData("xmlns:x=\"urn:other\"", "x")]
    public void ReadsWaypointIdIndependentOfNamespacePrefix(string namespaceDeclaration, string prefix)
    {
        var xml = $"""
            <gpx version="1.1" xmlns="{Gpx11}" {namespaceDeclaration}>
              <wpt lat="47" lon="8"><extensions><{prefix}:waypoint_id> 42 </{prefix}:waypoint_id></extensions></wpt>
            </gpx>
            """;

        Assert.Equal("42", Assert.Single(Parse(xml).Value.Waypoints).SourceWaypointId);
    }

    [Fact]
    public void DuplicateWaypointId_KeepsWaypointWithoutId()
    {
        var xml = Document("""
            <wpt lat="47" lon="8"><extensions><swisstopo:waypoint_id>7</swisstopo:waypoint_id></extensions></wpt>
            <wpt lat="47.1" lon="8.1"><extensions><swisstopo:waypoint_id>7</swisstopo:waypoint_id></extensions></wpt>
            """);

        var result = Parse(xml);

        Assert.Equal(["7", null], result.Value.Waypoints.Select(w => w.SourceWaypointId));
        var warning = Assert.Single(result.Value.Warnings);
        Assert.Equal("gpx.duplicate_waypoint_id", warning.Code);
        Assert.Equal(2, warning.Position);
    }

    [Theory]
    [InlineData("Wegpunkt")]
    [InlineData("wegpunkt")]
    [InlineData("  WEGPUNKT ")]
    [InlineData("")]
    [InlineData("   ")]
    public void DefaultOrEmptyNames_BecomeNull(string name)
    {
        var xml = Document($"<wpt lat=\"47\" lon=\"8\"><name>{name}</name></wpt>");

        Assert.Null(Assert.Single(Parse(xml).Value.Waypoints).Name);
    }

    [Fact]
    public void DefaultNamesAreConfigurable()
    {
        var xml = Document("<wpt lat=\"47\" lon=\"8\"><name>Waypoint</name></wpt>");
        var options = new GpxParserOptions { DefaultNames = ["Waypoint"] };

        Assert.Null(Assert.Single(GpxParser.Parse(Stream(xml), options).Value.Waypoints).Name);
    }

    [Fact]
    public void LongTexts_AreTruncatedWithWarning()
    {
        var xml = Document($"<wpt lat=\"47\" lon=\"8\"><name>{new string('n', 150)}</name><desc>{new string('d', 600)}</desc></wpt>");

        var result = Parse(xml);

        var waypoint = Assert.Single(result.Value.Waypoints);
        Assert.Equal(100, waypoint.Name!.Length);
        Assert.Equal(500, waypoint.Description!.Length);
        Assert.Equal(["gpx.name_truncated", "gpx.description_truncated"], result.Value.Warnings.Select(w => w.Code));
    }

    [Fact]
    public void HandlesEmptyWaypointElementsAndAnyChildOrder()
    {
        var xml = Document("""
            <wpt lat="47" lon="8"/>
            <wpt lat="47.1" lon="8.1">
              <extensions><swisstopo:waypoint_id>9</swisstopo:waypoint_id></extensions>
              <desc>Nach den Extensions</desc>
              <name>Zweiter</name>
              <ele>600.5</ele>
            </wpt>
            """);

        var waypoints = Parse(xml).Value.Waypoints;

        Assert.Equal(2, waypoints.Count);
        Assert.Equal(new GpxWaypoint(47.1, 8.1, 600.5, "Zweiter", "Nach den Extensions", "9"), waypoints[1]);
    }

    [Fact]
    public void DescriptionWinsOverComment()
    {
        var xml = Document("<wpt lat=\"47\" lon=\"8\"><cmt>Kommentar</cmt><desc>Beschreibung</desc></wpt>");

        Assert.Equal("Beschreibung", Assert.Single(Parse(xml).Value.Waypoints).Description);
    }

    private static Common.Results.Result<GpxParseResult> Parse(string xml) => GpxParser.Parse(Stream(xml), Options);

    private static Common.Results.Result<GpxParseResult> ParseFile(string fileName)
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Samples", fileName));
        return GpxParser.Parse(file, Options);
    }

    private static MemoryStream Stream(string xml) => new(Encoding.UTF8.GetBytes(xml));

    private static string Document(string body) => $"""
        <?xml version="1.0"?>
        <gpx version="1.1" creator="Test" xmlns="{Gpx11}" xmlns:swisstopo="{Swisstopo}">
        {body}
        </gpx>
        """;

    private static string Waypoints(int count) => string.Concat(Enumerable.Range(1, count).Select(i =>
        $"<wpt lat=\"47.{i:D4}\" lon=\"8.{i:D4}\"><name>Hase {i}</name></wpt>"));
}
