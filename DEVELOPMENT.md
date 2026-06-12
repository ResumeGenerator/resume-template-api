# Resume Template Service - Development Guide

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- MongoDB 4.4+
- Visual Studio Code or Visual Studio 2022

### Local Development Setup

```bash
# 1. Start MongoDB
docker run -d -p 27017:27017 --name mongodb mongo:7.0

# 2. Navigate to project
cd src/ResumeTemplateService.Api

# 3. Restore and run
dotnet restore
dotnet run

# 4. Open browser
# Swagger UI: http://localhost:5000
# Health Check: http://localhost:5000/health
```

## Project Layout

### Domain Layer
- **Entities**: `ResumeProfile.cs` - Main resume domain model with all nested entities
- **ValueObjects**: `ResumeViewModel.cs` - Simplified view model for templates

### Application Layer
- **Commands**: `RenderResumeTemplateCommand.cs` - Render resume with template
- **Queries**: `GetAvailableTemplatesQuery.cs` - List available templates
- **Interfaces**: Service contracts for repositories, mappers, and renderers
- **Mappings**: `ResumeMapper.cs` - Maps domain model to view model

### Infrastructure Layer
- **Repositories**: `ResumeRepository.cs` - MongoDB data access
- **Template Rendering**: 
  - `RazorTemplateRenderer.cs` - RazorLight integration
  - `TemplateProvider.cs` - Template discovery and metadata

### API Layer
- **Controllers**: 
  - `ResumesController.cs` - Resume preview endpoint
  - `TemplatesController.cs` - Template list endpoint
- **Middleware**: `GlobalExceptionMiddleware.cs` - Error handling
- **Extensions**: Dependency injection and app configuration

## Adding a New Template

1. **Create template directory**
   ```bash
   mkdir templates/my-template
   ```

2. **Create Razor template**
   ```bash
   # templates/my-template/template.cshtml
   @using ResumeTemplateService.Domain.ValueObjects
   @model ResumeViewModel
   
   <!-- Your template HTML here -->
   ```

3. **Add optional styles**
   ```bash
   # templates/my-template/style.css
   /* Your CSS here */
   ```

4. **Update TemplateProvider** (optional, for better descriptions)
   ```csharp
   // In TemplateProvider.cs GetTemplateDescription method
   case "my-template" => "Your template description",
   ```

5. **Test template**
   ```bash
   POST /api/resumes/preview
   {
     "resumeId": "<valid-mongo-id>",
     "templateId": "my-template"
   }
   ```

## Database Sample Document

For testing, insert this sample resume in MongoDB:

