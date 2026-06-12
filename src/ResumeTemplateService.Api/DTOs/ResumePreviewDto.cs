namespace ResumeTemplateService.Api.DTOs;

public class RenderResumePreviewRequest
{
    public string ResumeId { get; set; } = null!;
    public string TemplateId { get; set; } = null!;
}

public class RenderResumePreviewResponse
{
    public string TemplateId { get; set; } = null!;
    public string Html { get; set; } = null!;
}

public class TemplateDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
}
