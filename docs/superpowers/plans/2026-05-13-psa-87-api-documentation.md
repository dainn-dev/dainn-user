# PSA-87: API Documentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build comprehensive DocFX documentation site with API reference from XML docs, expanded articles, OpenAPI spec, and CI/CD deployment.

**Architecture:** DocFX generates static site from Markdown articles + auto-generated API YML. Existing docs move into `docs/articles/`. New content added for configuration, troubleshooting, FAQ, code examples. Swagger enhanced with XML comments and JWT security scheme.

**Tech Stack:** DocFX, Markdig, .NET 8 XML docs, Swashbuckle, GitHub Pages, GitHub Actions

---

## Phase 1: DocFX Scaffolding

### Task 1.1: Install DocFX tool
- [ ] Run `dotnet tool install -g docfx` (or `dotnet tool update -g docfx`)
- [ ] Verify: `docfx --version`

### Task 1.2: Create docfx.json
- [ ] File: `docs/docfx.json`
- [ ] Content from spec section "DocFX Configuration"
- [ ] Verify: `docfx build docs/docfx.json` parses config (may fail on missing content — that's OK)

### Task 1.3: Create index.md landing page
- [ ] File: `docs/index.md`
- [ ] Hero section, key features list, quick start snippet, navigation links
- [ ] Source from `docs/README.md` content

### Task 1.4: Create TOC files
- [ ] File: `docs/toc.yml` — top-level TOC linking to articles, API reference, OpenAPI
- [ ] File: `docs/articles/toc.yml` — articles TOC with all article pages
- [ ] File: `docs/api/index.md` — API reference landing with namespace links
- [ ] File: `docs/api/toc.yml` — placeholder for auto-generated TOC

### Task 1.5: Update .gitignore
- [ ] File: `.gitignore`
- [ ] Add:
```
# DocFX
docs/_site/
docs/api/*.yml
docs/obj/
```

---

## Phase 2: Content Migration

### Task 2.1: Move existing docs to articles/
- [ ] Move `docs/getting-started.md` → `docs/articles/getting-started.md`
- [ ] Move `docs/architecture.md` → `docs/articles/architecture.md`
- [ ] Move `docs/security.md` → `docs/articles/security.md`
- [ ] Move `docs/migrations.md` → `docs/articles/migrations.md`
- [ ] Update internal links in all moved files

### Task 2.2: Expand api-endpoints.md
- [ ] File: `docs/articles/api-endpoints.md`
- [ ] Add Profile endpoints section (GET/PUT profile, avatar upload/delete, settings)
- [ ] Add User management endpoints section (admin CRUD, lock/unlock, role assignment)
- [ ] Add Session endpoints section (list, revoke) — check SessionController if exists
- [ ] Each endpoint: HTTP method + path, request/response schema, status codes, auth, cURL example, C# example

---

## Phase 3: New Content

### Task 3.1: Write configuration.md
- [ ] File: `docs/articles/configuration.md`
- [ ] Complete config reference by section: Database, JWT, Email, SMS, Storage, OAuth, reCAPTCHA, Security, Session, Feature flags
- [ ] Each section: JSON schema, defaults, required vs optional, example, env var alternatives
- [ ] Extract from existing docs + `appsettings.json` + code

### Task 3.2: Write troubleshooting.md
- [ ] File: `docs/articles/troubleshooting.md`
- [ ] 10+ common issues by category: Install/Setup, DB/Migrations, Auth/JWT, Email, OAuth, Performance
- [ ] Each: symptom, root cause, solution, prevention
- [ ] Incorporate `docs/known-issues.md` content

### Task 3.3: Write faq.md
- [ ] File: `docs/articles/faq.md`
- [ ] 15-20 questions by category: General, Installation, Configuration, Features, Security, Deployment, Customization, Performance
- [ ] Each answer with links to relevant docs

### Task 3.4: Write code-examples.md
- [ ] File: `docs/articles/code-examples.md`
- [ ] 10+ cookbook scenarios with complete C# code
- [ ] Scenarios: register+verify, login, social login, password reset, 2FA setup, session management, avatar upload, admin create user, admin lock/unlock, custom validation

---

## Phase 4: Swagger/OpenAPI Enhancement

### Task 4.1: Update Program.cs Swagger configuration
- [ ] File: `src/DainnUser.Api/Program.cs`
- [ ] Add XML comments include to Swagger:
```csharp
var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
options.IncludeXmlComments(xmlPath);
```
- [ ] Add JWT Bearer security definition and requirement
- [ ] Add Contact and License info to OpenApiInfo
- [ ] Add required using: `using System.Reflection;`, `using Microsoft.OpenApi.Models;`

### Task 4.2: Add OpenAPI YAML export endpoint
- [ ] File: `src/DainnUser.Api/Program.cs`
- [ ] Add `app.MapGet("/openapi/v1.yaml", ...)` endpoint using `ISwaggerProvider`
- [ ] Requires `Microsoft.OpenApi.Readers` for YAML serialization (or use `Swashbuckle.AspNetCore` built-in)

---

## Phase 5: Build & Deploy

### Task 5.1: Create build script
- [ ] File: `docs/build.ps1` (PowerShell for Windows)
- [ ] Steps: `dotnet build`, `docfx build`, export OpenAPI

### Task 5.2: Create GitHub Actions workflow
- [ ] File: `.github/workflows/docs.yml`
- [ ] Content from spec section "GitHub Actions workflow"
- [ ] Trigger on push to main for docs/src changes
- [ ] Deploy to GitHub Pages via `peaceiris/actions-gh-pages`

---

## Phase 6: Verification

### Task 6.1: Verify DocFX build
- [ ] Run `dotnet build e:\Projects\DainnUser\DainnUser.sln -c Release`
- [ ] Run `docfx build docs/docfx.json`
- [ ] Fix any build errors
- [ ] Check `docs/_site/` for generated output

### Task 6.2: Verify Swagger
- [ ] Run `dotnet build` on API project
- [ ] Confirm Swagger UI loads with XML comments and JWT auth button
- [ ] Confirm `/openapi/v1.yaml` returns valid YAML

### Task 6.3: Self-Review
- [ ] Run self-review checklist from CLAUDE.md
- [ ] All tests pass: `dotnet test`

---

## Commit Checkpoints

| # | After | Message |
|---|-------|---------|
| 1 | Phase 1 | `docs: add DocFX scaffolding and landing page` |
| 2 | Phase 2 | `docs: migrate existing docs to articles structure` |
| 3 | Phase 2.2 | `docs: expand API endpoints documentation` |
| 4 | Phase 3 | `docs: add configuration, troubleshooting, faq, code-examples` |
| 5 | Phase 4 | `feat: enhance Swagger with XML docs and OpenAPI export` |
| 6 | Phase 5 | `ci: add docs build and deploy workflow` |
| 7 | Phase 6 | `chore: fix docfx build warnings and verify output` |
