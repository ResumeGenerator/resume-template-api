using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RazorLight;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Application.Mappings;
using ResumeTemplateService.Infrastructure.Repositories;
using ResumeTemplateService.Infrastructure.TemplateRendering;

namespace ResumeTemplateService.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IResumeMapper, ResumeMapper>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string mongoConnectionString,
        string databaseName,
        string templateBasePath)
    {
        // MongoDB
        var mongoClient = new MongoClient(mongoConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseName);
        services.AddSingleton(mongoDatabase);

        // Repository
        services.AddScoped<IResumeRepository, ResumeRepository>();

        // RazorLight Engine
        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templateBasePath)
            .Build();

        services.AddSingleton(engine);

        // Template Rendering and Provider
        services.AddScoped<ITemplateRenderer>(sp =>
            new RazorTemplateRenderer(
                sp.GetRequiredService<IRazorLightEngine>(),
                templateBasePath,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RazorTemplateRenderer>>()));

        services.AddScoped<ITemplateProvider>(sp =>
            new TemplateProvider(
                templateBasePath,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TemplateProvider>>()));

        return services;
    }

    public static IServiceCollection AddHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddMongoDb(mongodbConnectionString: GetMongoConnectionString(), name: "mongodb", tags: new[] { "db" });

        return services;
    }

    private static string GetMongoConnectionString()
    {
        // This will be overridden in the Program.cs setup
        return "mongodb://localhost:27017";
    }
}
