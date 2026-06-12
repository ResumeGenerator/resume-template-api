using System.Threading;
using System.Threading.Tasks;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Domain.ValueObjects;

namespace ResumeTemplateService.Application.Commands;

public class RenderResumeTemplateCommand
{
    public string ResumeId { get; set; } = null!;
    public string TemplateId { get; set; } = null!;
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

    public async Task<RenderResumeTemplateResponse> HandleAsync(
        RenderResumeTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        // Fetch resume profile
        var resumeProfile = await _resumeRepository.GetByIdAsync(command.ResumeId);
        if (resumeProfile == null)
        {
            throw new InvalidOperationException($"Resume with id '{command.ResumeId}' not found.");
        }

        // Check if template exists
        if (!await _templateRenderer.TemplateExistsAsync(command.TemplateId))
        {
            throw new InvalidOperationException($"Template '{command.TemplateId}' not found.");
        }

        // Map to view model
        var resumeViewModel = _resumeMapper.Map(resumeProfile);

        // Render template
        var html = await _templateRenderer.RenderAsync(command.TemplateId, resumeViewModel);

        return new RenderResumeTemplateResponse
        {
            TemplateId = command.TemplateId,
            Html = html
        };
    }
}

public class RenderResumeTemplateResponse
{
    public string TemplateId { get; set; } = null!;
    public string Html { get; set; } = null!;
}
