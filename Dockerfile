# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj files
COPY src/ResumeTemplateService.Domain/*.csproj ./src/ResumeTemplateService.Domain/
COPY src/ResumeTemplateService.Application/*.csproj ./src/ResumeTemplateService.Application/
COPY src/ResumeTemplateService.Infrastructure/*.csproj ./src/ResumeTemplateService.Infrastructure/
COPY src/ResumeTemplateService.Api/*.csproj ./src/ResumeTemplateService.Api/

# Restore dependencies
RUN dotnet restore src/ResumeTemplateService.Api/ResumeTemplateService.Api.csproj

# Copy source code
COPY src/ ./src/
COPY templates/ ./templates/

# Build application
RUN dotnet build -c Release src/ResumeTemplateService.Api/ResumeTemplateService.Api.csproj

# Publish application
RUN dotnet publish -c Release -o /app/publish src/ResumeTemplateService.Api/ResumeTemplateService.Api.csproj

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install curl for health checks and Chromium for server-side PDF rendering
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl chromium fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Copy templates
COPY --from=build /app/templates ./templates

# Create non-root user
RUN useradd -m -u 1001 dotnetuser
USER dotnetuser

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start application. Railway provides PORT; local Docker runs default to 8080.
ENTRYPOINT ["sh", "-c", "exec dotnet ResumeTemplateService.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
