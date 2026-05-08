# Project Facts — DainnUser

_Stable facts. Chỉ update khi project thay đổi căn bản._

## Description

DainnUser là một .NET/C# class library cung cấp đầy đủ tính năng user management cho các ứng dụng .NET. Library được thiết kế để developers có thể dễ dàng integrate qua NuGet package và customize theo business logic của họ.

**Mục tiêu chính:**
- Cung cấp authentication & authorization features out-of-the-box
- Hỗ trợ multiple database providers (SQL Server, PostgreSQL, MySQL, SQLite)
- Flexible configuration qua appsettings.json
- Dễ dàng extend và customize
- OWASP Top 10 compliance

## Tech Stack

**Core:**
- .NET 8.0+ (hoặc LTS version)
- C# 12+
- Entity Framework Core (database abstraction)

**Authentication & Authorization:**
- ASP.NET Core Identity (foundation)
- JWT tokens cho API authentication
- OAuth 2.0 / OpenID Connect cho social login
- TOTP cho 2FA

**Security:**
- Data Protection API
- Password hashing (PBKDF2/Argon2)
- Rate limiting
- Google reCAPTCHA integration

**Testing:**
- xUnit (unit tests)
- Moq (mocking)
- FluentAssertions
- TestServer (integration tests)
- OWASP ZAP (security testing)

**Validation:**
- FluentValidation

**Logging:**
- Microsoft.Extensions.Logging
- Serilog (recommended)

## Architecture

**Clean Architecture / Onion Architecture:**

```
┌─────────────────────────────────────┐
│         Presentation Layer          │
│  (DainnUser.Api, DainnUser.Web)    │
├─────────────────────────────────────┤
│       Application Layer             │
│    (DainnUser.Application)          │
│  - Services, DTOs, Validators       │
├─────────────────────────────────────┤
│      Infrastructure Layer           │
│   (DainnUser.Infrastructure)        │
│  - EF Core, Repositories, External  │
├─────────────────────────────────────┤
│          Core/Domain Layer          │
│       (DainnUser.Core)              │
│  - Entities, Interfaces, Enums      │
└─────────────────────────────────────┘
```

**Dependency Flow:** Presentation → Application → Infrastructure → Core

**Key Patterns:**
- Repository Pattern (data access abstraction)
- Unit of Work (transaction management)
- Options Pattern (configuration)
- Dependency Injection (IoC)
- CQRS (optional, cho complex scenarios)

## Database

**ORM:** Entity Framework Core

**Supported Providers:**
- Microsoft.EntityFrameworkCore.SqlServer
- Npgsql.EntityFrameworkCore.PostgreSQL
- Pomelo.EntityFrameworkCore.MySql
- Microsoft.EntityFrameworkCore.Sqlite

**Key Entities:**
- `User` — core user entity
- `Role` — RBAC roles
- `UserRole` — user-role mapping
- `UserClaim` — additional user claims
- `UserLogin` — external login providers
- `UserToken` — refresh tokens, 2FA tokens
- `LoginHistory` — audit trail
- `UserSession` — active sessions
- `UserProfile` — extended profile data
- `UserAddress` — address management
- `UserContact` — contact information
- `ActivityLog` — user activity tracking

**Migrations:** Code-first migrations với EF Core

## API Overview

**Authentication Endpoints:**
- `POST /api/auth/register` — User registration
- `POST /api/auth/login` — Login with credentials
- `POST /api/auth/logout` — Logout
- `POST /api/auth/refresh-token` — Refresh JWT token
- `POST /api/auth/forgot-password` — Request password reset
- `POST /api/auth/reset-password` — Reset password with token
- `POST /api/auth/verify-email` — Email verification
- `POST /api/auth/resend-verification` — Resend verification email

**2FA Endpoints:**
- `POST /api/auth/2fa/enable` — Enable 2FA
- `POST /api/auth/2fa/disable` — Disable 2FA
- `POST /api/auth/2fa/verify` — Verify 2FA code
- `GET /api/auth/2fa/qr-code` — Get QR code for authenticator app

