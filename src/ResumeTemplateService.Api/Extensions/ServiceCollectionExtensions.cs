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
        string collectionName,
        string templateBasePath)
    {
        // MongoDB
        var mongoClient = new MongoClient(mongoConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseName);
        services.AddSingleton(mongoDatabase);

        // Repository
        services.AddScoped<IResumeRepository>(sp =>
            new ResumeRepository(
                sp.GetRequiredService<IMongoDatabase>(),
                collectionName,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResumeRepository>>()));

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
}
