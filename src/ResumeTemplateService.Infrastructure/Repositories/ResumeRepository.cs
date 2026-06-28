using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ResumeTemplateService.Infrastructure.Repositories;

public class ResumeRepository : IResumeRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly IMongoCollection<BsonDocument> _editedCollection;
    private readonly ILogger<ResumeRepository> _logger;

    public ResumeRepository(
        IMongoDatabase database,
        string collectionName,
        string? editedCollectionName,
        ILogger<ResumeRepository> logger)
    {
        _logger = logger;
        _collection = database.GetCollection<BsonDocument>(collectionName);
        _editedCollection = database.GetCollection<BsonDocument>(
            string.IsNullOrWhiteSpace(editedCollectionName) ? "edited_resume" : editedCollectionName);
    }

    public async Task<ResumeProfile?> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Fetching resume with id: {ResumeId}", id);

            var filter = BuildIdFilter(id);
            var document = await _collection.Find(filter).FirstOrDefaultAsync();
            if (document != null)
            {
                document = await GetLatestEditedCopyAsync(id) ?? document;
            }
            else
            {
                document = await _editedCollection.Find(filter).FirstOrDefaultAsync();
            }

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

            if (count > 0)
            {
                return true;
            }

            return await _editedCollection.CountDocumentsAsync(filter) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if resume exists: {ResumeId}", id);
            throw;
        }
    }

    public async Task<ResumeProfile> SaveEditedAsync(
        string originalResumeId,
        string editedResumeJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(originalResumeId))
            {
                throw new ArgumentException("Original resume id is required.", nameof(originalResumeId));
            }

            if (string.IsNullOrWhiteSpace(editedResumeJson))
            {
                throw new ArgumentException("Edited resume content is required.", nameof(editedResumeJson));
            }

            var incomingDocument = BsonDocument.Parse(editedResumeJson);
            var document = NormalizeEditedDocument(originalResumeId.Trim(), incomingDocument);
            var latestEditedCopy = await GetLatestEditedCopyAsync(originalResumeId.Trim(), cancellationToken);
            var nextVersion = latestEditedCopy is null ? 1 : GetInt(latestEditedCopy, "version") + 1;
            var now = DateTime.UtcNow;

            document["_id"] = ObjectId.GenerateNewId();
            document["id"] = originalResumeId.Trim();
            document["resumeId"] = originalResumeId.Trim();
            document["originalResumeId"] = originalResumeId.Trim();
            document["version"] = nextVersion;
            document["status"] = "edited";
            document["updatedAt"] = now;

            if (!document.Contains("createdAt"))
            {
                document["createdAt"] = now;
            }

            await _editedCollection.InsertOneAsync(document, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Saved edited resume copy - ResumeId: {ResumeId}, Version: {Version}",
                originalResumeId,
                nextVersion);

            return MapDocument(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving edited resume with id: {ResumeId}", originalResumeId);
            throw;
        }
    }

    private async Task<BsonDocument?> GetLatestEditedCopyAsync(
        string originalResumeId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("originalResumeId", originalResumeId),
            Builders<BsonDocument>.Filter.Eq("resumeId", originalResumeId),
            Builders<BsonDocument>.Filter.Eq("id", originalResumeId));
        var sort = Builders<BsonDocument>.Sort
            .Descending("updatedAt")
            .Descending("createdAt");

        return await _editedCollection.Find(filter).Sort(sort).FirstOrDefaultAsync(cancellationToken);
    }

    private static FilterDefinition<BsonDocument> BuildIdFilter(string id)
    {
        var filters = new List<FilterDefinition<BsonDocument>>
        {
            Builders<BsonDocument>.Filter.Eq("_id", id),
            Builders<BsonDocument>.Filter.Eq("id", id),
            Builders<BsonDocument>.Filter.Eq("resumeId", id),
            Builders<BsonDocument>.Filter.Eq("originalResumeId", id)
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
            var profile = document["profile"].AsBsonDocument;
            if (profile.Contains("data") && profile["data"].IsBsonDocument)
            {
                return MapParsedResumeDocument(document);
            }

            return MapParserDocument(document);
        }

        return BsonSerializer.Deserialize<ResumeProfile>(document);
    }

    private static BsonDocument NormalizeEditedDocument(string originalResumeId, BsonDocument incomingDocument)
    {
        var document = incomingDocument.DeepClone().AsBsonDocument;

        if (document.Contains("_id"))
        {
            document.Remove("_id");
        }

        if (document.Contains("profile") && document["profile"].IsBsonDocument)
        {
            return document;
        }

        if (document.Contains("data") && document["data"].IsBsonDocument)
        {
            return new BsonDocument
            {
                ["id"] = originalResumeId,
                ["resumeId"] = originalResumeId,
                ["profile"] = document
            };
        }

        return new BsonDocument
        {
            ["id"] = originalResumeId,
            ["resumeId"] = originalResumeId,
            ["profile"] = new BsonDocument
            {
                ["data"] = document
            }
        };
    }

    private static ResumeProfile MapParsedResumeDocument(BsonDocument document)
    {
        var profile = GetDocument(document, "profile") ?? new BsonDocument();
        var data = GetDocument(profile, "data") ?? new BsonDocument();
        var nameParts = SplitFullName(GetString(data, "name"));
        var summary = GetString(data, "summary", GetSectionStringItems(data, "summary").FirstOrDefault() ?? string.Empty);
        var skills = GetSectionDocumentItems(data, "skill")
            .Select(skill => GetString(skill, "name"))
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var links = GetSectionDocumentItems(data, "link");

        return new ResumeProfile
        {
            Id = GetDocumentId(document),
            CandidateProfile = new CandidateProfile
            {
                FirstName = nameParts.firstName,
                LastName = nameParts.lastName,
                Email = GetString(data, "email"),
                Phone = GetString(data, "phone"),
                Location = GetString(data, "location"),
                Country = GetString(data, "location"),
                LinkedInUrl = GetLinkUrl(links, "linkedin"),
                GitHubUrl = GetLinkUrl(links, "github"),
                PortfolioUrl = GetLinkUrl(links, "portfolio")
            },
            CareerClassification = new CareerClassification
            {
                CurrentTitle = GetString(data, "title"),
                YearsOfExperience = 0,
                CareerLevel = string.Empty,
                Industry = string.Empty,
                Specialization = string.Empty
            },
            CareerProgression = new CareerProgression
            {
                ProgressionSummary = string.Empty,
                CareerTrajectory = new List<string>(),
                GrowthAreas = new List<string>()
            },
            CoreSkills = new CoreSkills
            {
                PrimarySkills = skills,
                SecondarySkills = new List<string>(),
                SoftSkills = new List<string>(),
                Languages = GetSectionStringItems(data, "language"),
                SkillsMatrix = new SkillsMatrix
                {
                    TechnicalProficiency = skills
                        .Select(skill => new SkillProficiency { Skill = skill, Level = "Experienced", Years = 0 })
                        .ToList(),
                    DomainExpertise = new List<SkillProficiency>()
                }
            },
            WorkExperience = GetSectionDocumentItems(data, "experience").Select(MapWorkExperience).ToList(),
            Education = GetSectionDocumentItems(data, "education").Select(MapEducation).ToList(),
            Certifications = GetSectionDocumentItems(data, "course").Select(MapCertification).ToList(),
            LeadershipHighlights = new List<string>(),
            TechnicalHighlights = new List<string>(),
            IndustryHighlights = new List<string>(),
            ResumeBlocks = new ResumeBlocks
            {
                ProfessionalSummary = summary,
                Headline = GetString(data, "title"),
                KeyAchievements = new List<string>()
            },
            AtsAnalysis = new AtsAnalysis
            {
                AtsScore = 0,
                KeywordMatches = 0,
                MissingKeywords = new List<string>(),
                FormattingIssues = new List<string>()
            },
            CreatedAt = GetDateTime(document, "createdAt"),
            UpdatedAt = GetDateTime(document, "updatedAt")
        };
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
        var summaryPoints = GetStringArray(resumeBlocks, "executiveSummary")
            .Concat(GetStringArray(profile, "professionalSummaryPoints"))
            .Where(point => !string.IsNullOrWhiteSpace(point))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var nameParts = SplitFullName(GetString(candidate, "fullName"));

        return new ResumeProfile
        {
            Id = GetDocumentId(document),
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
                ProfessionalSummary = string.Join(Environment.NewLine, summaryPoints),
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
        var responsibilities = GetStringArray(experience, "responsibilities");
        var achievements = responsibilities
            .Concat(GetStringArray(experience, "achievements"))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new WorkExperience
        {
            Company = GetString(experience, "companyOrOrganization", GetString(experience, "company")),
            JobTitle = GetString(experience, "role", GetString(experience, "position")),
            EmploymentType = GetString(experience, "employmentType", GetString(experience, "jobType")),
            Location = GetString(experience, "location"),
            StartDate = GetString(experience, "startDate", GetString(experience, "start")),
            EndDate = GetString(experience, "endDate", GetString(experience, "end")),
            IsCurrent = GetBool(experience, "isCurrent") || GetString(experience, "end").Equals("Present", StringComparison.OrdinalIgnoreCase),
            Description = string.Join(" ", responsibilities),
            Achievements = achievements,
            Technologies = GetStringArray(experience, "toolsAndTaxonomiesUsed")
        };
    }

    private static Education MapEducation(BsonDocument education)
    {
        return new Education
        {
            Institution = GetString(education, "institution", GetString(education, "school")),
            Degree = GetString(education, "degree"),
            FieldOfStudy = GetString(education, "majorOrFieldOfStudy", GetString(education, "faculty")),
            StartDate = GetString(education, "startDate", GetString(education, "start")),
            EndDate = GetString(education, "endDate", GetString(education, "end", GetString(education, "years"))),
            Grade = GetString(education, "gpa"),
            Description = GetString(education, "location")
        };
    }

    private static Certification MapCertification(BsonDocument certification)
    {
        return new Certification
        {
            Name = GetString(certification, "name", GetString(certification, "course")),
            Issuer = GetString(certification, "issuer", GetString(certification, "institution")),
            IssueDate = GetString(certification, "year", GetString(certification, "start")),
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

    private static string GetDocumentId(BsonDocument document)
    {
        return GetString(document, "id", GetString(document, "resumeId", document.GetValue("_id", ObjectId.Empty).ToString() ?? string.Empty));
    }

    private static BsonDocument? GetSection(BsonDocument data, string type)
    {
        return GetDocumentArray(data, "sections")
            .FirstOrDefault(section => GetString(section, "type").Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    private static List<BsonDocument> GetSectionDocumentItems(BsonDocument data, string type)
    {
        var section = GetSection(data, type);
        return section is null ? new List<BsonDocument>() : GetDocumentArray(section, "items");
    }

    private static List<string> GetSectionStringItems(BsonDocument data, string type)
    {
        var section = GetSection(data, type);
        return section is null ? new List<string>() : GetStringArray(section, "items");
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

    private static string GetLinkUrl(IEnumerable<BsonDocument> links, string label)
    {
        return links.FirstOrDefault(item =>
            GetString(item, "label").Contains(label, StringComparison.OrdinalIgnoreCase)) is { } link
            ? GetString(link, "link")
            : string.Empty;
    }
}
