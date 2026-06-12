# Resume Template Service - Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Angular Frontend                          │
│              (Resume Preview Application)                    │
└────────────────────────┬────────────────────────────────────┘
                         │
                    HTTP/REST
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│            Resume Template Service API (.NET 9)              │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  API Layer (Controllers & DTOs)                              │
│  ├── ResumesController      (POST /api/resumes/preview)      │
│  └── TemplatesController    (GET /api/templates)             │
│                                                               │
│  Middleware                                                   │
│  ├── GlobalExceptionMiddleware   (Error handling)            │
│  ├── CORS                        (Frontend integration)      │
│  └── Health Checks               (/health endpoint)          │
│                                                               │
│  Application Layer                                            │
│  ├── Commands                                                │
│  │   └── RenderResumeTemplateCommand                         │
│  ├── Queries                                                 │
│  │   └── GetAvailableTemplatesQuery                          │
│  ├── Mappings                                                │
│  │   └── ResumeMapper (ResumeProfile → ResumeViewModel)      │
│  └── Interfaces                                              │
│      ├── IResumeRepository                                   │
│      ├── ITemplateRenderer                                   │
│      ├── ITemplateProvider                                   │
│      └── IResumeMapper                                       │
│                                                               │
│  Infrastructure Layer                                        │
│  ├── Repositories                                            │
│  │   └── ResumeRepository (MongoDB access)                   │
│  ├── Template Rendering                                      │
│  │   ├── RazorTemplateRenderer (RazorLight engine)           │
│  │   └── TemplateProvider (Template discovery)              │
│  └── Configuration                                           │
│                                                               │
│  Domain Layer                                                │
│  ├── Entities                                                │
│  │   └── ResumeProfile (and nested entities)                 │
│  └── ValueObjects                                            │
│      └── ResumeViewModel                                     │
│                                                               │
└────────────┬─────────────────────────────────┬───────────────┘
             │                                 │
        MongoDB Driver                  Template Engine
             │                                 │
             ▼                                 ▼
┌──────────────────────┐        ┌──────────────────────────┐
│  MongoDB Database    │        │  Resume Templates        │
│                      │        │  (Razor + CSS)           │
│ ResumeDb             │        │                          │
│ ├── ResumeProfiles   │        │ ├── professional-dark-   │
│ │   (Collection)     │        │ │    blue/               │
│ └── ...              │        │ │    - template.cshtml   │
│                      │        │ │    - style.css         │
│                      │        │ │                        │
└──────────────────────┘        │ ├── modern-minimal/      │
                                │ │    - template.cshtml   │
                                │ │    - style.css         │
                                │ └── ...                  │
                                │                          │
                                └──────────────────────────┘
```

## Data Flow

### Resume Preview Rendering Flow

```
1. Angular Frontend
   │
   └─► POST /api/resumes/preview
       {
         "resumeId": "507f1f77bcf86cd799439011",
         "templateId": "professional-dark-blue"
       }
       │
       ▼
2. ResumesController.RenderResumePreview()
   ├─ Validate request
   └─ Create RenderResumeTemplateCommand
       │
       ▼
3. RenderResumeTemplateCommandHandler
   ├─ IResumeRepository.GetByIdAsync(resumeId)
   │  │
   │  └─► MongoDB: Find ResumeProfile by _id
   │
   ├─ ITemplateRenderer.TemplateExistsAsync(templateId)
   │
   ├─ IResumeMapper.Map(ResumeProfile → ResumeViewModel)
   │  └─ Extract and transform data for template
   │
   └─ ITemplateRenderer.RenderAsync(templateId, viewModel)
      │
      ├─► Load template from filesystem
      ├─► RazorLight.CompileRenderAsync()
      │   └─ Process Razor syntax with viewModel
      │
      └─► Return rendered HTML
       │
       ▼
4. ResumesController Returns Response
   {
     "templateId": "professional-dark-blue",
     "html": "<html>...</html>"
   }
   │
   └─► Angular Frontend
       └─► Display in <iframe [srcdoc]="resumeHtml"></iframe>
