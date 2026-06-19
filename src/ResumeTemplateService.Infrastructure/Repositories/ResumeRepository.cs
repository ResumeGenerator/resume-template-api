using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Threading.Tasks;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ResumeTemplateService.Infrastructure.Repositories;

public class ResumeRepository : IResumeRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<ResumeRepository> _logger;

    public ResumeRepository(IMongoDatabase database, string collectionName, ILogger<ResumeRepository> logger)
    {
        _logger = logger;
        _collection = database.GetCollection<BsonDocument>(collectionName);
    }

    public async Task<ResumeProfile?> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Fetching resume with id: {ResumeId}", id);

            var filter = BuildIdFilter(id);
            var document = await _collection.Find(filter).FirstOrDefaultAsync();
            var resume = document == null ? null : MapDocument(document);

            if (resume != null)
            {
                _logger.LogInformation("Resume found: {ResumeId}", id);
            }
            else
            {
                _logger.LogWarning("Resume not found: {ResumeId}", id);
            }

            return resume;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching resume with id: {ResumeId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        try
        {
            _logger.LogDebug("Checking if resume exists: {ResumeId}", id);

            var filter = BuildIdFilter(id);
            var count = await _collection.CountDocumentsAsync(filter);

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if resume exists: {ResumeId}", id);
            throw;
        }
    }

    private static FilterDefinition<BsonDocument> BuildIdFilter(string id)
    {
        var filters = new List<FilterDefinition<BsonDocument>>
        {
            Builders<BsonDocument>.Filter.Eq("_id", id)
        };

        if (ObjectId.TryParse(id, out var objectId))
        {
            filters.Add(Builders<BsonDocument>.Filter.Eq("_id", objectId));
        }

        return Builders<BsonDocument>.Filter.Or(filters);
    }

    private static ResumeProfile MapDocument(BsonDocument document)
    {
        if (document.Contains("profile") && document["profile"].IsBsonDocument)
        {
            return MapParserDocument(document);
        }

        return BsonSerializer.Deserialize<ResumeProfile>(document);
    }

    private static ResumeProfile MapParserDocument(BsonDocument document)
    {
        var profile = GetDocument(document, "profile") ?? new BsonDocument();
        var candidate = GetDocument(profile, "candidateProfile") ?? new BsonDocument();
        var careerClassification = GetDocument(profile, "careerClassification") ?? new BsonDocument();
        var careerProgression = GetDocument(profile, "careerProgression") ?? new BsonDocument();
        var coreSkills = GetDocument(profile, "coreSkills") ?? new BsonDocument();
        var resumeBlocks = GetDocument(profile, "resumeBlocks") ?? new BsonDocument();
        var atsAnalysis = GetDocument(profile, "atsAnalysis") ?? new BsonDocument();
        var nameParts = SplitFullName(GetString(candidate, "fullName"));

        return new ResumeProfile
        {
            Id = document.GetValue("_id", ObjectId.Empty).ToString() ?? string.Empty,
            CandidateProfile = new CandidateProfile
            {
                FirstName = nameParts.firstName,
                LastName = nameParts.lastName,
                Email = GetString(candidate, "email"),
                Phone = GetString(candidate, "phone"),
                Location = GetString(candidate, "location"),
                Country = GetString(candidate, "location"),
                LinkedInUrl = GetOnlineProfileUrl(candidate, "LinkedIn"),
                GitHubUrl = GetOnlineProfileUrl(candidate, "GitHub"),
                PortfolioUrl = GetOnlineProfileUrl(candidate, "Portfolio")
            },
            CareerClassification = new CareerClassification
            {
                CurrentTitle = GetString(candidate, "currentTitle"),
                YearsOfExperience = GetInt(candidate, "totalExperienceYears"),
                CareerLevel = GetString(careerProgression, "careerLevel", GetString(careerClassification, "seniorityLevel")),
                Industry = GetString(careerClassification, "industry"),
                Specialization = GetString(careerClassification, "subSpecialization")
            },
            CareerProgression = new CareerProgression
            {
                ProgressionSummary = string.Join(" | ", GetStringArray(careerProgression, "primarySpecialization")),
                CareerTrajectory = GetStringArray(careerProgression, "industryFocus"),
                GrowthAreas = GetStringArray(careerProgression, "secondarySpecialization")
            },
            CoreSkills = new CoreSkills
            {
                PrimarySkills = GetStringArray(coreSkills, "hardSkills"),
                SecondarySkills = GetStringArray(coreSkills, "toolsAndSoftware")
                    .Concat(GetStringArray(coreSkills, "methodologiesAndFrameworks"))
                    .ToList(),
                SoftSkills = GetStringArray(coreSkills, "softSkills"),
                Languages = GetStringArray(coreSkills, "languages"),
                SkillsMatrix = MapSkillsMatrix(profile)
            },
            WorkExperience = GetDocumentArray(profile, "workExperience").Select(MapWorkExperience).ToList(),
            Education = GetDocumentArray(profile, "education").Select(MapEducation).ToList(),
            Certifications = GetDocumentArray(profile, "certificationsAndLicenses").Select(MapCertification).ToList(),
            LeadershipHighlights = GetStringArray(resumeBlocks, "leadershipHighlights"),
            TechnicalHighlights = GetStringArray(resumeBlocks, "technicalHighlights"),
            IndustryHighlights = GetStringArray(resumeBlocks, "industryHighlights"),
            ResumeBlocks = new ResumeBlocks
            {
                ProfessionalSummary = string.Join(Environment.NewLine, GetStringArray(resumeBlocks, "executiveSummary")
                    .DefaultIfEmpty(string.Join(Environment.NewLine, GetStringArray(profile, "professionalSummaryPoints")))),
                Headline = GetString(candidate, "professionalHeadline", GetString(candidate, "currentTitle")),
                KeyAchievements = GetStringArray(resumeBlocks, "projectHighlights")
            },
            AtsAnalysis = new AtsAnalysis
            {
                AtsScore = GetDecimal(atsAnalysis, "estimatedAtsScore"),
                KeywordMatches = GetDocumentArray(atsAnalysis, "keywordDensity").Count,
                MissingKeywords = GetStringArray(atsAnalysis, "missingCriticalSections"),
                FormattingIssues = GetStringArray(atsAnalysis, "formattingRisks")
            },
            CreatedAt = GetDateTime(document, "createdAt"),
            UpdatedAt = GetDateTime(document, "updatedAt")
        };
    }

    private static WorkExperience MapWorkExperience(BsonDocument experience)
    {
        var achievements = GetStringArray(experience, "achievements");
        if (achievements.Count == 0)
        {
            achievements = GetStringArray(experience, "responsibilities");
        }

        return new WorkExperience
        {
            Company = GetString(experience, "companyOrOrganization"),
            JobTitle = GetString(experience, "role"),
            EmploymentType = GetString(experience, "employmentType"),
            Location = GetString(experience, "location"),
            StartDate = GetString(experience, "startDate"),
            EndDate = GetString(experience, "endDate"),
            IsCurrent = GetBool(experience, "isCurrent"),
            Description = string.Join(" ", GetStringArray(experience, "responsibilities").Take(2)),
            Achievements = achievements,
            Technologies = GetStringArray(experience, "toolsAndTaxonomiesUsed")
        };
    }

    private static Education MapEducation(BsonDocument education)
    {
        return new Education
        {
            Institution = GetString(education, "institution"),
            Degree = GetString(education, "degree"),
            FieldOfStudy = GetString(education, "majorOrFieldOfStudy"),
            StartDate = GetString(education, "startDate"),
            EndDate = GetString(education, "endDate"),
            Grade = GetString(education, "gpa"),
            Description = GetString(education, "location")
        };
    }

    private static Certification MapCertification(BsonDocument certification)
    {
        return new Certification
        {
            Name = GetString(certification, "name"),
            Issuer = GetString(certification, "issuer"),
            IssueDate = GetString(certification, "year"),
            ExpiryDate = string.Empty,
            CredentialId = string.Empty,
            CredentialUrl = string.Empty
        };
    }

    private static SkillsMatrix MapSkillsMatrix(BsonDocument profile)
    {
        var skillsMatrix = GetDocument(profile, "skillsMatrix") ?? new BsonDocument();
        var categories = new[] { "programmingLanguages", "frameworks", "cloudPlatforms", "databases", "devOpsTools", "testingTools", "businessTools", "industryTools" };
        var skills = categories
            .SelectMany(category => GetStringArray(skillsMatrix, category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(skill => new SkillProficiency { Skill = skill, Level = "Experienced", Years = 0 })
            .ToList();

        return new SkillsMatrix
        {
            TechnicalProficiency = skills,
            DomainExpertise = GetStringArray(GetDocument(profile, "careerClassification") ?? new BsonDocument(), "industry")
                .Select(skill => new SkillProficiency { Skill = skill, Level = "Experienced", Years = 0 })
                .ToList()
        };
    }

    private static (string firstName, string lastName) SplitFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (string.Empty, string.Empty);
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? (parts[0], string.Empty)
            : (parts[0], string.Join(" ", parts.Skip(1)));
    }

    private static BsonDocument? GetDocument(BsonDocument document, string name)
    {
        return document.TryGetValue(name, out var value) && value.IsBsonDocument ? value.AsBsonDocument : null;
    }

    private static List<BsonDocument> GetDocumentArray(BsonDocument document, string name)
    {
        if (!document.TryGetValue(name, out var value) || !value.IsBsonArray)
        {
            return new List<BsonDocument>();
        }

        return value.AsBsonArray.Where(item => item.IsBsonDocument).Select(item => item.AsBsonDocument).ToList();
    }

    private static List<string> GetStringArray(BsonDocument document, string name)
    {
        if (!document.TryGetValue(name, out var value))
        {
            return new List<string>();
        }

        if (value.IsString)
        {
            return new List<string> { value.AsString };
        }

        if (!value.IsBsonArray)
        {
            return new List<string>();
        }

        return value.AsBsonArray
            .Where(item => !item.IsBsonNull)
            .Select(item => item.ToString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private static string GetString(BsonDocument document, string name, string defaultValue = "")
    {
        return document.TryGetValue(name, out var value) && !value.IsBsonNull ? value.ToString() ?? defaultValue : defaultValue;
    }

    private static int GetInt(BsonDocument document, string name)
    {
        return document.TryGetValue(name, out var value) && value.IsNumeric ? value.ToInt32() : 0;
    }

    private static decimal GetDecimal(BsonDocument document, string name)
    {
        return document.TryGetValue(name, out var value) && value.IsNumeric ? Convert.ToDecimal(value.ToDouble()) : 0;
    }

    private static bool GetBool(BsonDocument document, string name)
    {
        return document.TryGetValue(name, out var value) && value.IsBoolean && value.AsBoolean;
    }

    private static DateTime GetDateTime(BsonDocument document, string name)
    {
        return document.TryGetValue(name, out var value) && value.IsValidDateTime ? value.ToUniversalTime() : DateTime.UtcNow;
    }

    private static string GetOnlineProfileUrl(BsonDocument candidate, string platform)
    {
        return GetDocumentArray(candidate, "onlineProfiles")
            .FirstOrDefault(item => GetString(item, "platform").Equals(platform, StringComparison.OrdinalIgnoreCase)) is { } profile
            ? GetString(profile, "url")
            : string.Empty;
    }
}