**Social Login Endpoints:**
- `GET /api/auth/social/{provider}` — Initiate OAuth flow (Google, Facebook, GitHub, Microsoft)
- `GET /api/auth/social/{provider}/callback` — OAuth callback
- `POST /api/auth/social/link` — Link social account to existing user
- `DELETE /api/auth/social/unlink/{provider}` — Unlink social account

**Profile Endpoints:**
- `GET /api/profile` — Get current user profile
- `PUT /api/profile` — Update profile
- `POST /api/profile/avatar` — Upload avatar
- `DELETE /api/profile/avatar` — Remove avatar
- `PUT /api/profile/settings` — Update language/timezone settings

**User Management Endpoints (Admin):**
- `GET /api/users` — List users (paginated)
- `GET /api/users/{id}` — Get user by ID
- `PUT /api/users/{id}` — Update user
- `DELETE /api/users/{id}` — Delete user
- `POST /api/users/{id}/roles` — Assign roles
- `DELETE /api/users/{id}/roles/{roleId}` — Remove role

**Session Management:**
- `GET /api/sessions` — List active sessions
- `DELETE /api/sessions/{id}` — Revoke session
- `DELETE /api/sessions/all` — Revoke all sessions

**Activity & History:**
- `GET /api/activity` — Get user activity log
- `GET /api/login-history` — Get login history

## Key Components

**DainnUser.Core:**
- `Entities/` — Domain entities
- `Interfaces/` — Repository & service interfaces
- `Enums/` — Enums (UserStatus, LoginProvider, ActivityType, etc.)
- `Exceptions/` — Custom exceptions

**DainnUser.Infrastructure:**
- `Data/DainnUserDbContext.cs` — EF Core DbContext
- `Repositories/` — Repository implementations
- `Identity/` — ASP.NET Core Identity customization
- `Services/` — External service integrations (email, SMS, storage)
- `Migrations/` — EF Core migrations

**DainnUser.Application:**
- `Services/` — Business logic services
- `DTOs/` — Data transfer objects
- `Validators/` — FluentValidation validators
- `Mappings/` — AutoMapper profiles
- `Interfaces/` — Application service interfaces

**DainnUser.Api:**
- `Controllers/` — API controllers
- `Middleware/` — Custom middleware (rate limiting, error handling)
- `Filters/` — Action filters
- `Extensions/` — Service registration extensions

**DainnUser.Web:**
- `Components/` — Razor/Blazor components
- `Pages/` — Razor Pages
- `TagHelpers/` — Custom tag helpers
- `ViewModels/` — View models

## Infrastructure

**Distribution:** NuGet package

**Target Frameworks:** .NET 8.0+

**Package Structure:**
- `DainnUser` — Meta-package (references all below)
- `DainnUser.Core` — Core domain
- `DainnUser.Infrastructure` — EF Core + implementations
- `DainnUser.Application` — Business logic
- `DainnUser.Api` — API components
- `DainnUser.Web` — Web components

**CI/CD:** GitHub Actions (planned)
- Build & test on PR
- Pack & publish to NuGet on release

## Conventions

**Code Style:**
- Follow Microsoft C# Coding Conventions
- Enable nullable reference types
- Use async/await cho tất cả I/O operations
- XML documentation cho public APIs

**Naming:**
- PascalCase: Classes, methods, properties, public fields
- camelCase: Private fields, parameters, local variables
- Prefix interfaces với `I` (e.g., `IUserRepository`)
- Suffix async methods với `Async` (e.g., `GetUserAsync`)

**Project Structure:**
- One entity per file
- Group by feature trong Application layer
- Separate concerns (controllers, services, repositories)

**Configuration:**
- Use Options pattern với `IOptions<T>`
- Validate options on startup
- Support appsettings.json overrides

**Error Handling:**
- Custom exceptions cho domain errors
- Global exception middleware
- Structured error responses

**Security:**
- Never log sensitive data (passwords, tokens)
- Use parameterized queries (EF Core handles this)
- Validate all inputs
- Implement rate limiting
- Use HTTPS only
- Secure cookie settings
- CSRF protection enabled
