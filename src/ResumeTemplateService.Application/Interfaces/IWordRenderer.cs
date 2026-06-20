using ResumeTemplateService.Domain.ValueObjects;

namespace ResumeTemplateService.Application.Interfaces;

public interface IWordRenderer
{
    Task<byte[]> RenderWordAsync(ResumeViewModel resume, string templateId, CancellationToken cancellationToken = default);
}
