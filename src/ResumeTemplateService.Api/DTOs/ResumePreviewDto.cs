using System.Text.Json;

namespace ResumeTemplateService.Api.DTOs;

public class RenderResumePreviewRequest
{
    public string ResumeId { get; set; } = null!;
    public string? TemplateId { get; set; }
    public IReadOnlyCollection<string>? TemplateIds { get; set; }
}

public class RenderResumePreviewResponse
{
    public string ResumeId { get; set; } = null!;
    public IReadOnlyCollection<RenderedTemplateDto> Templates { get; set; } = Array.Empty<RenderedTemplateDto>();
}

public class RenderResumePdfRequest
{
    public string ResumeId { get; set; } = null!;
    public string TemplateId { get; set; } = null!;
}

/// <summary>
/// Paged parsed resume list response.
/// </summary>
public class ParsedResumeListResponse
{
    /// <summary>
    /// Maximum number of parsed resumes requested.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Number of parsed resumes skipped.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Total parsed resume count.
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// Parsed resume summaries.
    /// </summary>
    public IReadOnlyCollection<ParsedResumeSummaryDto> Resumes { get; set; } = Array.Empty<ParsedResumeSummaryDto>();
}

/// <summary>
/// Parsed resume summary for list views.
/// </summary>
public class ParsedResumeSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string? CandidateName { get; set; }
    public string? CandidateEmail { get; set; }
    public string? CurrentTitle { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Request to save edited resume content and render an HTML preview.
/// </summary>
public class SaveEditedResumePreviewRequest
{
    /// <summary>
    /// Parsed resume id from the source parsed_resumes collection.
    /// </summary>
    public string ResumeId { get; set; } = null!;

    /// <summary>
    /// Template id to use for the rendered HTML preview.
    /// </summary>
    public string TemplateId { get; set; } = null!;

    /// <summary>
    /// Edited resume document, profile wrapper, or profile.data object.
    /// </summary>
    public JsonElement Document { get; set; }

    /// <summary>
    /// Alternate edited resume payload field accepted from UI clients.
    /// </summary>
    public JsonElement Content { get; set; }

    /// <summary>
    /// Alternate edited resume payload field accepted from UI clients.
    /// </summary>
    public JsonElement Contents { get; set; }
}

/// <summary>
/// Response containing the saved edited resume preview HTML.
/// </summary>
public class SaveEditedResumePreviewResponse
{
    /// <summary>
    /// Parsed resume id used to save the edited copy.
    /// </summary>
    public string ResumeId { get; set; } = null!;

    /// <summary>
    /// Template id used to render the HTML.
    /// </summary>
    public string TemplateId { get; set; } = null!;

    /// <summary>
    /// Rendered HTML for the edited resume and selected template.
    /// </summary>
    public string Html { get; set; } = null!;
}

/// <summary>
/// Response returned after saving an edited resume document.
/// </summary>
public class SaveEditedResumeResponse
{
    /// <summary>
    /// Parsed resume id associated with the saved edited document.
    /// </summary>
    public string ResumeId { get; set; } = null!;

    /// <summary>
    /// Save result message.
    /// </summary>
    public string Message { get; set; } = null!;
}

/// <summary>
/// Response containing rendered HTML for a resume.
/// </summary>
public class RenderResumeHtmlResponse
{
    /// <summary>
    /// Parsed resume id used to render the HTML.
    /// </summary>
    public string ResumeId { get; set; } = null!;

    /// <summary>
    /// Template id used to render the HTML.
    /// </summary>
    public string TemplateId { get; set; } = null!;

    /// <summary>
    /// Rendered HTML.
    /// </summary>
    public string Html { get; set; } = null!;

    /// <summary>
    /// Parsed resume document data for UI binding.
    /// </summary>
    public JsonElement Data { get; set; }
}

public class RenderedTemplateDto
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

public class ShortResumeTemplateDto
{
    public string TemplateId { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public string ShortDescription { get; set; } = string.Empty;
    public string? PreviewThumbnailUrl { get; set; }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
}