```json
{
  "_id": ObjectId("507f1f77bcf86cd799439011"),
  "candidate_profile": {
    "first_name": "John",
    "last_name": "Doe",
    "email": "john@example.com",
    "phone": "+1-555-0123",
    "location": "San Francisco, CA",
    "city": "San Francisco",
    "state": "CA",
    "country": "USA",
    "linkedin_url": "https://linkedin.com/in/johndoe",
    "github_url": "https://github.com/johndoe",
    "portfolio_url": "https://johndoe.dev"
  },
  "career_classification": {
    "current_title": "Senior Software Engineer",
    "years_of_experience": 8,
    "career_level": "Senior",
    "industry": "Technology",
    "specialization": "Full Stack Development"
  },
  "career_progression": {
    "progression_summary": "Progressed from Junior Developer to Senior Engineer with expertise in cloud architecture",
    "career_trajectory": ["Junior Developer", "Mid-level Developer", "Senior Developer", "Senior Engineer"],
    "growth_areas": ["Leadership", "System Design", "Cloud Architecture"]
  },
  "core_skills": {
    "primary_skills": [".NET", "C#", "SQL", "Azure", "Docker"],
    "secondary_skills": ["JavaScript", "React", "MongoDB", "Kubernetes"],
    "soft_skills": ["Leadership", "Communication", "Problem Solving", "Team Collaboration"],
    "languages": ["English", "Spanish"],
    "skills_matrix": {
      "technical_proficiency": [
        {"skill": ".NET Core", "level": "Expert", "years": 6},
        {"skill": "Azure", "level": "Advanced", "years": 5},
        {"skill": "Docker", "level": "Advanced", "years": 4}
      ],
      "domain_expertise": [
        {"skill": "Microservices", "level": "Expert", "years": 5},
        {"skill": "Cloud Architecture", "level": "Advanced", "years": 4}
      ]
    }
  },
  "work_experience": [
    {
      "company": "Tech Corp",
      "job_title": "Senior Software Engineer",
      "employment_type": "Full-time",
      "location": "San Francisco, CA",
      "start_date": "2021-01",
      "end_date": "",
      "is_current": true,
      "description": "Leading development of cloud-based microservices",
      "achievements": [
        "Architected and implemented microservices platform serving 1M+ requests/day",
        "Mentored 5 junior developers and improved code quality by 40%"
      ],
      "technologies": [".NET 9", "Azure", "Docker", "Kubernetes", "MongoDB"]
    },
    {
      "company": "Software Inc",
      "job_title": "Software Engineer",
      "employment_type": "Full-time",
      "location": "New York, NY",
      "start_date": "2018-06",
      "end_date": "2020-12",
      "is_current": false,
      "description": "Developed full-stack web applications",
      "achievements": [
        "Built REST APIs serving 100K+ daily users",
        "Reduced API response time by 60% through optimization"
      ],
      "technologies": [".NET Framework", "ASP.NET", "SQL Server", "Angular"]
    }
  ],
  "education": [
    {
      "institution": "State University",
      "degree": "Bachelor of Science",
      "field_of_study": "Computer Science",
      "start_date": "2014-09",
      "end_date": "2018-05",
      "grade": "3.8 GPA",
      "activities": ["Computer Science Club", "Hackathon Participant"],
      "description": "Focused on software engineering and algorithms"
    }
  ],
  "certifications": [
    {
      "name": "Azure Solutions Architect Expert",
      "issuer": "Microsoft",
      "issue_date": "2022-06",
      "expiry_date": "2025-06",
      "credential_id": "ABC123",
      "credential_url": "https://microsoft.com/credentials/abc123"
    },
    {
      "name": "Kubernetes Application Developer",
      "issuer": "Linux Foundation",
      "issue_date": "2021-12",
      "expiry_date": "",
      "credential_id": "DEF456",
      "credential_url": "https://cncf.io/credentials/def456"
    }
  ],
  "leadership_highlights": [
    "Led team of 8 engineers in delivering critical platform",
    "Mentored 5 junior developers to senior level",
    "Drove architectural improvements reducing costs by 30%"
  ],
  "technical_highlights": [
    "Designed and implemented microservices platform",
    "Optimized database queries reducing latency by 70%",
    "Implemented CI/CD pipeline with GitOps"
  ],
  "industry_highlights": [
    "Speaker at tech conferences on cloud architecture",
    "Open source contributor with 200+ GitHub stars",
    "Technical writer published in major tech blogs"
  ],
  "resume_blocks": {
    "professional_summary": "Senior Software Engineer with 8 years of experience building scalable cloud-based systems. Expertise in microservices architecture, cloud platforms, and full-stack development. Passionate about mentoring and driving technical excellence.",
    "headline": "Senior Software Engineer | Cloud Architecture | Microservices",
    "key_achievements": [
      "Led development of microservices platform serving 1M+ requests daily",
      "Reduced API latency by 60% through optimization",
      "Mentored 5 junior developers to senior roles"
    ]
  },
  "ats_analysis": {
    "ats_score": 87.5,
    "keyword_matches": 45,
    "missing_keywords": ["Machine Learning", "Data Science"],
    "formatting_issues": []
  },
  "created_at": ISODate("2024-01-15T10:30:00Z"),
  "updated_at": ISODate("2024-01-15T10:30:00Z")
}
```

## API Testing

### Using curl

```bash
# Get templates
curl http://localhost:5000/api/templates

# Render resume
curl -X POST http://localhost:5000/api/resumes/preview \
  -H "Content-Type: application/json" \
  -d '{
    "resumeId": "507f1f77bcf86cd799439011",
    "templateId": "professional-dark-blue"
  }'

# Check health
curl http://localhost:5000/health
```

### Using Postman

1. Import the provided Postman collection (if available)
2. Set variables: `base_url`, `resume_id`, `template_id`
3. Test each endpoint

## Troubleshooting

### MongoDB Connection Issues
```bash
# Check if MongoDB is running
docker ps | grep mongodb

# View logs
docker logs mongodb

# Restart MongoDB
docker restart mongodb
```

### Template Not Found
- Verify template directory exists: `templates/template-id/template.cshtml`
- Check template file permissions
- Restart the application

### Slow Rendering
- Check MongoDB query performance
- Verify template complexity
- Monitor resource usage: `docker stats`

## Performance Tips

1. **Caching**: RazorLight automatically caches compiled templates
2. **Async**: All I/O operations use async/await
3. **Connection Pooling**: MongoDB driver manages connection pools
4. **Logging**: Use appropriate log levels (not Debug in production)

## Code Quality

```bash
# Format code
dotnet format

# Run static analysis
dotnet build /p:TreatWarningsAsErrors=true

# View test results
dotnet test --logger "console;verbosity=normal"
```

## Git Workflow

```bash
# Clone repository
git clone <repository-url>

# Create feature branch
git checkout -b feature/template-name

# Commit changes
git commit -m "feat: add new template"

# Push and create PR
git push origin feature/template-name
```

## Docker Commands

```bash
# Build image
docker build -t resume-template-api:latest .

# Run container
docker run -p 8080:8080 resume-template-api:latest

# View logs
docker logs <container-id>

# Compose up
docker-compose up -d --build

# Compose down
docker-compose down -v
```

## Additional Resources

- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [MongoDB Documentation](https://docs.mongodb.com/)
- [RazorLight Documentation](https://github.com/toddams/RazorLight)
