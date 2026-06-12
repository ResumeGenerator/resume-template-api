using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using ResumeTemplateService.Api.Extensions;
using ResumeTemplateService.Api.Middleware;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? throw new InvalidOperationException("MongoDB connection string not configured.");
var databaseName = builder.Configuration.GetSection("MongoDB:DatabaseName").Value
    ?? throw new InvalidOperationException("MongoDB database name not configured.");
var templateBasePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "templates");

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
builder.Services.AddInfrastructureServices(mongoConnectionString, databaseName, templateBasePath);

// Health Checks
//builder.Services.AddHealthChecks()
//    .AddMongoDb(mongoConnectionString, name: "mongodb", tags: new[] { "db" });

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
