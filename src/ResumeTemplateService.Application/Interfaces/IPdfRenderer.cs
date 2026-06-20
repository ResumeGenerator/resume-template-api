namespace ResumeTemplateService.Application.Interfaces;

public interface IPdfRenderer
{
    Task<byte[]> RenderPdfAsync(string html, CancellationToken cancellationToken = default);
}
