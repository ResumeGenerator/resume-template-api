using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumeTemplateService.Api.DTOs;
using ResumeTemplateService.Application.Interfaces;

namespace ResumeTemplateService.Api.Controllers;

/// <summary>
/// API controller for managing and listing available resume templates.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateProvider _templateProvider;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateProvider templateProvider,
        ILogger<TemplatesController> logger)
    {
        _templateProvider = templateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets a list of all available resume templates.
    /// </summary>
    /// <returns>A collection of available templates.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailableTemplates()
    {
        try
        {
            _logger.LogInformation("Fetching available templates");

            var templates = await _templateProvider.GetAvailableTemplatesAsync();

            var response = templates.Select(t => new TemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                PreviewUrl = t.PreviewUrl
            }).ToList();

            _logger.LogInformation("Retrieved {TemplateCount} templates", response.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available templates");
            var error = new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An error occurred while fetching available templates.",
                Details = ex.Message
            };
            return StatusCode(StatusCodes.Status500InternalServerError, error);
        }
    }

    /// <summary>
    /// Gets a concise list of resume templates for template selection panes.
    /// </summary>
    /// <returns>A short summary list of available resume templates.</returns>
    [HttpGet("resumes/short")]
    [ProducesResponseType(typeof(IEnumerable<ShortResumeTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetShortResumeTemplates()
    {
        try
        {
            _logger.LogInformation("Fetching short resume template list");

            var templates = await _templateProvider.GetAvailableTemplatesAsync();

            var response = templates.Select(t => new ShortResumeTemplateDto
            {
                TemplateId = t.Id,
                TemplateName = t.Name,
                ShortDescription = t.Description,
                PreviewThumbnailUrl = null
            }).ToList();

            if (response.Count == 0)
            {
                _logger.LogWarning("No resume templates are available");
                return NotFound(new ErrorResponse
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "No resume templates are available."
                });
            }

            _logger.LogInformation("Retrieved {TemplateCount} short resume templates", response.Count);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching short resume template list");
            var error = new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An error occurred while fetching short resume templates.",
                Details = ex.Message
            };
            return StatusCode(StatusCodes.Status500InternalServerError, error);
        }
    }
}
