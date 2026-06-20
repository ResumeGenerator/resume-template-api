using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ResumeTemplateService.Application.Interfaces;
using ResumeTemplateService.Domain.ValueObjects;

namespace ResumeTemplateService.Infrastructure.TemplateRendering;

public class DocxWordRenderer : IWordRenderer
{
    private const string Blue = "17376D";
    private const string DarkBlue = "111827";
    private const string MutedBlue = "334155";
    private const string LightBlue = "DCE7F5";
    private const string SnapshotBlue = "254B83";
    private const string Gold = "D7A82F";
    private const string BulletGold = "B8891D";
    private const int PageWidth = 11906;
    private const int PageHeight = 16838;
    private const int Margin = 720;

    public Task<byte[]> RenderWordAsync(ResumeViewModel resume, string templateId, CancellationToken cancellationToken = default)
    {
        if (resume == null)
        {
            throw new InvalidOperationException("Cannot render an empty resume as Word.");
        }

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var body = mainPart.Document.Body!;
            body.Append(BuildSectionProperties());

            AddHeader(body, resume);
            AddBulletSection(body, "Professional Summary", resume.SummaryPoints);
            AddSkillSection(body, resume.SkillGroups);

            var highlights = resume.TechnicalHighlights
                .Concat(resume.LeadershipHighlights)
                .Concat(resume.ProjectHighlights)
                .Concat(resume.IndustryHighlights)
                .Where(HasValue)
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            AddBulletSection(body, "Selected Highlights", highlights);
            AddExperienceSection(body, resume.Experience);
            AddEducationSection(body, resume.Education);
            AddCertificationSection(body, resume.Certifications);

            mainPart.Document.Save();
        }

