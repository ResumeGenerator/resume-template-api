using System.Collections.Generic;
using System.Linq;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Domain.Entities;
using ResumeTemplateService.Domain.ValueObjects;

namespace ResumeTemplateService.Application.Mappings;

public class ResumeMapper : IResumeMapper
{
    public ResumeViewModel Map(ResumeProfile profile)
    {
        var viewModel = new ResumeViewModel
        {
            PersonalInfo = MapPersonalInfo(profile.CandidateProfile),
            Headline = profile.ResumeBlocks?.Headline ?? profile.CareerClassification?.CurrentTitle ?? string.Empty,
            SummaryPoints = ExtractSummaryPoints(profile),
            CoreCompetencies = profile.CoreSkills?.PrimarySkills ?? new List<string>(),
            TechnicalSkills = MapTechnicalSkills(profile.CoreSkills),
            Experience = profile.WorkExperience?.Select(MapExperience).ToList() ?? new List<ExperienceViewModel>(),
            Education = profile.Education?.Select(MapEducation).ToList() ?? new List<EducationViewModel>(),
            Certifications = profile.Certifications?.Select(MapCertification).ToList() ?? new List<CertificationViewModel>(),
            LeadershipHighlights = profile.LeadershipHighlights ?? new List<string>(),
            TechnicalHighlights = profile.TechnicalHighlights ?? new List<string>(),
            IndustryHighlights = profile.IndustryHighlights ?? new List<string>()
        };

        return viewModel;
    }

    private PersonalInfoViewModel MapPersonalInfo(CandidateProfile candidate)
    {
        return new PersonalInfoViewModel
        {
            FirstName = candidate.FirstName,
            LastName = candidate.LastName,
            Email = candidate.Email,
            Phone = candidate.Phone,
            Location = BuildLocation(candidate),
            LinkedInUrl = candidate.LinkedInUrl,
            GitHubUrl = candidate.GitHubUrl,
            PortfolioUrl = candidate.PortfolioUrl
        };
    }

    private string BuildLocation(CandidateProfile candidate)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(candidate.City)) parts.Add(candidate.City);
        if (!string.IsNullOrEmpty(candidate.State)) parts.Add(candidate.State);
        if (!string.IsNullOrEmpty(candidate.Country)) parts.Add(candidate.Country);

        return parts.Any() ? string.Join(", ", parts) : candidate.Location;
    }

    private List<string> ExtractSummaryPoints(ResumeProfile profile)
    {
        var points = new List<string>();

        if (!string.IsNullOrEmpty(profile.ResumeBlocks?.ProfessionalSummary))
        {
            // Split professional summary into bullet points if needed
            var summary = profile.ResumeBlocks.ProfessionalSummary;
            if (summary.Contains('\n'))
            {
                points.AddRange(summary.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            else
            {
                points.Add(summary);
            }
        }

        if (profile.ResumeBlocks?.KeyAchievements?.Any() == true)
        {
            points.AddRange(profile.ResumeBlocks.KeyAchievements);
        }

        return points.Take(5).ToList(); // Limit to top 5 summary points
    }

    private List<TechnicalSkillViewModel> MapTechnicalSkills(CoreSkills coreSkills)
    {
        if (coreSkills?.SkillsMatrix?.TechnicalProficiency == null)
            return new List<TechnicalSkillViewModel>();

        return coreSkills.SkillsMatrix.TechnicalProficiency
            .Select(skill => new TechnicalSkillViewModel
            {
                Skill = skill.Skill,
                Level = skill.Level,
                Years = skill.Years
            })
            .ToList();
    }

    private ExperienceViewModel MapExperience(WorkExperience experience)
    {
        return new ExperienceViewModel
        {
            Company = experience.Company,
            JobTitle = experience.JobTitle,
            Location = experience.Location,
            StartDate = experience.StartDate,
            EndDate = experience.EndDate,
            IsCurrent = experience.IsCurrent,
            Description = experience.Description,
            Achievements = experience.Achievements ?? new List<string>(),
            Technologies = experience.Technologies ?? new List<string>()
        };
    }

    private EducationViewModel MapEducation(Education education)
    {
        return new EducationViewModel
        {
            Institution = education.Institution,
            Degree = education.Degree,
            FieldOfStudy = education.FieldOfStudy,
            StartDate = education.StartDate,
            EndDate = education.EndDate,
            Grade = education.Grade,
            Description = education.Description
        };
    }

    private CertificationViewModel MapCertification(Certification certification)
    {
        return new CertificationViewModel
        {
            Name = certification.Name,
            Issuer = certification.Issuer,
            IssueDate = certification.IssueDate,
            ExpiryDate = certification.ExpiryDate,
            CredentialUrl = certification.CredentialUrl
        };
    }
}
