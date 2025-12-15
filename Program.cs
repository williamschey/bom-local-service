using BomLocalService.Services;
using BomLocalService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// AddControllersWithViews includes AddControllers, so we use it for both MVC and API controllers
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure CORS - MUST be added before other services
var corsOrigins = builder.Configuration.GetValue<string>("Cors:AllowedOrigins", "*");
var corsMethods = builder.Configuration.GetValue<string>("Cors:AllowedMethods", "GET,POST,OPTIONS");
var corsHeaders = builder.Configuration.GetValue<string>("Cors:AllowedHeaders", "*");
var corsAllowCredentials = builder.Configuration.GetValue<bool>("Cors:AllowCredentials", false);

// Support environment variable override (comma-separated for multiple origins)
var corsOriginsEnv = Environment.GetEnvironmentVariable("CORS__ALLOWEDORIGINS");
if (!string.IsNullOrEmpty(corsOriginsEnv))
{
    corsOrigins = corsOriginsEnv;
}

var corsMethodsEnv = Environment.GetEnvironmentVariable("CORS__ALLOWEDMETHODS");
if (!string.IsNullOrEmpty(corsMethodsEnv))
{
    corsMethods = corsMethodsEnv;
}

var corsHeadersEnv = Environment.GetEnvironmentVariable("CORS__ALLOWEDHEADERS");
if (!string.IsNullOrEmpty(corsHeadersEnv))
{
    corsHeaders = corsHeadersEnv;
}

var corsAllowCredentialsEnv = Environment.GetEnvironmentVariable("CORS__ALLOWCREDENTIALS");
if (!string.IsNullOrEmpty(corsAllowCredentialsEnv) && bool.TryParse(corsAllowCredentialsEnv, out var parsedCredentials))
{
    corsAllowCredentials = parsedCredentials;
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins == "*")
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            // Split comma-separated origins
            var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            policy.WithOrigins(origins);
        }

        // Split comma-separated methods
        var methods = corsMethods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        policy.WithMethods(methods);

        // Split comma-separated headers or allow all
        if (corsHeaders == "*")
        {
            policy.AllowAnyHeader();
        }
        else
        {
            var headers = corsHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            policy.WithHeaders(headers);
        }

        if (corsAllowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});

// Register core services via interfaces (order matters - dependencies must be registered first)
builder.Services.AddSingleton<IDebugService, DebugService>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<ITimeParsingService, TimeParsingService>();
builder.Services.AddSingleton<IBrowserService, BrowserService>();
builder.Services.AddSingleton<IScrapingService, ScrapingService>();

// Register BOM Radar Service as singleton (orchestrator, depends on all above services)
builder.Services.AddSingleton<IBomRadarService, BomLocalService.Services.BomRadarService>();

// Register background services (order matters - management service needs radar service)
builder.Services.AddHostedService<CacheCleanupService>();
builder.Services.AddHostedService<CacheManagementService>();

// Add configuration - appsettings.json provides default values
// All values can be overridden via environment variables
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS redirection is optional and disabled by default for Docker flexibility
// Users can enable it by setting ENABLE_HTTPS_REDIRECTION=true environment variable
// or EnableHttpsRedirection=true in appsettings.json
var enableHttpsRedirection = builder.Configuration.GetValue<bool>("EnableHttpsRedirection", false) ||
                             Environment.GetEnvironmentVariable("ENABLE_HTTPS_REDIRECTION")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

// Enable routing (required for CORS to work with controllers)
app.UseRouting();

// CORS middleware - MUST be after UseRouting but before MapControllers
app.UseCors();

// No authorization required - service is designed to run behind a reverse proxy if auth is needed

// Map MVC routes first (before API routes to avoid conflicts)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=RadarTest}/{action=Index}/{suburb?}/{state?}");

// Map API controllers (with /api prefix)
app.MapControllers();

// Cleanup incomplete cache folders from previous crashes/restarts before starting services
var cacheService = app.Services.GetRequiredService<ICacheService>();
var deletedCount = cacheService.CleanupIncompleteCacheFolders();
if (deletedCount > 0)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Startup recovery: cleaned up {Count} incomplete cache folder(s) from previous session", deletedCount);
}

// Cleanup on shutdown
app.Lifetime.ApplicationStopped.Register(() =>
{
    var service = app.Services.GetService<IBomRadarService>();
    if (service is IDisposable disposable)
    {
        disposable.Dispose();
    }
});

app.Run();
