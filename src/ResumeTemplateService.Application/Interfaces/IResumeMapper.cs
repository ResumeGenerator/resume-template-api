using ResumeTemplateService.Domain.Entities;
using ResumeTemplateService.Domain.ValueObjects;

namespace ResumeTemplateService.Application.Interfaces;

public interface IResumeMapper
{
    ResumeViewModel Map(ResumeProfile profile);
}
