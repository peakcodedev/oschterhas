using Microsoft.Extensions.Options;
using Oschterhas.Api.Hosting;
using Oschterhas.Core;
using Oschterhas.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<HostingOptions>()
    .Bind(builder.Configuration.GetSection(HostingOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<HostingOptions>, HostingOptionsValidator>();
builder.Services.AddSingleton(TimeProvider.System);

var connectionString = builder.Configuration.GetConnectionString("Oschterhas")
    ?? throw new InvalidOperationException("Connection string 'Oschterhas' is missing.");
builder.Services.AddOschterhasData(connectionString);
builder.Services.AddOschterhasCore();

builder.Services.AddControllers();
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
}

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
