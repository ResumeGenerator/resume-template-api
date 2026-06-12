# Quick Start Guide

## 30-Second Setup

### Option 1: Docker Compose (Easiest)
```bash
docker-compose up -d
# Wait 30 seconds for services to start
# API: http://localhost:8080
# Swagger: http://localhost:8080
```

### Option 2: Local Development
```bash
# Terminal 1: MongoDB
docker run -p 27017:27017 mongo:7.0

# Terminal 2: API
cd src/ResumeTemplateService.Api
dotnet run

# Open browser: http://localhost:5000
```

## Test the API

### 1. Get Available Templates
```bash
curl http://localhost:8080/api/templates
```

**Expected Response:**
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

### 2. Insert Sample Resume in MongoDB

Open MongoDB shell:
```bash
docker exec -it resume-mongodb mongosh
```

Run in MongoDB shell:
```javascript
use ResumeDb

db.ResumeProfiles.insertOne({
  candidate_profile: {
    first_name: "John",
    last_name: "Doe",
    email: "john@example.com",
    phone: "+1-555-0123",
    location: "San Francisco, CA",
    city: "San Francisco",
    state: "CA",
    country: "USA"
  },
  career_classification: {
    current_title: "Senior Software Engineer",
    years_of_experience: 8,
    career_level: "Senior",
    industry: "Technology",
    specialization: "Full Stack Development"
  },
  career_progression: {
    progression_summary: "Progressed from Junior Developer to Senior Engineer",
    career_trajectory: ["Junior Developer", "Senior Developer", "Senior Engineer"],
    growth_areas: ["Leadership", "System Design"]
  },
  core_skills: {
    primary_skills: [".NET", "C#", "SQL", "Azure", "Docker"],
    secondary_skills: ["JavaScript", "React", "MongoDB"],
    soft_skills: ["Leadership", "Communication"],
    languages: ["English"],
    skills_matrix: {
      technical_proficiency: [
        {skill: ".NET Core", level: "Expert", years: 6},
        {skill: "Azure", level: "Advanced", years: 5}
      ],
      domain_expertise: [
        {skill: "Microservices", level: "Expert", years: 5}
      ]
    }
  },
  work_experience: [
    {
      company: "Tech Corp",
      job_title: "Senior Software Engineer",
      location: "San Francisco, CA",
      start_date: "2021-01",
      end_date: "",
      is_current: true,
      description: "Leading development of cloud-based systems",
      achievements: [
        "Architected microservices platform",
        "Mentored 5 junior developers"
      ],
      technologies: [".NET", "Azure", "Docker"]
    }
  ],
  education: [
    {
      institution: "State University",
      degree: "Bachelor of Science",
      field_of_study: "Computer Science",
      start_date: "2014-09",
      end_date: "2018-05",
      grade: "3.8 GPA",
      description: "Focused on software engineering"
    }
  ],
  certifications: [
    {
      name: "Azure Solutions Architect Expert",
      issuer: "Microsoft",
      issue_date: "2022-06",
      expiry_date: "2025-06"
    }
  ],
  leadership_highlights: ["Led team of 8 engineers", "Mentored 5 developers"],
  technical_highlights: ["Designed microservices platform", "Optimized queries 70%"],
  industry_highlights: ["Tech conference speaker", "Open source contributor"],
  resume_blocks: {
    professional_summary: "Senior Software Engineer with 8 years of experience",
    headline: "Senior Software Engineer | Cloud Architecture",
    key_achievements: [
      "Led microservices platform development",
      "Reduced latency by 60%"
    ]
  },
  ats_analysis: {
    ats_score: 87.5,
    keyword_matches: 45,
    missing_keywords: [],
    formatting_issues: []
  },
  created_at: new Date(),
  updated_at: new Date()
})
```

Copy the `_id` from the inserted document.

### 3. Render Resume Preview

```bash
curl -X POST http://localhost:8080/api/resumes/preview \
  -H "Content-Type: application/json" \
  -d '{
    "resumeId": "65a1b2c3d4e5f6g7h8i9j0k1",
    "templateId": "professional-dark-blue"
  }'
```

**Expected Response:**
```json
{
  "templateId": "professional-dark-blue",
  "html": "<html>...</html>"
}
```

### 4. Check Health

```bash
curl http://localhost:8080/health
```

**Expected Response:**
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

## Integration with Angular

```typescript
// service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ResumeService {
  private apiUrl = 'http://localhost:8080/api';

  constructor(private http: HttpClient) {}

  renderResume(resumeId: string, templateId: string) {
    return this.http.post<any>(`${this.apiUrl}/resumes/preview`, {
      resumeId,
      templateId
    });
  }

  getTemplates() {
    return this.http.get<any[]>(`${this.apiUrl}/templates`);
  }
}

// component.ts
import { Component, OnInit } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-resume-preview',
  template: `
    <div class="resume-container">
      <select [(ngModel)]="selectedTemplate" (change)="render()">
        <option *ngFor="let t of templates" [value]="t.id">
          {{ t.name }}
        </option>
      </select>
      <iframe [srcdoc]="resumeHtml"></iframe>
    </div>
  `
})
export class ResumePreviewComponent implements OnInit {
  resumeHtml: string = '';
  selectedTemplate = 'professional-dark-blue';
  templates: any[] = [];

  constructor(
    private resumeService: ResumeService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit() {
    const resumeId = this.route.snapshot.params['id'];
    
    this.resumeService.getTemplates().subscribe(templates => {
      this.templates = templates;
    });

    this.render();
  }

  render() {
    const resumeId = this.route.snapshot.params['id'];
    this.resumeService.renderResume(resumeId, this.selectedTemplate)
      .subscribe(response => {
        this.resumeHtml = response.html;
      });
  }
}
```

## Common Issues

### MongoDB Connection Error
```bash
# Check if MongoDB is running
docker ps | grep mongodb

# Start MongoDB
docker run -d -p 27017:27017 --name mongodb mongo:7.0
```

### Port Already in Use
```bash
# Change port in docker-compose.yml or
# Kill process using port:
# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# Mac/Linux
lsof -ti:8080 | xargs kill -9
```

### Template Not Found
- Verify template ID: `professional-dark-blue` or `modern-minimal`
- Check templates directory exists

## Cleanup

```bash
# Stop services
docker-compose down

# Remove volumes (data)
docker-compose down -v

# Stop MongoDB container
docker stop resume-mongodb
docker rm resume-mongodb
```

## Documentation

- 📖 [README.md](README.md) - Full documentation
- 🏗️ [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- 🛠️ [DEVELOPMENT.md](DEVELOPMENT.md) - Development guide
- 🚀 Swagger UI: http://localhost:8080

## Next Steps

1. ✅ Test API endpoints
2. ✅ Insert sample resume data
3. ✅ Render resume preview
4. ⬜ Integrate with Angular frontend
5. ⬜ Deploy to production
6. ⬜ Add more templates
7. ⬜ Add caching layer (Redis)
8. ⬜ Add authentication (JWT)

Happy coding! 🚀
