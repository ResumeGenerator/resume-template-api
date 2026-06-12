using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RazorLight;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Domain.ValueObjects;

namespace ResumeTemplateService.Infrastructure.TemplateRendering;

public class RazorTemplateRenderer : ITemplateRenderer
{
    private readonly IRazorLightEngine _engine;
    private readonly string _templateBasePath;
    private readonly ILogger<RazorTemplateRenderer> _logger;

    public RazorTemplateRenderer(
        IRazorLightEngine engine,
        string templateBasePath,
        ILogger<RazorTemplateRenderer> logger)
    {
        _engine = engine;
        _templateBasePath = templateBasePath;
        _logger = logger;
    }

    public async Task<string> RenderAsync(string templateId, ResumeViewModel model)
    {
        try
        {
            _logger.LogInformation("Rendering template: {TemplateId}", templateId);

            if (!await TemplateExistsAsync(templateId))
            {
                throw new InvalidOperationException($"Template '{templateId}' not found.");
            }

            var templatePath = Path.Combine(_templateBasePath, templateId, "template.cshtml");
            
            // Render the template using RazorLight
            var result = await _engine.CompileRenderAsync(templatePath, model);

            _logger.LogInformation("Template rendered successfully: {TemplateId}", templateId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template: {TemplateId}", templateId);
            throw;
        }
    }

    public Task<bool> TemplateExistsAsync(string templateId)
    {
        try
        {
            var templatePath = Path.Combine(_templateBasePath, templateId, "template.cshtml");
            var exists = File.Exists(templatePath);

            if (exists)
            {
                _logger.LogDebug("Template found: {TemplateId}", templateId);
            }
            else
            {
                _logger.LogWarning("Template not found: {TemplateId}", templateId);
            }

            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if template exists: {TemplateId}", templateId);
            throw;
        }
    }
}
