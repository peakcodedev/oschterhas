namespace Oschterhas.Core.Gpx;

public sealed class GpxParserOptions
{
    public const string SectionName = "Gpx";

    /// <summary>More waypoints abort the import. At least 300 must work (requirement); default 1000.</summary>
    public int MaxWaypoints { get; set; } = 1000;

    /// <summary>
    /// Placeholder names that GPS apps assign automatically (swisstopo: "Wegpunkt").
    /// Such waypoints get no name; the app shows "Hase {number}" in the user's language instead.
    /// </summary>
    public IList<string> DefaultNames { get; set; } = ["Wegpunkt"];

    /// <summary>Upper limit for the XML document size in characters (protects against huge inputs).</summary>
    public long MaxCharactersInDocument { get; set; } = 10_000_000;
}