```

## Database Schema (MongoDB)

```
ResumeProfiles Collection
{
  _id: ObjectId (Primary Key)
  
  // Candidate Information
  candidate_profile: {
    first_name: String,
    last_name: String,
    email: String,
    phone: String,
    location: String,
    city: String,
    state: String,
    country: String,
    linkedin_url: String,
    github_url: String,
    portfolio_url: String
  }
  
  // Career Details
  career_classification: {
    current_title: String,
    years_of_experience: Number,
    career_level: String,
    industry: String,
    specialization: String
  }
  
  career_progression: {
    progression_summary: String,
    career_trajectory: [String],
    growth_areas: [String]
  }
  
  // Skills
  core_skills: {
    primary_skills: [String],
    secondary_skills: [String],
    soft_skills: [String],
    languages: [String],
    skills_matrix: {
      technical_proficiency: [{skill: String, level: String, years: Number}],
      domain_expertise: [{skill: String, level: String, years: Number}]
    }
  }
  
  // Experience, Education, Certifications
  work_experience: [{
    company: String,
    job_title: String,
    employment_type: String,
    location: String,
    start_date: String,
    end_date: String,
    is_current: Boolean,
    description: String,
    achievements: [String],
    technologies: [String]
  }]
  
  education: [{
    institution: String,
    degree: String,
    field_of_study: String,
    start_date: String,
    end_date: String,
    grade: String,
    activities: [String],
    description: String
  }]
  
  certifications: [{
    name: String,
    issuer: String,
    issue_date: String,
    expiry_date: String,
    credential_id: String,
    credential_url: String
  }]
  
  // Highlights
  leadership_highlights: [String],
  technical_highlights: [String],
  industry_highlights: [String],
  
  // Resume Content
  resume_blocks: {
    professional_summary: String,
    headline: String,
    key_achievements: [String]
  }
  
  // ATS Analysis
  ats_analysis: {
    ats_score: Number,
    keyword_matches: Number,
    missing_keywords: [String],
    formatting_issues: [String]
  }
  
  // Metadata
  created_at: ISODate,
  updated_at: ISODate
}
```

## Dependency Injection Container

```
Services Registration (Program.cs)
├─ Application Services
│  └─ IResumeMapper → ResumeMapper (Scoped)
│
├─ Infrastructure Services
│  ├─ IMongoDatabase (MongoDB)
│  ├─ IResumeRepository → ResumeRepository (Scoped)
│  ├─ IRazorLightEngine (RazorLight)
│  ├─ ITemplateRenderer → RazorTemplateRenderer (Scoped)
│  └─ ITemplateProvider → TemplateProvider (Scoped)
│
├─ Logging
│  └─ ILogger<T> (Built-in)
│
├─ Health Checks
│  └─ MongoDB health check
│
├─ Swagger
│  └─ Swagger/OpenAPI documentation
│
└─ CORS
   └─ AllowAngularApp policy
```

## Error Handling

```
Global Exception Middleware
├─ InvalidOperationException
│  └─ HTTP 400 Bad Request
│      "Invalid operation: {message}"
│
├─ ArgumentException
│  └─ HTTP 400 Bad Request
│      "Invalid argument: {message}"
│
└─ All Other Exceptions
   └─ HTTP 500 Internal Server Error
      "An unexpected error occurred."
```

## Logging Strategy

```
Application-wide Logging
├─ Entry Points (Controllers)
│  ├─ INFO: Start of operation (method, parameters)
│  └─ INFO/ERROR: Completion status
│
├─ Business Logic (Handlers)
│  ├─ INFO: Operation start
│  └─ ERROR: Failures with context
│
├─ Data Access (Repository)
│  ├─ INFO: DB query execution
│  ├─ DEBUG: Query details
│  └─ ERROR: DB failures
│
└─ Template Rendering
   ├─ INFO: Template load/render start
   ├─ DEBUG: Template path verification
   └─ ERROR: Rendering failures
```

## Deployment Architecture

### Development (localhost)
```
┌────────────────────────────────────┐
│ Visual Studio Code / Visual Studio │
├────────────────────────────────────┤
│ dotnet run (localhost:5000)         │
└────────────────────────────────────┘
         ▲
         │
┌────────┴────────────────────────────┐
│ MongoDB (local or docker)            │
└─────────────────────────────────────┘
```

### Docker Compose (Development)
```
┌─────────────────────────────────────┐
│ docker-compose up                   │
├─────────────────────────────────────┤
│ resume-template-api:latest (8080)   │
└────────────┬────────────────────────┘
             │
             ├─► resume-mongodb:27017
             └─► templates/ (volume mounted)
```

### Production (Kubernetes)
```
┌──────────────────────────────────────┐
│ Kubernetes Cluster                   │
├──────────────────────────────────────┤
│ ┌─ Service (LoadBalancer)            │
│ │  └─ Pod: resume-template-api       │
│ │     └─ Container: API (.NET 9)     │
│ │        └─ Volume: templates/       │
│ │                                    │
│ └─ MongoDB StatefulSet or Atlas      │
│    └─ Persistent Volume: data/db    │
└──────────────────────────────────────┘
```

## Scalability Considerations

1. **API Scaling**: Stateless design allows horizontal scaling
2. **Template Caching**: RazorLight caches compiled templates
3. **Connection Pooling**: MongoDB driver connection pooling
4. **Load Balancing**: Use reverse proxy (Nginx, HAProxy)
5. **Database**: MongoDB horizontal scaling via sharding
6. **Container Orchestration**: Kubernetes for production

## Performance Optimization

1. **Async I/O**: All database and file operations are async
2. **Template Pre-compilation**: Cached on first use
3. **Database Indexing**: _id index on ResumeProfiles
4. **Connection Pooling**: Managed by MongoDB driver
5. **Minimal Dependencies**: Lightweight NuGet packages
6. **Structured Logging**: Negligible performance impact

## Security Best Practices

1. ✅ Input Validation: All API inputs validated
2. ✅ CORS: Configured for specific origins
3. ✅ Error Handling: No sensitive info in error responses
4. ✅ Docker Security: Non-root user, minimal image
5. ✅ Logging: No sensitive data in logs
6. ⚠️  TODO: Add API rate limiting
7. ⚠️  TODO: Add authentication/authorization (JWT)
8. ⚠️  TODO: Add HTTPS/TLS configuration
