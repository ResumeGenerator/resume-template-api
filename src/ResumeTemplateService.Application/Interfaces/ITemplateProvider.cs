using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResumeTemplateService.Application.Interfaces;

public interface ITemplateProvider
{
    Task<IEnumerable<AvailableTemplate>> GetAvailableTemplatesAsync();
}

public class AvailableTemplate
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
}
