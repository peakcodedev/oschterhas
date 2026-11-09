using Microsoft.EntityFrameworkCore;
using Oschterhas.Core.Gpx;
using Oschterhas.Data;
using Oschterhas.Data.Entities;

namespace Oschterhas.Api.Hosting;

/// <summary>
/// Fills an empty development database with sample instances. Development only, never in production.
/// <list type="bullet">
/// <item><c>tann</c>: active, hares from samples/sample_data_2.gpx, custom domain <c>ostern.cevi-test.localhost</c></item>
/// <item><c>vorschau</c>: draft (search not started yet)</item>
/// <item><c>vorbei</c>: ended</item>
/// </list>
/// </summary>
internal static class DevelopmentSeeder
{
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var ct = app.Lifetime.ApplicationStopping;

        if (await db.Instances.AnyAsync(ct))
        {
            return;
        }

        var samples = Path.GetFullPath(app.Configuration["DevelopmentSeed:SamplesDirectory"] ?? "../../../../samples", app.Environment.ContentRootPath);
        var tannWaypoints = ReadWaypoints(Path.Combine(samples, "sample_data_2.gpx"));
        var exampleWaypoints = ReadWaypoints(Path.Combine(samples, "beispiel-hasen.gpx"));

        var tann = CreateInstance("tann", "Ostersuche Tann (Beispiel)", InstanceStatus.Active, tannWaypoints, now, found: [2, 5, 9]);
        tann.Description = "Beispieldaten aus einem swisstopo-Export.";
        tann.CustomDomain = "ostern.cevi-test.localhost";
        tann.CustomDomainStatus = CustomDomainStatus.Verified;
        tann.CustomDomainVerifiedAt = now;

        db.Instances.AddRange(
            tann,
            CreateInstance("vorschau", "Ostersuche Vorschau", InstanceStatus.Draft, exampleWaypoints, now, found: []),
            CreateInstance("vorbei", "Ostersuche 2026", InstanceStatus.Ended, exampleWaypoints, now, found: [1, 2, 3]));

        await db.SaveChangesAsync(ct);
        app.Logger.LogInformation("Development seed: instances tann, vorschau and vorbei created");
    }

    private static IReadOnlyList<GpxWaypoint> ReadWaypoints(string path)
    {
        using var file = File.OpenRead(path);
        var result = GpxParser.Parse(file, new GpxParserOptions());
        return result.IsSuccess
            ? result.Value.Waypoints
            : throw new InvalidOperationException($"Seed file {path}: {result.Error!.Message}");
    }

    private static Instance CreateInstance(
        string slug,
        string name,
        InstanceStatus status,
        IReadOnlyList<GpxWaypoint> waypoints,
        DateTimeOffset now,
        int[] found) => new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            Status = status,
            EventDate = new DateOnly(2027, 3, 28),
            CreatedAt = now,
            UpdatedAt = now,
            Hares = waypoints.Select((w, index) =>
            {
                var number = index + 1;
                var isFound = found.Contains(number);
                return new Hare
                {
                    Id = Guid.NewGuid(),
                    Number = number,
                    Name = w.Name,
                    Description = w.Description,
                    Lat = w.Lat,
                    Lon = w.Lon,
                    Elevation = w.Elevation,
                    SourceWaypointId = w.SourceWaypointId,
                    FoundAt = isFound ? now.AddMinutes(-number) : null,
                    FoundBy = isFound ? "Lina" : null,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
            }).ToList(),
        };
}
