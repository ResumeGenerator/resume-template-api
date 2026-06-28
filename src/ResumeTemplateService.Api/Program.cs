using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using HealthChecks.MongoDb;
using ResumeTemplateService.Api.Extensions;
using ResumeTemplateService.Api.Middleware;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

// Configuration
var mongoConnectionString = FirstConfiguredValue(builder.Configuration,
        builder.Configuration.GetSection("MongoDB:ConnectionString").Value,
        builder.Configuration.GetConnectionString("MongoDB"),
        builder.Configuration["MONGO_URL"],
        builder.Configuration["MONGODB_URL"],
        builder.Configuration["MONGODB_URI"],
        builder.Configuration["DATABASE_URL"])
    ?? throw new InvalidOperationException("MongoDB connection string not configured.");
var databaseName = FirstConfiguredValue(builder.Configuration,
        builder.Configuration.GetSection("MongoDB:DatabaseName").Value,
        builder.Configuration["MONGODB_DATABASE"],
        builder.Configuration["MONGO_DATABASE"],
        builder.Configuration["MONGO_INITDB_DATABASE"])
    ?? throw new InvalidOperationException("MongoDB database name not configured.");
var collectionName = FirstConfiguredValue(builder.Configuration,
        builder.Configuration.GetSection("MongoDB:CollectionName").Value,
        builder.Configuration["MONGODB_COLLECTION"],
        builder.Configuration["MONGO_COLLECTION"])
    ?? throw new InvalidOperationException("MongoDB collection name not configured.");
var editedCollectionName = FirstConfiguredValue(builder.Configuration,
    builder.Configuration.GetSection("MongoDB:EditedCollectionName").Value,
    builder.Configuration["MONGODB_EDITED_COLLECTION"],
    builder.Configuration["MONGO_EDITED_COLLECTION"]);
var configuredTemplateBasePath = builder.Configuration.GetSection("Templates:BasePath").Value;
var templateBasePath = ResolveTemplateBasePath(configuredTemplateBasePath, builder.Environment.ContentRootPath);
var chromiumExecutablePath = builder.Configuration.GetSection("Pdf:ChromiumExecutablePath").Value;
var allowedCorsOrigins = ResolveAllowedCorsOrigins(builder.Configuration);

// Services
builder.Services.AddControllers();

// Logging
builder.Services.AddLogging(configure =>
{
    configure.ClearProviders();
    configure.AddConsole();
    if (builder.Environment.IsDevelopment())
    {
        configure.AddDebug();
    }
});

// Application Services
builder.Services.AddApplicationServices();

// Infrastructure Services
builder.Services.AddInfrastructureServices(
    mongoConnectionString,
    databaseName,
    collectionName,
    editedCollectionName,
    templateBasePath,
    chromiumExecutablePath);

// Health Checks
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnectionString, name: "mongodb", tags: new[] { "db" });

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Resume Template Service API",
        Version = "v1",
        Description = "API for rendering resume templates with MongoDB integration",
        Contact = new OpenApiContact
        {
            Name = "Development Team"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.MapType<JsonElement>(() => new OpenApiSchema
    {
        Type = "object",
        AdditionalPropertiesAllowed = true,
        Description = "Flexible JSON object"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policyBuilder =>
    {
        policyBuilder
            .SetIsOriginAllowed(origin =>
            {
                var normalizedOrigin = NormalizeCorsOrigin(origin);
                return normalizedOrigin is not null &&
                    allowedCorsOrigins.Contains(normalizedOrigin, StringComparer.OrdinalIgnoreCase);
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();
app.Logger.LogInformation(
    "Runtime URLs - PORT: {Port}, ASPNETCORE_URLS: {AspNetCoreUrls}",
    railwayPort,
    Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "(not set)");
app.Logger.LogInformation("Allowed CORS origins: {AllowedCorsOrigins}", string.Join(", ", allowedCorsOrigins));

// Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Routing
app.UseRouting();

app.MapGet("/", () => Results.Ok("ResumeTemplateService API running"));

// CORS
app.UseCors("AllowAngularApp");

// Health Checks
app.UseHealthChecksEndpoint();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/swagger", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/swagger/");
        return;
    }

    if (HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});

// Swagger
app.UseSwaggerSetup();

// Authorization
app.UseAuthorization();

// Endpoints
app.MapGet("/live", () => Results.Ok(new { status = "alive" }));
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

static string ResolveTemplateBasePath(string? configuredPath, string contentRootPath)
{
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        return Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(contentRootPath, configuredPath));
    }

    var contentRootTemplatesPath = Path.Combine(contentRootPath, "templates");
    if (Directory.Exists(contentRootTemplatesPath))
    {
        return contentRootTemplatesPath;
    }

    return Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", "templates"));
}

static string[] ResolveAllowedCorsOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? Array.Empty<string>();

    var environmentOrigins = new[]
        {
            configuration["ALLOWED_ORIGIN"],
            configuration["ALLOWED_ORIGINS"],
            configuration["CORS_ALLOWED_ORIGIN"],
            configuration["CORS_ALLOWED_ORIGINS"],
            configuration["FRONTEND_URL"],
            configuration["CLIENT_URL"]
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .SelectMany(value => value!.Split(',', ';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

    return configuredOrigins
        .Concat(environmentOrigins)
        .Select(NormalizeCorsOrigin)
        .Where(origin => origin is not null)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray()!;
}

static string? NormalizeCorsOrigin(string? origin)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return null;
    }

    return origin.Trim().Trim('"', '\'').TrimEnd('/');
}

static string? FirstConfiguredValue(IConfiguration configuration, params string?[] values)
{
    foreach (var value in values)
    {
        var resolvedValue = ResolveConfigurationPlaceholder(configuration, value);
        if (!string.IsNullOrWhiteSpace(resolvedValue))
        {
            return resolvedValue;
        }
    }

    return null;
}

static string? ResolveConfigurationPlaceholder(IConfiguration configuration, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    if (!value.StartsWith("${", StringComparison.Ordinal) || !value.EndsWith("}", StringComparison.Ordinal))
    {
        return value;
    }

    var variableName = value[2..^1];
    var resolvedValue = configuration[variableName] ?? Environment.GetEnvironmentVariable(variableName);
    return string.IsNullOrWhiteSpace(resolvedValue) ? null : resolvedValue;
}
