using System.Text.Json;
using System.Text.Json.Serialization;
using Oschterhas.Api.Auth;
using Oschterhas.Api.Hosting;
using Oschterhas.Api.Http;
using Oschterhas.Core;
using Oschterhas.Core.Domains;
using Oschterhas.Core.Events;
using Oschterhas.Core.Gpx;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Identity;
using Oschterhas.Core.Users;
using Oschterhas.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

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
builder.Services.AddOptions<CustomDomainOptions>().Bind(builder.Configuration.GetSection(CustomDomainOptions.SectionName));
builder.Services.AddOptions<InternalNetworkOptions>().Bind(builder.Configuration.GetSection(InternalNetworkOptions.SectionName));
builder.Services.AddHostedService<CustomDomainMonitor>();
builder.Services.AddReverseProxySupport(builder.Configuration);
builder.Services.AddWebApp();

// Same JSON settings for MVC and for the OpenAPI generator (which reads the HTTP JSON options).
static void ConfigureJson(JsonSerializerOptions json)
{
    json.Converters.Add(new JsonStringEnumConverter());
    json.NumberHandling = JsonNumberHandling.Strict;
}

builder.Services.AddControllers().AddJsonOptions(o =>
{
    ConfigureJson(o.JsonSerializerOptions);
    // Invalid JSON gets a generic 400 without parser internals (positions, expected tokens).
    o.AllowInputFormatterExceptionMessages = false;
});
builder.Services.ConfigureHttpJsonOptions(o => ConfigureJson(o.SerializerOptions));
builder.Services.AddProblemDetails();
// OpenAPI 3.0: understood by openapi-typescript and by OWASP ZAP's importer (3.1 is only partly supported there).
builder.Services.AddOpenApi(o => o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Deploy step before the new version starts: "dotnet Oschterhas.Api.dll --migrate" applies migrations and exits.
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await app.MigrateDatabaseAsync();
    return;
}

app.UseForwardedHeaders();
app.UseSecurityHeaders(IdentityOrigin(app.Configuration), app.Environment.IsDevelopment());
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    await app.MigrateDatabaseAsync();
    await app.SeedDevelopmentDataAsync();
}

app.UseWebAppFiles();

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<HostResolutionMiddleware>());

app.UseAuthentication();
app.UseMiddleware<SecurityEventLogging>();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapWebAppFallback().AllowAnonymous();

await app.RunAsync();

// Origin of Peakcode Identity (for the CSP form-action of the logout redirect).
static string? IdentityOrigin(IConfiguration configuration) =>
    Uri.TryCreate(configuration["Oidc:Authority"], UriKind.Absolute, out var authority)
        ? authority.GetLeftPart(UriPartial.Authority)
        : null;
