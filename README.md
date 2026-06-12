# Resume Template Service

A production-ready .NET 9 microservice for rendering resume templates using MongoDB data and RazorLight. This service reads parsed resume profiles, maps them to a view model, applies Razor templates, and returns HTML previews that can be rendered in an Angular application.

## Features

- **Clean Architecture**: Well-organized project structure with clear separation of concerns
- **ASP.NET Core 9 Web API**: Modern and performant web framework
- **MongoDB Integration**: Scalable NoSQL database for resume storage
- **RazorLight Template Engine**: Fast and flexible template rendering
- **Repository Pattern**: Abstracted data access layer
- **SOLID Principles**: Maintainable and extensible codebase
- **Async/Await**: Fully asynchronous operations
- **Structured Logging**: Comprehensive logging with Serilog
- **Health Checks**: Built-in health check endpoints
- **Docker Support**: Ready for containerized deployment
- **Swagger/OpenAPI**: Interactive API documentation
- **CORS Support**: Ready for Angular/Frontend integration
- **Global Exception Handling**: Centralized error handling middleware

## Project Structure

```
ResumeTemplateService/
├── src/
│   ├── ResumeTemplateService.Api/              # Web API project
│   │   ├── Controllers/                        # API endpoints
│   │   ├── DTOs/                               # Data transfer objects
│   │   ├── Extensions/                         # Service & app extensions
│   │   ├── Middleware/                         # Custom middleware
│   │   ├── Program.cs                          # Application startup
│   │   └── appsettings*.json                   # Configuration
│   │
│   ├── ResumeTemplateService.Application/      # Application layer
│   │   ├── Commands/                           # CQRS commands
│   │   ├── Queries/                            # CQRS queries
│   │   ├── Interfaces/                         # Service contracts
│   │   └── Mappings/                           # Object mappings
│   │
│   ├── ResumeTemplateService.Domain/           # Domain layer
│   │   ├── Entities/                           # Domain entities
│   │   └── ValueObjects/                       # Value objects
│   │
│   └── ResumeTemplateService.Infrastructure/   # Infrastructure layer
│       ├── Repositories/                       # Data repositories
│       ├── TemplateRendering/                  # Template rendering
│       └── Configuration/                      # Infrastructure config
│
├── templates/
│   ├── professional-dark-blue/
│   │   ├── template.cshtml
│   │   └── style.css
│   │
│   └── modern-minimal/
│       ├── template.cshtml
│       └── style.css
│
├── Dockerfile
├── docker-compose.yml
├── ResumeTemplateService.sln
└── README.md
```

## Prerequisites

- .NET 9.0 SDK or later
- MongoDB 4.4+
- Docker & Docker Compose (optional, for containerized deployment)

## Getting Started

### Local Development

1. **Clone the repository**
   ```bash
   cd ResumeTemplateService
   ```

2. **Install MongoDB** (if not already installed)
   ```bash
   # Using Docker
   docker run -d -p 27017:27017 --name mongodb mongo:7.0
   ```

3. **Configure MongoDB connection** in `appsettings.Development.json`
   ```json
   {
     "MongoDB": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "ResumeDb"
     }
   }
   ```

4. **Restore dependencies**
   ```bash
   dotnet restore
   ```

5. **Build the solution**
   ```bash
   dotnet build
   ```

6. **Run the API**
   ```bash
   cd src/ResumeTemplateService.Api
   dotnet run
   ```

7. **Access Swagger UI**
   ```
   http://localhost:5000
   ```

### Docker Deployment

1. **Build and run with Docker Compose**
   ```bash
   docker-compose up -d
   ```

2. **Access the API**
   ```
   http://localhost:8080
   ```

3. **Stop the services**
   ```bash
   docker-compose down
   ```

## API Endpoints

### Get Available Templates

```http
GET /api/templates
```

**Response:**
```json
[
  {
    "id": "professional-dark-blue",
    "name": "Professional Dark Blue",
    "description": "A professional dark blue themed resume template",
    "previewUrl": "/api/templates/professional-dark-blue/preview"
  },
  {
    "id": "modern-minimal",
    "name": "Modern Minimal",
    "description": "A modern minimal resume template",
    "previewUrl": "/api/templates/modern-minimal/preview"
  }
]
```

### Render Resume Preview

```http
POST /api/resumes/preview
Content-Type: application/json

{
  "resumeId": "507f1f77bcf86cd799439011",
  "templateId": "professional-dark-blue"
}
```

**Response:**
```json
{
  "templateId": "professional-dark-blue",
  "html": "<html>...</html>"
}
```

### Health Check

```http
GET /health
```

