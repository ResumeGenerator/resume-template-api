using System.Threading.Tasks;
using ResumeTemplateService.Domain.ValueObjects;

namespace ResumeTemplateService.Application.Interfaces;

public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateId, ResumeViewModel model);
    Task<bool> TemplateExistsAsync(string templateId);
}
