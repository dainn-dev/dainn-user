# PSA-87: API Documentation Design

**Date:** 2026-05-13  
**Status:** Approved  
**Linear Issue:** [PSA-87](https://linear.app/psa-app/issue/PSA-87/write-api-documentation)

## Overview

Implement comprehensive API documentation for DainnUser .NET library using DocFX to generate a static documentation site with API reference, conceptual guides, configuration reference, troubleshooting, FAQ, and code examples.

## Goals

1. **Complete documentation coverage** — all public APIs, configuration options, and features documented
2. **Developer-friendly** — easy to navigate, searchable, with practical examples
3. **Maintainable** — auto-generated API reference from XML docs, single source of truth
4. **Publishable** — static site deployable to GitHub Pages or docs hosting
5. **Standards-compliant** — OpenAPI/Swagger spec for API consumers

## Current State

**Existing documentation:**
- `docs/README.md` — basic overview, installation, quick start
- `docs/getting-started.md` — setup guide with examples
- `docs/api-endpoints.md` — partial API docs (Auth endpoints only)
- `docs/architecture.md` — system architecture
- `docs/security.md` — security guide
- `docs/migrations.md` — database migrations guide
- `docs/known-issues.md` — known issues tracker

**Gaps:**
- No DocFX site structure
- Missing configuration reference (scattered across files)
- Missing troubleshooting guide
- Missing FAQ
- Missing comprehensive code examples
- API reference incomplete (only Auth controller documented)
- No OpenAPI spec export
- XML documentation exists but not published

**XML docs status:**
- ✅ All 5 projects have `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- ✅ All projects suppress warning 1591 (missing XML comments)
- ⚠️ XML docs coverage unknown — needs audit

**Swagger status:**
- ✅ Swagger enabled in `src/DainnUser.Api/Program.cs`
- ✅ Swagger UI available at `/swagger` endpoint
- ⚠️ No OpenAPI spec export configured

## Proposed Solution

### Documentation Site Structure

```
docs/
├── docfx.json                    # DocFX configuration
├── index.md                      # Landing page (overview + quick links)
├── toc.yml                       # Top-level table of contents
├── articles/                     # Conceptual documentation
│   ├── toc.yml                   # Articles TOC
│   ├── getting-started.md        # Moved from docs/ root
│   ├── configuration.md          # NEW — complete config reference
│   ├── architecture.md           # Moved from docs/ root
│   ├── api-endpoints.md          # Expanded with all controllers
│   ├── security.md               # Moved from docs/ root
│   ├── migrations.md             # Moved from docs/ root
│   ├── troubleshooting.md        # NEW — from known-issues + common problems
│   ├── faq.md                    # NEW — frequently asked questions
│   └── code-examples.md          # NEW — cookbook with common scenarios
├── api/                          # Auto-generated API reference
│   ├── index.md                  # API reference landing page
│   └── toc.yml                   # Auto-generated from XML docs
├── openapi/                      # OpenAPI specification
│   └── openapi.yaml              # Exported from Swagger
├── templates/                    # DocFX template customization
│   └── dainnuser/                # Custom template (optional)
├── _site/                        # Generated site output (gitignored)
└── README.md                     # Stays as repo/NuGet entry point
```

### DocFX Configuration

**`docfx.json`:**
```json
{
  "metadata": [
    {
      "src": [
        {
          "files": ["**/*.csproj"],
          "src": "../src"
        }
      ],
      "dest": "api",
      "includePrivateMembers": false,
      "disableGitFeatures": false,
      "disableDefaultFilter": false,
      "properties": {
        "TargetFramework": "net8.0"
      }
    }
  ],
  "build": {
    "content": [
      {
        "files": ["api/**.yml", "api/index.md"]
      },
      {
        "files": ["articles/**.md", "articles/**/toc.yml", "toc.yml", "*.md"]
      }
    ],
    "resource": [
      {
        "files": ["images/**"]
      }
    ],
    "overwrite": [
      {
        "files": ["apidoc/**.md"],
        "exclude": ["obj/**", "_site/**"]
      }
    ],
    "dest": "_site",
    "globalMetadataFiles": [],
    "fileMetadataFiles": [],
    "template": ["default", "modern"],
    "postProcessors": [],
    "markdownEngineName": "markdig",
    "noLangKeyword": false,
    "keepFileLink": false,
    "cleanupCacheHistory": false,
    "disableGitFeatures": false,
    "globalMetadata": {
      "_appTitle": "DainnUser Documentation",
      "_appFooter": "© 2026 DainnUser. Licensed under MIT.",
      "_enableSearch": true,
      "_enableNewTab": true
    }
  }
}
```

### Content Plan

#### 1. `index.md` (Landing Page)

**Content:**
- Hero section with library description
- Key features list
- Quick start code snippet
- Navigation to main sections (Getting Started, API Reference, Configuration, Examples)
- Links to GitHub, NuGet packages

**Source:** New, based on `docs/README.md`

#### 2. `articles/getting-started.md`

**Content:**
- Installation via NuGet
- Basic setup (Program.cs, appsettings.json)
- First API call (register + login)
- Next steps links

**Source:** Move from `docs/getting-started.md`, review for completeness

#### 3. `articles/configuration.md` (NEW)

**Content:**
- Complete configuration reference organized by section:
  - Database configuration (all 4 providers)
  - JWT settings
  - Email providers (SMTP, SendGrid, AWS SES)
  - SMS providers (Twilio, AWS SNS)
  - Storage providers (Local, Azure Blob, AWS S3)
  - OAuth providers (Google, Facebook, GitHub, Microsoft)
  - reCAPTCHA settings
  - Security settings (password requirements, lockout, rate limiting)
  - Session settings
  - Feature flags (enable/disable features)
- Each section with:
  - JSON schema
  - Default values
  - Required vs optional
  - Example configuration
  - Environment variable alternatives

**Source:** Extract from `getting-started.md`, `security.md`, `architecture.md`, code

#### 4. `articles/api-endpoints.md`

**Content:**
- Expand current Auth endpoints documentation
- Add Profile endpoints (GET, PUT profile, avatar upload/delete)
- Add User management endpoints (admin CRUD, role assignment, lock/unlock)
- Add Session endpoints (list, revoke)
- For each endpoint:
  - HTTP method + path
  - Request body schema
  - Response schema
  - Status codes
  - Authorization requirements
  - Example cURL command
  - Example C# code

**Source:** Expand `docs/api-endpoints.md`, read controllers

#### 5. `articles/troubleshooting.md` (NEW)

**Content:**
- Common issues organized by category:
  - Installation & Setup
  - Database & Migrations
  - Authentication & JWT
  - Email sending
  - OAuth configuration
  - Performance
- Each issue with:
  - Symptom description
  - Root cause
  - Solution steps
  - Prevention tips
- Include known issue from `docs/known-issues.md`

**Source:** `docs/known-issues.md` + new content based on common patterns

#### 6. `articles/faq.md` (NEW)

**Content:**
- 15-20 frequently asked questions organized by category:
  - General (What is DainnUser? License? Support?)
  - Installation (NuGet packages? Dependencies?)
  - Configuration (Which database? How to customize?)
  - Features (2FA? Social login? RBAC?)
  - Security (OWASP compliance? Password hashing?)
  - Deployment (Production checklist? Docker?)
  - Customization (Extend entities? Override services?)
  - Performance (Scalability? Caching?)
- Each answer with links to relevant docs

**Source:** New, based on project patterns and developer needs

#### 7. `articles/code-examples.md` (NEW)

**Content:**
- Cookbook with complete, runnable examples for common scenarios:
  1. Register user + verify email
  2. Login with password
  3. Login with 2FA
  4. Social login (Google OAuth flow)
  5. Password reset flow
  6. Change password
  7. Enable 2FA
  8. Manage user sessions
  9. Upload avatar
  10. Admin: create user with role
  11. Admin: lock/unlock account
  12. Custom validation rules
  13. Extend user entity
  14. Override email service
- Each example with:
  - Scenario description
  - Complete C# code
  - Configuration needed
  - Expected output
  - Common pitfalls

**Source:** New, based on sample apps and common use cases

#### 8. `articles/architecture.md`

**Content:** Move from `docs/architecture.md`, no changes needed

#### 9. `articles/security.md`

**Content:** Move from `docs/security.md`, no changes needed

#### 10. `articles/migrations.md`

**Content:** Move from `docs/migrations.md`, no changes needed

#### 11. `api/index.md` (API Reference Landing)

**Content:**
- Introduction to API reference
- How to read API docs
- Links to main namespaces:
  - DainnUser.Core (entities, interfaces, enums)
  - DainnUser.Application (services, DTOs, validators)
  - DainnUser.Infrastructure (DbContext, repositories, external services)
  - DainnUser.Api (controllers, DTOs, filters)
  - DainnUser.Web (components, pages, tag helpers)

**Source:** New

#### 12. `openapi/openapi.yaml`

**Content:**
- OpenAPI 3.0 specification exported from Swagger
- Includes all API endpoints with schemas

**Source:** Export from Swagger endpoint

### XML Documentation Audit & Completion

**Scope:**
- Audit all public APIs across 5 projects
- Add missing XML doc comments for:
  - Public classes
  - Public methods
  - Public properties
  - Public interfaces
  - Enums
- Follow .NET XML documentation standards:
  - `<summary>` for all members
  - `<param>` for method parameters
  - `<returns>` for return values
  - `<exception>` for thrown exceptions
  - `<example>` for complex APIs
  - `<remarks>` for additional context

**Priority:**
1. Core interfaces and services (high visibility)
2. DTOs and models (used by consumers)
3. Controllers (API surface)
4. Infrastructure implementations (lower priority)

### OpenAPI/Swagger Enhancement

**Changes to `src/DainnUser.Api/Program.cs`:**

1. **Add XML comments to Swagger:**
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DainnUser API",
        Version = "v1",
        Description = "User management, authentication, and authorization API",
        Contact = new OpenApiContact
        {
            Name = "DainnUser",
            Url = new Uri("https://github.com/yourorg/dainnuser")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    
    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

2. **Add OpenAPI export endpoint:**
```csharp
app.MapGet("/openapi/v1.yaml", async (IServiceProvider sp) =>
{
    var swagger = sp.GetRequiredService<ISwaggerProvider>();
    var doc = swagger.GetSwagger("v1");
    var yaml = doc.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0);
    return Results.Text(yaml, "application/yaml");
});
```

### Build & Deployment

**Build script (`docs/build.sh` or `docs/build.ps1`):**
```bash
#!/bin/bash
# Build documentation

