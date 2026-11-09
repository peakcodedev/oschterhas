using System.Text.Json;
using System.Text.Json.Serialization;
using Oschterhas.Api.Hosting;
using Oschterhas.Api.Http;
using Oschterhas.Core;
using Oschterhas.Core.Events;
using Oschterhas.Core.Gpx;
using Oschterhas.Core.Hosting;
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
    app.MapOpenApi();
    await app.MigrateDatabaseAsync();
    await app.SeedDevelopmentDataAsync();
}

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<HostResolutionMiddleware>());

app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
