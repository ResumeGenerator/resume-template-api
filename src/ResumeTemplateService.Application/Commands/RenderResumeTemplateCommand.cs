using System.Threading;
using System.Threading.Tasks;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Domain.ValueObjects;

namespace ResumeTemplateService.Application.Commands;

public class RenderResumeTemplateCommand
{
    public string ResumeId { get; set; } = null!;
    public IReadOnlyCollection<string> TemplateIds { get; set; } = Array.Empty<string>();
}

public class RenderResumeTemplateCommandHandler
{
    private readonly IResumeRepository _resumeRepository;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IResumeMapper _resumeMapper;

    public RenderResumeTemplateCommandHandler(
        IResumeRepository resumeRepository,
        ITemplateRenderer templateRenderer,
        IResumeMapper resumeMapper)
    {
        _resumeRepository = resumeRepository;
        _templateRenderer = templateRenderer;
        _resumeMapper = resumeMapper;
    }

    public async Task<IReadOnlyCollection<RenderResumeTemplateResponse>> HandleAsync(
        RenderResumeTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        // Fetch resume profile
        var resumeProfile = await _resumeRepository.GetByIdAsync(command.ResumeId);
        if (resumeProfile == null)
        {
            throw new InvalidOperationException($"Resume with id '{command.ResumeId}' not found.");
        }

        var templateIds = command.TemplateIds
            .Where(templateId => !string.IsNullOrWhiteSpace(templateId))
            .Select(templateId => templateId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (templateIds.Count == 0)
        {
            throw new InvalidOperationException("At least one template id is required.");
        }

        // Map to view model
        var resumeViewModel = _resumeMapper.Map(resumeProfile);

        var renderedTemplates = new List<RenderResumeTemplateResponse>();
        foreach (var templateId in templateIds)
        {
            // Check if template exists
            if (!await _templateRenderer.TemplateExistsAsync(templateId))
            {
                throw new InvalidOperationException($"Template '{templateId}' not found.");
            }

            // Render template
            var html = await _templateRenderer.RenderAsync(templateId, resumeViewModel);

            renderedTemplates.Add(new RenderResumeTemplateResponse
            {
                TemplateId = templateId,
                Html = html
            });
        }

        return renderedTemplates;
    }
}

public class RenderResumeTemplateResponse
{
    public string TemplateId { get; set; } = null!;
    public string Html { get; set; } = null!;
}
