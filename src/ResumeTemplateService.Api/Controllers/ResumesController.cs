using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumeTemplateService.Api.DTOs;
using ResumeTemplateService.Application.Commands;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Application.Queries;

namespace ResumeTemplateService.Api.Controllers;

/// <summary>
/// API controller for resume preview and template rendering operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ResumesController : ControllerBase
{
    private readonly IResumeRepository _resumeRepository;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IResumeMapper _resumeMapper;
    private readonly IPdfRenderer _pdfRenderer;
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(
        IResumeRepository resumeRepository,
        ITemplateRenderer templateRenderer,
        IResumeMapper resumeMapper,
        IPdfRenderer pdfRenderer,
        ILogger<ResumesController> logger)
    {
        _resumeRepository = resumeRepository;
        _templateRenderer = templateRenderer;
        _resumeMapper = resumeMapper;
        _pdfRenderer = pdfRenderer;
        _logger = logger;
    }

    /// <summary>
    /// Renders a resume with one or more specified templates and returns HTML previews.
    /// </summary>
    /// <param name="request">The render request containing resume ID and template IDs.</param>
    /// <returns>HTML previews of the resume.</returns>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(RenderResumePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenderResumePreview([FromBody] RenderResumePreviewRequest request)
    {
        try
        {
            var templateIds = GetRequestedTemplateIds(request);

            _logger.LogInformation("Rendering resume preview - ResumeId: {ResumeId}, TemplateIds: {TemplateIds}",
                request.ResumeId, string.Join(", ", templateIds));

            if (string.IsNullOrWhiteSpace(request.ResumeId))
            {
                var error = new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "ResumeId is required.",
                    Details = "The 'resumeId' field cannot be empty."
                };
                return BadRequest(error);
            }

            if (templateIds.Count == 0)
            {
                var error = new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "At least one template id is required.",
                    Details = "Provide either 'templateId' or 'templateIds'."
                };
                return BadRequest(error);
            }

            // Execute the command
            var command = new RenderResumeTemplateCommand
            {
                ResumeId = request.ResumeId,
                TemplateIds = templateIds
            };

            var handler = new RenderResumeTemplateCommandHandler(
                _resumeRepository,
                _templateRenderer,
                _resumeMapper);

            var results = await handler.HandleAsync(command);

            var response = new RenderResumePreviewResponse
            {
                ResumeId = request.ResumeId,
                Templates = results.Select(result => new RenderedTemplateDto
                {
                    TemplateId = result.TemplateId,
                    Html = result.Html
                }).ToList()
            };

            _logger.LogInformation("Resume preview rendered successfully - ResumeId: {ResumeId}, TemplateCount: {TemplateCount}",
                request.ResumeId, response.Templates.Count);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Resume or template not found");
            var error = new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = ex.Message,
            };
            return NotFound(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering resume preview");
            var error = new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An error occurred while rendering the resume preview.",
                Details = ex.Message
            };
            return StatusCode(StatusCodes.Status500InternalServerError, error);
        }
    }

    /// <summary>
    /// Saves an edited resume document to the edited resume collection.
    /// </summary>
    /// <param name="resumeId">The parsed resume id to associate with the edited copy.</param>
    /// <param name="document">The full edited resume document.</param>
    /// <returns>The saved resume id.</returns>
    [HttpPost("edited/{resumeId}")]
    [ProducesResponseType(typeof(SaveEditedResumeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SaveEditedResume(string resumeId, [FromBody] JsonElement document)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resumeId))
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "ResumeId is required.",
                    Details = "The route value 'resumeId' cannot be empty."
                });
            }

            if (document.ValueKind != JsonValueKind.Object)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Edited resume document must be a JSON object.",
                    Details = "Submit the full edited resume document as the request body."
                });
            }

            var normalizedResumeId = resumeId.Trim();
            if (!await _resumeRepository.ExistsAsync(normalizedResumeId))
            {
                throw new InvalidOperationException($"Resume with id '{normalizedResumeId}' not found.");
            }

            await _resumeRepository.SaveEditedAsync(
                normalizedResumeId,
                document.GetRawText(),
                HttpContext.RequestAborted);

            return Ok(new SaveEditedResumeResponse
            {
                ResumeId = normalizedResumeId,
                Message = "Edited resume saved successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Resume not found while saving edited document");
            return NotFound(new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving edited resume document");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An error occurred while saving the edited resume document.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Renders a parsed resume as HTML.
    /// </summary>
    /// <param name="resumeId">The parsed resume id.</param>
    /// <param name="templateId">Optional template id. If omitted, the template saved in profile.template is used.</param>
    /// <returns>The rendered HTML for the parsed resume.</returns>
    [HttpGet("{resumeId}/html")]
    [ProducesResponseType(typeof(RenderResumeHtmlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenderResumeHtml(string resumeId, [FromQuery] string? templateId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resumeId))
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "ResumeId is required.",
                    Details = "The route value 'resumeId' cannot be empty."
                });
            }

            var normalizedResumeId = resumeId.Trim();
            var resolvedTemplateId = string.IsNullOrWhiteSpace(templateId)
                ? await _resumeRepository.GetParsedTemplateIdAsync(normalizedResumeId, HttpContext.RequestAborted)
                : templateId.Trim();

            if (string.IsNullOrWhiteSpace(resolvedTemplateId))
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "TemplateId is required.",
                    Details = "Pass templateId as a query string or save a template value in profile.template."
                });
            }

            var resumeProfile = await _resumeRepository.GetParsedByIdAsync(
                normalizedResumeId,
                HttpContext.RequestAborted);
            if (resumeProfile == null)
            {
                throw new InvalidOperationException($"Parsed resume with id '{normalizedResumeId}' not found.");
            }

            if (!await _templateRenderer.TemplateExistsAsync(resolvedTemplateId))
            {
                throw new InvalidOperationException($"Template '{resolvedTemplateId}' not found.");
            }

            var viewModel = _resumeMapper.Map(resumeProfile);
            var html = await _templateRenderer.RenderAsync(resolvedTemplateId, viewModel);

            return Ok(new RenderResumeHtmlResponse
            {
                ResumeId = normalizedResumeId,
                TemplateId = resolvedTemplateId,
                Html = html
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Resume or template not found while rendering HTML");
            return NotFound(new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering resume HTML");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An error occurred while rendering the resume HTML.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Saves edited resume content and returns an HTML preview for the selected template.
    /// </summary>
    /// <param name="request">The edited resume content and template to render.</param>
    /// <returns>The rendered HTML preview for the saved edited resume.</returns>
    [HttpPost("edited/preview")]
    [ProducesResponseType(typeof(SaveEditedResumePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SaveEditedResumePreview([FromBody] SaveEditedResumePreviewRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ResumeId))
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "ResumeId is required.",
                    Details = "The 'resumeId' field cannot be empty."
                });
            }

            if (string.IsNullOrWhiteSpace(request.TemplateId))
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "TemplateId is required.",
                    Details = "The 'templateId' field cannot be empty."
                });
            }

            var editedDocument = GetEditedDocument(request);
            if (editedDocument.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Edited resume document is required.",
                    Details = "Provide the edited content in the 'document', 'content', or 'contents' field."
                });
            }

            if (editedDocument.ValueKind != JsonValueKind.Object)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Edited resume document must be a JSON object.",
                    Details = "The edited content cannot be an array, string, number, boolean, or null."
                });
            }

            var resumeId = request.ResumeId.Trim();
            var templateId = request.TemplateId.Trim();

            _logger.LogInformation(
                "Saving edited resume preview - ResumeId: {ResumeId}, TemplateId: {TemplateId}",
                resumeId,
                templateId);

            if (!await _resumeRepository.ExistsAsync(resumeId))
            {
                throw new InvalidOperationException($"Resume with id '{resumeId}' not found.");
            }

            if (!await _templateRenderer.TemplateExistsAsync(templateId))
            {
                throw new InvalidOperationException($"Template '{templateId}' not found.");
            }

            var resumeProfile = await _resumeRepository.SaveEditedAsync(
                resumeId,
                editedDocument.GetRawText(),
                HttpContext.RequestAborted);
            var viewModel = _resumeMapper.Map(resumeProfile);
            var html = await _templateRenderer.RenderAsync(templateId, viewModel);

            return Ok(new SaveEditedResumePreviewResponse
            {
                ResumeId = resumeId,
                TemplateId = templateId,
                Html = html
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Resume or template not found while saving edited preview");
            return NotFound(new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving edited resume preview");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An error occurred while saving and rendering the edited resume preview.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Renders a resume template as PDF and returns the generated file.
    /// </summary>
    /// <param name="request">The PDF render request containing resume ID and template ID.</param>
    /// <returns>A PDF generated from the selected resume template.</returns>
    [HttpPost("pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenderResumePdf([FromBody] RenderResumePdfRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ResumeId))
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "ResumeId is required.",
                    Details = "The 'resumeId' field cannot be empty."
                });
            }

            if (string.IsNullOrWhiteSpace(request.TemplateId))
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "TemplateId is required.",
                    Details = "The 'templateId' field cannot be empty."
                });
            }

            var templateId = request.TemplateId.Trim();
            _logger.LogInformation("Rendering resume PDF - ResumeId: {ResumeId}, TemplateId: {TemplateId}",
                request.ResumeId, templateId);

            var resumeProfile = await _resumeRepository.GetByIdAsync(request.ResumeId.Trim());
            if (resumeProfile == null)
            {
                throw new InvalidOperationException($"Resume with id '{request.ResumeId}' not found.");
            }

            if (!await _templateRenderer.TemplateExistsAsync(templateId))
            {
                throw new InvalidOperationException($"Template '{templateId}' not found.");
            }

            var viewModel = _resumeMapper.Map(resumeProfile);
            var html = await _templateRenderer.RenderAsync(templateId, viewModel);
            var pdfBytes = await _pdfRenderer.RenderPdfAsync(html, HttpContext.RequestAborted);
            var fileName = $"{Slugify(templateId)}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Resume or template not found while rendering PDF");
            return NotFound(new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering resume PDF");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An error occurred while rendering the resume PDF.",
                Details = ex.Message
            });
        }
    }

    private static IReadOnlyCollection<string> GetRequestedTemplateIds(RenderResumePreviewRequest request)
    {
        var templateIds = new List<string>();

        if (request.TemplateIds != null)
        {
            templateIds.AddRange(request.TemplateIds);
        }

        if (!string.IsNullOrWhiteSpace(request.TemplateId))
        {
            templateIds.Add(request.TemplateId);
        }

        return templateIds
            .Where(templateId => !string.IsNullOrWhiteSpace(templateId))
            .Select(templateId => templateId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static JsonElement GetEditedDocument(SaveEditedResumePreviewRequest request)
    {
        if (request.Document.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            return request.Document;
        }

        if (request.Content.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            return request.Content;
        }

        return request.Contents;
    }

    private static string Slugify(string value)
    {
        var slug = new string(value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        return string.Join("-", slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
