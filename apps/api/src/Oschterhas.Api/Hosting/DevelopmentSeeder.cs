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

        if (!await db.Instances.AnyAsync(ct))
        {
            await SeedInstancesAsync(app, db, now, ct);
        }

        if (!await db.Users.AnyAsync(ct))
        {
            await SeedUsersAsync(app, db, now, ct);
        }
    }

    /// <summary>
    /// Test users of the Keycloak realm "oschterhas" (identity_peakcode/realms/oschterhas-realm.json, fixed ids).
    /// nina.neu@unbekannt.test is deliberately missing: she has no access.
    /// </summary>
    private static async Task SeedUsersAsync(WebApplication app, AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var issuer = app.Configuration["Oidc:Authority"];
        if (string.IsNullOrEmpty(issuer))
        {
            return;
        }

        AppUser User(string subject, string email, string name, bool admin) => new()
        {
            Id = Guid.NewGuid(),
            IdentityIssuer = issuer,
            IdentitySubject = subject,
            Email = email,
            DisplayName = name,
            IsPlatformAdmin = admin,
            CreatedAt = now,
        };

        var pia = User("7a2b0c10-0000-4b00-8000-000000000001", "pia.plattform@oschterhas.test", "Pia Plattform", admin: true);
        var otto = User("7a2b0c10-0000-4b00-8000-000000000002", "otto.organisator@oschterhas.test", "Otto Organisator", admin: false);
        var olga = User("7a2b0c10-0000-4b00-8000-000000000003", "olga.organisatorin@oschterhas.test", "Olga Organisatorin", admin: false);
        db.Users.AddRange(pia, otto, olga);

        var instances = await db.Instances.Where(i => i.Slug == "tann" || i.Slug == "vorschau").ToDictionaryAsync(i => i.Slug, ct);
        if (instances.TryGetValue("tann", out var tann))
        {
            db.InstanceOrganizers.Add(new InstanceOrganizer { InstanceId = tann.Id, UserId = otto.Id, CreatedAt = now });
        }

        if (instances.TryGetValue("vorschau", out var vorschau))
        {
            db.InstanceOrganizers.Add(new InstanceOrganizer { InstanceId = vorschau.Id, UserId = olga.Id, CreatedAt = now });
        }

        await db.SaveChangesAsync(ct);
        app.Logger.LogInformation("Development seed: users Pia (platform admin), Otto (tann) and Olga (vorschau) created");
    }

    private static async Task SeedInstancesAsync(WebApplication app, AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var samples = Path.GetFullPath(app.Configuration["DevelopmentSeed:SamplesDirectory"] ?? "../../../../samples", app.Environment.ContentRootPath);
        var tannWaypoints = ReadWaypoints(Path.Combine(samples, "sample_data_2.gpx"));
        var exampleWaypoints = ReadWaypoints(Path.Combine(samples, "beispiel-hasen.gpx"));

        var tann = CreateInstance("tann", "Ostersuche Tann (Beispiel)", InstanceStatus.Active, tannWaypoints, now, found: [2, 5, 9]);
        tann.Description = "Beispieldaten aus einem swisstopo-Export.";
        tann.CustomDomain = "ostern.cevi-test.localhost";
        tann.CustomDomainStatus = CustomDomainStatus.Verified;
        // A real token, so the periodic re-check (via the dev server on port 4300) succeeds.
        tann.CustomDomainToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
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
