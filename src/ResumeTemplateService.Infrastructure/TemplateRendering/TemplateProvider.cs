using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ResumeTemplateService.Application.Interfaces;

namespace ResumeTemplateService.Infrastructure.TemplateRendering;

public class TemplateProvider : ITemplateProvider
{
    private readonly string _templateBasePath;
    private readonly ILogger<TemplateProvider> _logger;

    public TemplateProvider(string templateBasePath, ILogger<TemplateProvider> logger)
    {
        _templateBasePath = templateBasePath;
        _logger = logger;
    }

    public Task<IEnumerable<AvailableTemplate>> GetAvailableTemplatesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching available templates from: {TemplateBasePath}", _templateBasePath);

            if (!Directory.Exists(_templateBasePath))
            {
                _logger.LogWarning("Template directory does not exist: {TemplateBasePath}", _templateBasePath);
                return Task.FromResult(Enumerable.Empty<AvailableTemplate>());
            }

            var templates = Directory.GetDirectories(_templateBasePath)
                .Where(dir => File.Exists(Path.Combine(dir, "template.cshtml")))
                .Select(dir =>
                {
                    var templateId = Path.GetFileName(dir);
                    return new AvailableTemplate
                    {
                        Id = templateId,
                        Name = FormatTemplateName(templateId),
                        Description = GetTemplateDescription(templateId),
                        PreviewUrl = $"/api/templates/{templateId}/preview"
                    };
                })
                .ToList();

            _logger.LogInformation("Found {TemplateCount} templates", templates.Count);
            return Task.FromResult(templates.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available templates");
            throw;
        }
    }

    private string FormatTemplateName(string templateId)
    {
        return string.Join(" ", templateId.Split('-')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1)));
    }

    private string GetTemplateDescription(string templateId)
    {
        return templateId switch
        {
            "professional-dark-blue" => "A professional dark blue themed resume template",
            "modern-minimal" => "A modern minimal resume template",
            _ => "Resume template"
        };
    }
}
