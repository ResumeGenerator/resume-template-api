using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumeTemplateService.Api.DTOs;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Application.Queries;

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
}