        return Task.FromResult(stream.ToArray());
    }

    private static SectionProperties BuildSectionProperties()
    {
        return new SectionProperties(
            new PageSize { Width = PageWidth, Height = PageHeight },
            new PageMargin { Top = Margin, Right = Margin, Bottom = Margin, Left = Margin });
    }

    private static void AddHeader(Body body, ResumeViewModel resume)
    {
        var snapshotItems = BuildSnapshotItems(resume);
        var focusAreas = resume.CareerSnapshot.FocusAreas
            .Where(HasValue)
            .Select(item => item.Trim())
            .Distinct()
            .ToList();

        Add(body, Para(resume.PersonalInfo.FullName, 66, bold: true, color: "FFFFFF", fill: Blue, before: 420, after: 120, left: 520, right: 520));
        Add(body, Para(resume.Headline, 28, bold: true, color: LightBlue, fill: Blue, after: 210, left: 520, right: 520));
        Add(body, Para(BuildContactText(resume.PersonalInfo), 24, bold: true, color: "FFFFFF", fill: Blue, after: 250, left: 520, right: 520));

        if (snapshotItems.Any())
        {
            Add(body, Para(string.Join(" | ", snapshotItems), 24, bold: true, color: "FFFFFF", fill: SnapshotBlue, after: 80, left: 760, right: 760));
        }

        if (focusAreas.Any())
        {
            Add(body, Para($"Focus: {string.Join(", ", focusAreas)}", 23, color: "FFFFFF", fill: SnapshotBlue, after: 240, left: 760, right: 760));
        }

        Add(body, Para(string.Empty, 7, fill: Gold, after: 330));
    }

    private static void AddBulletSection(Body body, string title, IEnumerable<string> items)
    {
        var values = items
            .Where(HasValue)
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!values.Any())
        {
            return;
        }

        AddSectionTitle(body, title);
        foreach (var item in values)
        {
            Add(body, Bullet(item));
        }
    }

    private static void AddSkillSection(Body body, IReadOnlyCollection<SkillGroupViewModel> groups)
    {
        var visibleGroups = groups
            .Where(group => HasValue(group.Name) && group.Skills.Any(HasValue))
            .ToList();

        if (!visibleGroups.Any())
        {
            return;
        }

        AddSectionTitle(body, "Skills & Platforms");
        foreach (var group in visibleGroups)
        {
            var skills = string.Join(", ", group.Skills.Where(HasValue).Select(skill => skill.Trim()).Distinct());
            Add(body, LabelValue(group.Name, skills));
        }
    }

    private static void AddExperienceSection(Body body, IReadOnlyCollection<ExperienceViewModel> experiences)
    {
        var visibleExperiences = experiences
            .Where(exp => HasValue(exp.JobTitle) || HasValue(exp.Company))
            .ToList();

        if (!visibleExperiences.Any())
        {
            return;
        }

        AddSectionTitle(body, "Professional Experience");
        foreach (var exp in visibleExperiences)
        {
            Add(body, Para($"{exp.JobTitle}\t{exp.DateRange}", 26, bold: true, color: DarkBlue, after: 40, tabRight: true));

            var company = exp.Company + (HasValue(exp.Location) ? $" - {exp.Location}" : string.Empty);
            Add(body, Para(company, 23, bold: true, color: MutedBlue, after: 80));

            foreach (var achievement in exp.Achievements.Where(HasValue).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Add(body, Bullet(achievement));
            }

            if (exp.Technologies.Any(HasValue))
            {
                Add(body, Para($"Tech: {string.Join(", ", exp.Technologies.Where(HasValue).Distinct())}", 20, color: "475569", after: 160));
            }
        }
    }

    private static void AddEducationSection(Body body, IReadOnlyCollection<EducationViewModel> education)
    {
        var visibleEducation = education
            .Where(edu => HasValue(edu.Degree) || HasValue(edu.Institution))
            .ToList();

        if (!visibleEducation.Any())
        {
            return;
        }

        AddSectionTitle(body, "Education");
        foreach (var edu in visibleEducation)
        {
            Add(body, Para(edu.Degree, 23, bold: true, color: DarkBlue));
            Add(body, Para($"{edu.Institution} - {edu.FieldOfStudy} {edu.DateRange}".Trim(), 20, color: "475569", after: 130));
        }
    }

    private static void AddCertificationSection(Body body, IReadOnlyCollection<CertificationViewModel> certifications)
    {
        var visibleCertifications = certifications.Where(cert => HasValue(cert.Name)).ToList();
        if (!visibleCertifications.Any())
        {
            return;
        }

        AddSectionTitle(body, "Certifications");
        foreach (var cert in visibleCertifications)
        {
            Add(body, Para(cert.Name, 22, bold: true, color: DarkBlue));
            if (HasValue(cert.Issuer) || HasValue(cert.IssueDate))
            {
                Add(body, Para($"{cert.Issuer} {cert.IssueDate}".Trim(), 20, color: "475569", after: 110));
            }
        }
    }

    private static void AddSectionTitle(Body body, string title)
    {
        Add(body, Para(
            title.ToUpperInvariant(),
            24,
            bold: true,
            color: "FFFFFF",
            fill: Blue,
            before: 240,
            after: 160,
            left: 300,
            paragraphLeftBorderColor: Gold));
    }

    private static Paragraph Bullet(string text)
    {
        return new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "45" },
                new Indentation { Left = "220", Hanging = "120" }),
            Run(char.ConvertFromUtf32(0x2022), 22, color: BulletGold),
            Run($"  {text}", 22, color: DarkBlue));
    }

    private static Paragraph LabelValue(string label, string value)
    {
        return new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "85" }),
            Run(label, 22, bold: true, color: Blue),
            Run($"  {value}", 22, color: DarkBlue));
    }

    private static Paragraph Para(
        string? text,
        int size,
        bool bold = false,
        string color = DarkBlue,
        string? fill = null,
        int before = 0,
        int after = 0,
        int left = 0,
        int right = 0,
        bool tabRight = false,
        string? paragraphLeftBorderColor = null)
    {
        var props = new ParagraphProperties(
            new SpacingBetweenLines { Before = before.ToString(), After = after.ToString() });

        if (left > 0 || right > 0)
        {
            props.Append(new Indentation { Left = left.ToString(), Right = right.ToString() });
        }

        if (HasValue(fill))
        {
            props.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fill });
        }

        if (HasValue(paragraphLeftBorderColor))
        {
            props.Append(new ParagraphBorders(
                new LeftBorder { Val = BorderValues.Single, Color = paragraphLeftBorderColor, Size = 24, Space = 4 }));
        }

        if (tabRight)
        {
            props.Append(new Tabs(new TabStop { Val = TabStopValues.Right, Position = 10300 }));
        }

        return new Paragraph(props, Run(text ?? string.Empty, size, bold, color));
    }

    private static Run Run(string text, int size, bool bold = false, string color = DarkBlue)
    {
        var props = new RunProperties(
            new RunFonts { Ascii = "Arial", HighAnsi = "Arial" },
            new FontSize { Val = size.ToString() },
            new Color { Val = color });

        if (bold)
        {
            props.Append(new Bold());
        }

        return new Run(props, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static void Add(Body body, OpenXmlElement element)
    {
        body.InsertBefore(element, body.GetFirstChild<SectionProperties>());
    }

    private static List<string> BuildSnapshotItems(ResumeViewModel resume)
    {
        var snapshotItems = new List<string>();
        if (resume.CareerSnapshot.YearsOfExperience > 0)
        {
            snapshotItems.Add($"{resume.CareerSnapshot.YearsOfExperience}+ years");
        }

        AddIfPresent(snapshotItems, resume.CareerSnapshot.CareerLevel);
        AddIfPresent(snapshotItems, resume.CareerSnapshot.Industry);
        AddIfPresent(snapshotItems, resume.CareerSnapshot.Specialization);
        return snapshotItems;
    }

    private static string BuildContactText(PersonalInfoViewModel personalInfo)
    {
        var parts = new List<string>();
        AddIfPresent(parts, personalInfo.Email);
        AddIfPresent(parts, personalInfo.Phone);
        AddIfPresent(parts, personalInfo.Location);

        if (HasValue(personalInfo.LinkedInUrl))
        {
            parts.Add("LinkedIn");
        }

        if (HasValue(personalInfo.GitHubUrl))
        {
            parts.Add("GitHub");
        }

        if (HasValue(personalInfo.PortfolioUrl))
        {
            parts.Add("Portfolio");
        }

        return string.Join("  |  ", parts);
    }

    private static void AddIfPresent(ICollection<string> values, string? value)
    {
        if (HasValue(value))
        {
            values.Add(value!.Trim());
        }
    }

    private static bool HasValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}
