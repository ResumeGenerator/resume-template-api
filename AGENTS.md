# AGENTS.md

Guidance for AI coding agents working on the Resume Template Service.

## Repository Scope

Run commands from this directory, which contains `ResumeTemplateService.sln`.

```powershell
dotnet restore
dotnet build ResumeTemplateService.sln
```

There are currently no test projects in the solution. If adding behavior with meaningful risk, add focused tests when practical and always run a build.

## Service Summary

This is an ASP.NET Core 9 Web API that renders resume data from MongoDB into HTML, PDF, and Word outputs.

Core flow:

1. API receives a render request with `resumeId` and template id(s).
2. `ResumeRepository` loads a resume document from MongoDB.
3. `ResumeMapper` maps the domain model to `ResumeViewModel`.
4. Razor templates under `templates/<template-id>/template.cshtml` render HTML via RazorLight.
5. `ChromiumPdfRenderer` generates PDF bytes from HTML.
6. `DocxWordRenderer` generates DOCX bytes.

Important endpoints:

- `GET /`
- `GET /live`
- `GET /health`
- `GET /api/templates`
- `POST /api/resumes/preview`
- `POST /api/resumes/pdf`
- `POST /api/resumes/word`

## Project Layout

- `src/ResumeTemplateService.Api/`: API startup, controllers, DTOs, middleware, Swagger, CORS, health checks, and DI registration.
- `src/ResumeTemplateService.Application/`: use-case orchestration, interfaces, and `ResumeMapper`.
- `src/ResumeTemplateService.Domain/`: MongoDB-backed domain entities and template-facing value objects.
- `src/ResumeTemplateService.Infrastructure/`: MongoDB repository, RazorLight renderer, template discovery, PDF renderer, and Word renderer.
- `templates/`: filesystem-discovered Razor templates. Each template directory must contain `template.cshtml`; `style.css` is optional.

## Architecture Rules

- Keep controllers thin: validate HTTP requests, call application/infrastructure abstractions, and shape responses.
- Do not put MongoDB driver calls in controllers or application handlers.
- Do not put RazorLight, Chromium, DOCX, or filesystem template discovery in Domain or Application.
- Extend existing interfaces and implementations before adding parallel service paths.
- Keep changes scoped and avoid unrelated refactors.

## Configuration Notes

Configuration is resolved in `src/ResumeTemplateService.Api/Program.cs`.

MongoDB aliases include:

- `MongoDB:ConnectionString`, connection string `MongoDB`, `MONGO_URL`, `MONGODB_URL`, `MONGODB_URI`, `DATABASE_URL`
- `MongoDB:DatabaseName`, `MONGODB_DATABASE`, `MONGO_DATABASE`, `MONGO_INITDB_DATABASE`
- `MongoDB:CollectionName`, `MONGODB_COLLECTION`, `MONGO_COLLECTION`
- `MongoDB:EditedCollectionName`, `MONGODB_EDITED_COLLECTION`, `MONGO_EDITED_COLLECTION`

Other important config:

- `Templates:BasePath`
- `Pdf:ChromiumExecutablePath`
- `Cors:AllowedOrigins`
- `PORT` for Railway-style hosting

## Data Model Notes

`ResumeRepository` supports both the direct service schema and a parser schema with nested `profile` data. When changing resume fields, check:

- `src/ResumeTemplateService.Domain/Entities/ResumeProfile.cs`
- `src/ResumeTemplateService.Infrastructure/Repositories/ResumeRepository.cs`
- `src/ResumeTemplateService.Application/Mappings/ResumeMapper.cs`
- `src/ResumeTemplateService.Domain/ValueObjects/ResumeViewModel.cs`

If `EditedCollectionName` is configured, the repository prefers the newest edited copy by `originalResumeId`, sorted by `updatedAt` and `createdAt`.

## Template Guidance

To add a template:

1. Create `templates/<template-id>/template.cshtml`.
2. Add `templates/<template-id>/style.css` if needed.
3. Use `@model ResumeTemplateService.Domain.ValueObjects.ResumeViewModel`.
4. Keep template ids URL-safe and lowercase with hyphens.
5. Update `TemplateProvider.GetTemplateDescription` if a custom description is useful.

Render sections conditionally when content is absent. Avoid empty headings and large blank gaps, especially because HTML may be converted to PDF.

## API Contract Notes

Preview accepts either one `templateId` or many `templateIds`:

```json
{
  "resumeId": "507f1f77bcf86cd799439011",
  "templateId": "professional-dark-blue",
  "templateIds": ["professional-dark-blue", "modern-minimal"]
}
```

Preview returns:

```json
{
  "resumeId": "507f1f77bcf86cd799439011",
  "templates": [
    {
      "templateId": "professional-dark-blue",
      "html": "<html>...</html>"
    }
  ]
}
```

PDF and Word requests accept a single `templateId`.

## Development Commands

```powershell
dotnet restore
dotnet build ResumeTemplateService.sln
dotnet format ResumeTemplateService.sln
dotnet test
```

Run locally:

```powershell
cd src/ResumeTemplateService.Api
dotnet run
```

Docker:

```powershell
docker compose up -d --build
docker compose down
```

Manual smoke checks:

```powershell
Invoke-RestMethod http://localhost:5000/
Invoke-RestMethod http://localhost:5000/live
Invoke-RestMethod http://localhost:5000/health
Invoke-RestMethod http://localhost:5000/api/templates
```

## Verification Expectations

For any C# code change, run:

```powershell
dotnet build ResumeTemplateService.sln
```

For endpoint changes, describe how the endpoint was checked or why it was not checked. For MongoDB mapping changes, verify both direct `ResumeProfile` documents and nested parser `profile` documents when possible.

## Known Gaps

- No test project is currently included in `ResumeTemplateService.sln`.
- Some human-facing docs may lag behind current endpoints; source code is the final authority.
- `TemplateProvider.PreviewUrl` points to `/api/templates/{templateId}/preview`, but there is no matching controller action in the current code.
