using System.Text.Json;
using System.Text.Json.Serialization;
using Oschterhas.Api.Auth;
using Oschterhas.Api.Hosting;
using Oschterhas.Api.Http;
using Oschterhas.Core;
using Oschterhas.Core.Events;
using Oschterhas.Core.Gpx;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Identity;
using Oschterhas.Core.Users;
using Oschterhas.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<HostingOptions>()
    .Bind(builder.Configuration.GetSection(HostingOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<GpxParserOptions>()
    .Bind(builder.Configuration.GetSection(GpxParserOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);

var connectionString = builder.Configuration.GetConnectionString("Oschterhas")
    ?? throw new InvalidOperationException("Connection string 'Oschterhas' is missing.");
builder.Services.AddOschterhasData(connectionString);
builder.Services.AddOschterhasCore();
builder.Services.AddOptions<EventStreamOptions>()
    .Bind(builder.Configuration.GetSection(EventStreamOptions.SectionName));
builder.Services.AddOschterhasRateLimiting(builder.Configuration);
builder.Services.AddOptions<AuthOptions>().Bind(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddOptions<IdentityAdminOptions>()
    .Bind(builder.Configuration.GetSection(IdentityAdminOptions.SectionName))
    .PostConfigure(o =>
    {
        // Invited users must match logins: same issuer as the OIDC authority.
        o.Issuer = builder.Configuration[$"{OidcOptions.SectionName}:Authority"] ?? o.Issuer;
    });
builder.Services.AddOschterhasAuthentication(builder.Configuration, builder.Environment);

// Same JSON settings for MVC and for the OpenAPI generator (which reads the HTTP JSON options).
static void ConfigureJson(JsonSerializerOptions json)
{
    json.Converters.Add(new JsonStringEnumConverter());
    json.NumberHandling = JsonNumberHandling.Strict;
}

builder.Services.AddControllers().AddJsonOptions(o => ConfigureJson(o.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(o => ConfigureJson(o.SerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    await app.MigrateDatabaseAsync();
    await app.SeedDevelopmentDataAsync();
}

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<HostResolutionMiddleware>());

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();
