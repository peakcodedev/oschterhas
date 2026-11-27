using System.Globalization;
using System.Xml;
using Oschterhas.Common.Results;
using Oschterhas.Data;

namespace Oschterhas.Core.Gpx;

/// <summary>
/// Reads the waypoints (<c>&lt;wpt&gt;</c>) of a GPX 1.0/1.1 file. Tracks, routes and everything else are skipped.
/// Uses only <see cref="XmlReader"/> (no third-party library); DTDs are rejected, so XXE and entity expansion are impossible.
/// </summary>
public static class GpxParser
{
    public static Result<GpxParseResult> Parse(Stream gpx, GpxParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(gpx);
        ArgumentNullException.ThrowIfNull(options);

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = options.MaxCharactersInDocument,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            CloseInput = false,
        };

        try
        {
            using var reader = XmlReader.Create(gpx, settings);
            return ParseDocument(reader, options);
        }
        catch (XmlException ex) when (ex.Message.Contains("DTD", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("gpx.dtd_not_allowed", "GPX files with a DTD are not allowed.");
        }
        catch (XmlException ex)
        {
            return Error.Validation("gpx.invalid_xml", $"The file is not valid XML: {ex.Message}");
        }
    }

    private static Result<GpxParseResult> ParseDocument(XmlReader reader, GpxParserOptions options)
    {
        if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "gpx")
        {
            return Error.Validation("gpx.not_gpx", "The file is not a GPX file (root element <gpx> missing).");
        }

        var defaultNames = new HashSet<string>(options.DefaultNames, StringComparer.OrdinalIgnoreCase);
        var waypoints = new List<GpxWaypoint>();
        var warnings = new List<GpxWarning>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var position = 0;

        if (reader.IsEmptyElement)
        {
            return Error.Validation("gpx.no_waypoints", "The file contains no waypoints.");
        }

        reader.Read();
        while (reader.NodeType != XmlNodeType.EndElement && !reader.EOF)
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                reader.Read();
                continue;
            }

            if (reader.LocalName != "wpt")
            {
                reader.Skip();
                continue;
            }

            position++;
            if (position > options.MaxWaypoints)
            {
                return Error.Validation(
                    "gpx.too_many_waypoints",
                    $"The file contains more than {options.MaxWaypoints} waypoints.");
            }

            var waypoint = ReadWaypoint(reader, position, defaultNames, warnings);
            if (waypoint is null)
            {
                continue;
            }

            if (waypoint.SourceWaypointId is { } id && !seenIds.Add(id))
            {
                warnings.Add(new GpxWarning(
                    "gpx.duplicate_waypoint_id",
                    position,
                    $"waypoint_id '{id}' appears more than once; this waypoint keeps no id."));
                waypoint = waypoint with { SourceWaypointId = null };
            }

            waypoints.Add(waypoint);
        }

        if (waypoints.Count == 0)
        {
            return Error.Validation("gpx.no_waypoints", "The file contains no valid waypoints.");
        }

        return new GpxParseResult(waypoints, warnings);
    }

    /// <summary>Reads one &lt;wpt&gt; and leaves the reader after its end tag. Returns null if it is skipped.</summary>
    private static GpxWaypoint? ReadWaypoint(
        XmlReader reader,
        int position,
        HashSet<string> defaultNames,
        List<GpxWarning> warnings)
    {
        var lat = ParseDouble(reader.GetAttribute("lat"));
        var lon = ParseDouble(reader.GetAttribute("lon"));
        double? elevation = null;
        string? name = null, description = null, comment = null, waypointId = null;

        using (var wpt = reader.ReadSubtree())
        {
            wpt.Read(); // <wpt> itself
            wpt.Read(); // first child (or EOF for an empty <wpt/>)

            // ReadElementContentAsString already moves to the next node, so only call Read() otherwise.
            while (!wpt.EOF)
            {
                if (wpt.NodeType != XmlNodeType.Element)
                {
                    wpt.Read();
                    continue;
                }

                switch (wpt.LocalName)
                {
                    case "ele" when wpt.Depth == 1:
                        elevation = ParseDouble(wpt.ReadElementContentAsString());
                        break;
                    case "name" when wpt.Depth == 1:
                        name = wpt.ReadElementContentAsString();
                        break;
                    case "desc" when wpt.Depth == 1:
                        description = wpt.ReadElementContentAsString();
                        break;
                    case "cmt" when wpt.Depth == 1:
                        comment = wpt.ReadElementContentAsString();
                        break;
                    case "waypoint_id":
                        waypointId = wpt.ReadElementContentAsString();
                        break;
                    default:
                        wpt.Read(); // descend, e.g. into <extensions>
                        break;
                }
            }
        }

        // ReadSubtree leaves the outer reader on the end tag (or the empty element); move past it.
        reader.Read();

        if (lat is not (>= -90 and <= 90) || lon is not (>= -180 and <= 180))
        {
            warnings.Add(new GpxWarning(
                "gpx.invalid_coordinates",
                position,
                "Waypoint skipped: lat/lon missing, not a number or out of range."));
            return null;
        }

        name = Clean(name);
        if (name is not null && defaultNames.Contains(name))
        {
            name = null;
        }

        return new GpxWaypoint(
            lat.Value,
            lon.Value,
            elevation,
            Truncate(name, FieldLengths.HareName, "gpx.name_truncated", position, warnings),
            Truncate(Clean(description) ?? Clean(comment), FieldLengths.HareDescription, "gpx.description_truncated", position, warnings),
            Truncate(Clean(waypointId), FieldLengths.SourceWaypointId, "gpx.waypoint_id_truncated", position, warnings));
    }

    private static double? ParseDouble(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && double.IsFinite(result)
            ? result
            : null;

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string? Truncate(string? value, int maxLength, string code, int position, List<GpxWarning> warnings)
    {
        if (value is null || value.Length <= maxLength)
        {
            return value;
        }

        warnings.Add(new GpxWarning(code, position, $"Text shortened to {maxLength} characters."));
        return value[..maxLength];
    }
}
