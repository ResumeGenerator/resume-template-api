using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(
        IResumeRepository resumeRepository,
        ITemplateRenderer templateRenderer,
        IResumeMapper resumeMapper,
        ILogger<ResumesController> logger)
    {
        _resumeRepository = resumeRepository;
        _templateRenderer = templateRenderer;
        _resumeMapper = resumeMapper;
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
}
