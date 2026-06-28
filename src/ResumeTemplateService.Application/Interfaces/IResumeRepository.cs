using System.Threading;
using System.Threading.Tasks;
using ResumeTemplateService.Domain.Entities;

namespace ResumeTemplateService.Application.Interfaces;

public interface IResumeRepository
{
    Task<ResumeProfile?> GetByIdAsync(string id);
    Task<ResumeProfile?> GetParsedByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<string?> GetParsedDocumentJsonAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id);
    Task<ResumeProfile> SaveEditedAsync(string originalResumeId, string editedResumeJson, CancellationToken cancellationToken = default);
    Task<string> GetTemplateIdAsync(string id, CancellationToken cancellationToken = default);
    Task<string> GetParsedTemplateIdAsync(string id, CancellationToken cancellationToken = default);
}