# Step 1: Build projects to generate XML docs
dotnet build ../DainnUser.sln -c Release

# Step 2: Install DocFX (if not installed)
dotnet tool install -g docfx

# Step 3: Build DocFX site
docfx build docfx.json

# Step 4: Export OpenAPI spec (requires API running)
# curl http://localhost:5000/openapi/v1.yaml -o openapi/openapi.yaml

echo "Documentation built successfully in _site/"
```

**GitHub Actions workflow (`.github/workflows/docs.yml`):**
```yaml
name: Build and Deploy Docs

on:
  push:
    branches: [main, master]
    paths:
      - 'docs/**'
      - 'src/**/*.cs'
      - '.github/workflows/docs.yml'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build projects (generate XML docs)
        run: dotnet build DainnUser.sln -c Release
      
      - name: Install DocFX
        run: dotnet tool install -g docfx
      
      - name: Build documentation
        run: docfx build docs/docfx.json
      
      - name: Deploy to GitHub Pages
        uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./docs/_site
```

### `.gitignore` Updates

Add to `.gitignore`:
```
# DocFX
docs/_site/
docs/api/*.yml
docs/obj/
```

## Implementation Phases

### Phase 1: DocFX Setup
1. Install DocFX tooling
2. Create `docfx.json` configuration
3. Create `docs/index.md` landing page
4. Create `docs/toc.yml` top-level TOC
5. Create `docs/articles/toc.yml` articles TOC
6. Test DocFX build with existing docs

### Phase 2: Content Migration & Organization
1. Move existing docs to `articles/` folder
2. Update internal links
3. Create `api/index.md`
4. Test site navigation

### Phase 3: New Content Creation
1. Write `articles/configuration.md`
2. Write `articles/troubleshooting.md`
3. Write `articles/faq.md`
4. Write `articles/code-examples.md`
5. Expand `articles/api-endpoints.md`

### Phase 4: XML Documentation Audit
1. Audit Core project
2. Audit Application project
3. Audit Infrastructure project
4. Audit Api project
5. Audit Web project
6. Add missing XML comments

### Phase 5: Swagger/OpenAPI Enhancement
1. Update `Program.cs` with XML comments integration
2. Add JWT security scheme to Swagger
3. Add OpenAPI export endpoint
4. Test Swagger UI
5. Export `openapi.yaml`

### Phase 6: Build & Deployment
1. Create build script
2. Test local build
3. Create GitHub Actions workflow
4. Configure GitHub Pages
5. Deploy and verify

## Success Criteria

- [ ] DocFX site builds without errors
- [ ] All existing docs migrated to `articles/`
- [ ] Configuration reference complete with all sections
- [ ] Troubleshooting guide covers 10+ common issues
- [ ] FAQ has 15+ questions with answers
- [ ] Code examples cover 10+ scenarios
- [ ] API endpoints documentation covers all controllers
- [ ] XML documentation coverage > 90% for public APIs
- [ ] Swagger UI includes XML comments
- [ ] OpenAPI spec exported and accessible
- [ ] Site deployed to GitHub Pages
- [ ] All internal links working
- [ ] Search functionality working
- [ ] Mobile-responsive design

## Non-Goals

- Custom DocFX theme (use default/modern template)
- Video tutorials
- Interactive API playground (Swagger UI sufficient)
- Multi-language support (English only)
- Versioned documentation (single version for now)

## Dependencies

- DocFX tool (install via `dotnet tool install -g docfx`)
- .NET 8 SDK (already required)
- GitHub Pages (for deployment)

## Risks & Mitigations

**Risk:** XML documentation audit takes longer than expected  
**Mitigation:** Prioritize high-visibility APIs first, defer infrastructure implementations

**Risk:** DocFX build fails due to XML doc errors  
**Mitigation:** Fix errors incrementally, use `<NoWarn>1591</NoWarn>` for incomplete sections temporarily

**Risk:** OpenAPI export requires API running  
**Mitigation:** Document manual export process, automate in CI/CD later

## References

- [DocFX Documentation](https://dotnet.github.io/docfx/)
- [.NET XML Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)
- [OpenAPI Specification](https://swagger.io/specification/)
- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
