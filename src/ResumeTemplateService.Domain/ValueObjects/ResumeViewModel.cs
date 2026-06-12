using System;
using System.Collections.Generic;

namespace ResumeTemplateService.Domain.ValueObjects;

public class ResumeViewModel
{
    public PersonalInfoViewModel PersonalInfo { get; set; } = null!;
    public string Headline { get; set; } = string.Empty;
    public List<string> SummaryPoints { get; set; } = new();
    public List<string> CoreCompetencies { get; set; } = new();
    public List<TechnicalSkillViewModel> TechnicalSkills { get; set; } = new();
    public List<ExperienceViewModel> Experience { get; set; } = new();
    public List<EducationViewModel> Education { get; set; } = new();
    public List<CertificationViewModel> Certifications { get; set; } = new();
    public List<string> LeadershipHighlights { get; set; } = new();
    public List<string> TechnicalHighlights { get; set; } = new();
    public List<string> IndustryHighlights { get; set; } = new();
}

public class PersonalInfoViewModel
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string GitHubUrl { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class TechnicalSkillViewModel
{
    public string Skill { get; set; } = null!;
    public string Level { get; set; } = null!;
    public int Years { get; set; }
}

public class ExperienceViewModel
{
    public string Company { get; set; } = null!;
    public string JobTitle { get; set; } = null!;
    public string Location { get; set; } = string.Empty;
    public string StartDate { get; set; } = null!;
    public string EndDate { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Achievements { get; set; } = new();
    public List<string> Technologies { get; set; } = new();

    public string DateRange => IsCurrent
        ? $"{StartDate} - Present"
        : $"{StartDate} - {EndDate}";
}

public class EducationViewModel
{
    public string Institution { get; set; } = null!;
    public string Degree { get; set; } = null!;
    public string FieldOfStudy { get; set; } = null!;
    public string StartDate { get; set; } = null!;
    public string EndDate { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string DateRange => string.IsNullOrEmpty(EndDate)
        ? StartDate
        : $"{StartDate} - {EndDate}";
}

public class CertificationViewModel
{
    public string Name { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string IssueDate { get; set; } = null!;
    public string ExpiryDate { get; set; } = string.Empty;
    public string CredentialUrl { get; set; } = string.Empty;

    public bool IsExpired => !string.IsNullOrEmpty(ExpiryDate) && 
        DateTime.TryParse(ExpiryDate, out var expiry) && 
        expiry < DateTime.UtcNow;
}
