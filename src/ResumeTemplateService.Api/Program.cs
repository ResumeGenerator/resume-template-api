using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using HealthChecks.MongoDb;
using ResumeTemplateService.Api.Extensions;
using ResumeTemplateService.Api.Middleware;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var mongoConnectionString = builder.Configuration.GetSection("MongoDB:ConnectionString").Value
    ?? builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new InvalidOperationException("MongoDB connection string not configured.");
var databaseName = builder.Configuration.GetSection("MongoDB:DatabaseName").Value
    ?? throw new InvalidOperationException("MongoDB database name not configured.");
var collectionName = builder.Configuration.GetSection("MongoDB:CollectionName").Value
    ?? throw new InvalidOperationException("MongoDB collection name not configured.");
var configuredTemplateBasePath = builder.Configuration.GetSection("Templates:BasePath").Value;
var templateBasePath = ResolveTemplateBasePath(configuredTemplateBasePath, builder.Environment.ContentRootPath);

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
builder.Services.AddInfrastructureServices(mongoConnectionString, databaseName, collectionName, templateBasePath);

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
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policyBuilder =>
    {
        policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// CORS
app.UseCors("AllowAngularApp");

// Routing
app.UseRouting();

// Health Checks
app.UseHealthChecksEndpoint();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerSetup();
}

// Authorization
app.UseAuthorization();

// Endpoints
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
