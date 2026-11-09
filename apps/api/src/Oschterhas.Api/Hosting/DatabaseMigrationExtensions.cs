using Microsoft.EntityFrameworkCore;
using Oschterhas.Common.Resilience;
using Oschterhas.Data;

namespace Oschterhas.Api.Hosting;

internal static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations. Development only: in production migrations run as a separate deploy step.
    /// Retries while the database container is still starting.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var options = new RetryOptions { MaxAttempts = 6, BaseDelay = TimeSpan.FromSeconds(1) };

        await Retry.ExecuteAsync(ct => db.Database.MigrateAsync(ct), options, timeProvider, app.Lifetime.ApplicationStopping);
    }
}