**Response:**
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "mongodb",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0234567"
    }
  ],
  "totalDuration": "00:00:00.0567890"
}
```

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ResumeDb",
    "CollectionName": "ResumeProfiles"
  }
}
```

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Set to `Development`, `Staging`, or `Production`
- `ASPNETCORE_URLS`: URL the API listens on (default: `http://+:5000`)
- `MongoDB__ConnectionString`: MongoDB connection string
- `MongoDB__DatabaseName`: MongoDB database name

## Database Setup

The service expects resume documents in MongoDB with the following structure:

```json
{
  "_id": ObjectId,
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
    "progression_summary": "...",
    "career_trajectory": [...],
    "growth_areas": [...]
  },
  "core_skills": {
    "primary_skills": [...],
    "secondary_skills": [...],
    "soft_skills": [...],
    "languages": [...],
    "skills_matrix": {
      "technical_proficiency": [...],
      "domain_expertise": [...]
    }
  },
  "work_experience": [...],
  "education": [...],
  "certifications": [...],
  "leadership_highlights": [...],
  "technical_highlights": [...],
  "industry_highlights": [...],
  "resume_blocks": {
    "professional_summary": "...",
    "headline": "...",
    "key_achievements": [...]
  },
  "ats_analysis": {
    "ats_score": 85.5,
    "keyword_matches": 42,
    "missing_keywords": [...],
    "formatting_issues": [...]
  },
  "created_at": ISODate,
  "updated_at": ISODate
}
```

## Angular Integration

To use the rendered HTML in an Angular application:

```typescript
// service.ts
export class ResumeService {
  constructor(private http: HttpClient) {}

  renderResumePreview(resumeId: string, templateId: string) {
    return this.http.post<RenderResumePreviewResponse>(
      '/api/resumes/preview',
      { resumeId, templateId }
    );
  }

  getAvailableTemplates() {
    return this.http.get<TemplateDto[]>('/api/templates');
  }
}

// component.ts
export class ResumePreviewComponent implements OnInit {
  resumeHtml: string = '';
  selectedTemplate: string = 'professional-dark-blue';

  constructor(
    private resumeService: ResumeService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit() {
    this.renderPreview();
  }

  renderPreview() {
    const resumeId = this.route.snapshot.params['id'];
    this.resumeService
      .renderResumePreview(resumeId, this.selectedTemplate)
      .subscribe(response => {
        this.resumeHtml = response.html;
      });
  }
}

// template.html
<iframe [srcdoc]="resumeHtml"></iframe>
```

## Architecture Highlights

### Clean Architecture Layers

1. **Domain Layer** (`ResumeTemplateService.Domain`)
   - Contains pure business logic
   - Database-agnostic entities
   - No external dependencies

2. **Application Layer** (`ResumeTemplateService.Application`)
   - CQRS pattern (Commands & Queries)
   - Use case handlers
   - Business rule orchestration
   - Mapping logic

3. **Infrastructure Layer** (`ResumeTemplateService.Infrastructure`)
   - MongoDB repository implementation
   - RazorLight template rendering
   - Database queries and operations

4. **API Layer** (`ResumeTemplateService.Api`)
   - HTTP endpoints
   - Request/response handling
   - Dependency injection setup
   - Middleware configuration

### SOLID Principles

- **S**ingle Responsibility: Each class has one reason to change
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Interfaces are properly implemented
- **I**nterface Segregation: Focused, client-specific interfaces
- **D**ependency Inversion: Depends on abstractions, not concretions

## Performance Considerations

- **Connection Pooling**: MongoDB driver automatically manages connection pools
- **Template Caching**: RazorLight caches compiled templates
- **Async Operations**: All I/O operations are asynchronous
- **Structured Logging**: Minimal performance impact with proper log levels
- **Docker Optimization**: Multi-stage build reduces image size

## Security

- Input validation on all API endpoints
- CORS policy configured for Angular frontend
- Non-root Docker user
- Health checks for MongoDB connectivity
- Error handling without sensitive information exposure

## Logging

The application uses structured logging with Serilog:

```csharp
_logger.LogInformation("Rendering template: {TemplateId}", templateId);
_logger.LogError(ex, "Error rendering template: {TemplateId}", templateId);
_logger.LogDebug("Checking if template exists: {TemplateId}", templateId);
```

Log levels: Debug, Information, Warning, Error, Critical

## Contributing

1. Follow SOLID principles
2. Write async code
3. Add comprehensive logging
4. Include XML documentation on public methods
5. Test locally before pushing

## License

[Your License Here]

## Support

For issues or questions, please contact the development team.

## Deployment Checklist

- [ ] Update MongoDB connection string for production
- [ ] Set `ASPNETCORE_ENVIRONMENT` to `Production`
- [ ] Enable CORS for production domain
- [ ] Configure proper logging level
- [ ] Set up MongoDB backups
- [ ] Configure API rate limiting (if needed)
- [ ] Test health checks
- [ ] Verify template paths in container
- [ ] Set resource limits in docker-compose
- [ ] Enable monitoring and alerting
