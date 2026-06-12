using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResumeTemplateService.Application.Interfaces;

namespace ResumeTemplateService.Application.Queries;

public class GetAvailableTemplatesQuery
{
}

public class GetAvailableTemplatesQueryHandler
{
    private readonly ITemplateProvider _templateProvider;

    public GetAvailableTemplatesQueryHandler(ITemplateProvider templateProvider)
    {
        _templateProvider = templateProvider;
    }

    public async Task<IEnumerable<AvailableTemplate>> HandleAsync(
        GetAvailableTemplatesQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _templateProvider.GetAvailableTemplatesAsync();
    }
}
