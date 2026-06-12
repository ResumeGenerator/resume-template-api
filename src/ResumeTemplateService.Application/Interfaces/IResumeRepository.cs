using System.Threading.Tasks;
using ResumeTemplateService.Domain.Entities;

namespace ResumeTemplateService.Application.Interfaces;

public interface IResumeRepository
{
    Task<ResumeProfile?> GetByIdAsync(string id);
    Task<bool> ExistsAsync(string id);
}
