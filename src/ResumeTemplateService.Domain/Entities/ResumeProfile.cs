using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace ResumeTemplateService.Domain.Entities;

[BsonIgnoreExtraElements]
public class ResumeProfile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("candidate_profile")]
    public CandidateProfile CandidateProfile { get; set; } = null!;

    [BsonElement("career_classification")]
    public CareerClassification CareerClassification { get; set; } = null!;

    [BsonElement("career_progression")]
    public CareerProgression CareerProgression { get; set; } = null!;

    [BsonElement("core_skills")]
    public CoreSkills CoreSkills { get; set; } = null!;

    [BsonElement("work_experience")]
    public List<WorkExperience> WorkExperience { get; set; } = new();

    [BsonElement("education")]
    public List<Education> Education { get; set; } = new();

    [BsonElement("certifications")]
    public List<Certification> Certifications { get; set; } = new();

    [BsonElement("leadership_highlights")]
    public List<string> LeadershipHighlights { get; set; } = new();

    [BsonElement("technical_highlights")]
    public List<string> TechnicalHighlights { get; set; } = new();

    [BsonElement("industry_highlights")]
    public List<string> IndustryHighlights { get; set; } = new();

    [BsonElement("resume_blocks")]
    public ResumeBlocks ResumeBlocks { get; set; } = null!;

    [BsonElement("ats_analysis")]
    public AtsAnalysis AtsAnalysis { get; set; } = null!;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

[BsonIgnoreExtraElements]
public class CandidateProfile
{
    [BsonElement("first_name")]
    public string FirstName { get; set; } = null!;

    [BsonElement("last_name")]
    public string LastName { get; set; } = null!;

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    [BsonElement("linkedin_url")]
    public string LinkedInUrl { get; set; } = string.Empty;

    [BsonElement("github_url")]
    public string GitHubUrl { get; set; } = string.Empty;

    [BsonElement("portfolio_url")]
    public string PortfolioUrl { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class CareerClassification
{
    [BsonElement("current_title")]
    public string CurrentTitle { get; set; } = null!;

    [BsonElement("years_of_experience")]
    public int YearsOfExperience { get; set; }

    [BsonElement("career_level")]
    public string CareerLevel { get; set; } = null!;

    [BsonElement("industry")]
    public string Industry { get; set; } = null!;

    [BsonElement("specialization")]
    public string Specialization { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class CareerProgression
{
    [BsonElement("progression_summary")]
    public string ProgressionSummary { get; set; } = null!;

    [BsonElement("career_trajectory")]
    public List<string> CareerTrajectory { get; set; } = new();

    [BsonElement("growth_areas")]
    public List<string> GrowthAreas { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class CoreSkills
{
    [BsonElement("primary_skills")]
    public List<string> PrimarySkills { get; set; } = new();

    [BsonElement("secondary_skills")]
    public List<string> SecondarySkills { get; set; } = new();

    [BsonElement("soft_skills")]
    public List<string> SoftSkills { get; set; } = new();

    [BsonElement("languages")]
    public List<string> Languages { get; set; } = new();

    [BsonElement("skills_matrix")]
    public SkillsMatrix SkillsMatrix { get; set; } = null!;
}

[BsonIgnoreExtraElements]
public class SkillsMatrix
{
    [BsonElement("technical_proficiency")]
    public List<SkillProficiency> TechnicalProficiency { get; set; } = new();

    [BsonElement("domain_expertise")]
    public List<SkillProficiency> DomainExpertise { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class SkillProficiency
{
    [BsonElement("skill")]
    public string Skill { get; set; } = null!;

    [BsonElement("level")]
    public string Level { get; set; } = null!;

    [BsonElement("years")]
    public int Years { get; set; }
}

[BsonIgnoreExtraElements]
public class WorkExperience
{
    [BsonElement("company")]
    public string Company { get; set; } = null!;

    [BsonElement("job_title")]
    public string JobTitle { get; set; } = null!;

    [BsonElement("employment_type")]
    public string EmploymentType { get; set; } = string.Empty;

    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

    [BsonElement("start_date")]
    public string StartDate { get; set; } = null!;

    [BsonElement("end_date")]
    public string EndDate { get; set; } = string.Empty;

    [BsonElement("is_current")]
    public bool IsCurrent { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("achievements")]
    public List<string> Achievements { get; set; } = new();

    [BsonElement("technologies")]
    public List<string> Technologies { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class Education
{
    [BsonElement("institution")]
    public string Institution { get; set; } = null!;

    [BsonElement("degree")]
    public string Degree { get; set; } = null!;

    [BsonElement("field_of_study")]
    public string FieldOfStudy { get; set; } = null!;

    [BsonElement("start_date")]
    public string StartDate { get; set; } = null!;

    [BsonElement("end_date")]
    public string EndDate { get; set; } = string.Empty;

    [BsonElement("grade")]
    public string Grade { get; set; } = string.Empty;

    [BsonElement("activities")]
    public List<string> Activities { get; set; } = new();

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class Certification
{
    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("issuer")]
    public string Issuer { get; set; } = null!;

    [BsonElement("issue_date")]
    public string IssueDate { get; set; } = null!;

    [BsonElement("expiry_date")]
    public string ExpiryDate { get; set; } = string.Empty;

    [BsonElement("credential_id")]
    public string CredentialId { get; set; } = string.Empty;

    [BsonElement("credential_url")]
    public string CredentialUrl { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class ResumeBlocks
{
    [BsonElement("professional_summary")]
    public string ProfessionalSummary { get; set; } = string.Empty;

    [BsonElement("headline")]
    public string Headline { get; set; } = string.Empty;

    [BsonElement("key_achievements")]
    public List<string> KeyAchievements { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class AtsAnalysis
{
    [BsonElement("ats_score")]
    public decimal AtsScore { get; set; }

    [BsonElement("keyword_matches")]
    public int KeywordMatches { get; set; }

    [BsonElement("missing_keywords")]
    public List<string> MissingKeywords { get; set; } = new();

    [BsonElement("formatting_issues")]
    public List<string> FormattingIssues { get; set; } = new();
}
