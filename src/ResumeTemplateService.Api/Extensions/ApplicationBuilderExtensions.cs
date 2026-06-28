using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace ResumeTemplateService.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerSetup(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Resume Template Service API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "Resume Template Service - Swagger UI";
            c.DefaultModelsExpandDepth(1);
            c.DefaultModelExpandDepth(1);
        });

        return app;
    }

    public static IApplicationBuilder UseHealthChecksEndpoint(this IApplicationBuilder app)
    {
        var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        description = x.Value.Description,
                        duration = x.Value.Duration
                    }),
                    totalDuration = report.TotalDuration
                };

                await context.Response.WriteAsJsonAsync(response, jsonSerializerOptions);
            }
        });

        return app;
    }
}
