# Architecture — DainnUser

## Overview

DainnUser là một .NET class library được thiết kế theo Clean Architecture principles, cung cấp đầy đủ tính năng user management cho các ứng dụng .NET. Library hỗ trợ multiple database providers thông qua Entity Framework Core và có thể được customize dễ dàng theo business logic của từng ứng dụng.

**Core Philosophy:**
- **Separation of Concerns:** Mỗi layer có responsibility rõ ràng
- **Dependency Inversion:** Core domain không phụ thuộc vào infrastructure
- **Extensibility:** Dễ dàng extend và override behaviors
- **Security First:** OWASP Top 10 compliance built-in
- **Configuration Over Code:** Enable/disable features qua appsettings.json

## System Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Consumer Application                     │
│                  (Web API / MVC / Blazor)                    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         │ Install via NuGet
                         │
┌────────────────────────▼────────────────────────────────────┐
│                    DainnUser Library                         │
│                                                              │
│  ┌────────────────────────────────────────────────────┐    │
│  │         Presentation Layer                         │    │
│  │  ┌──────────────────┐  ┌──────────────────┐      │    │
│  │  │  DainnUser.Api   │  │  DainnUser.Web   │      │    │
│  │  │  - Controllers   │  │  - Components    │      │    │
│  │  │  - DTOs          │  │  - Pages         │      │    │
│  │  │  - Filters       │  │  - ViewModels    │      │    │
│  │  └──────────────────┘  └──────────────────┘      │    │
│  └────────────────┬───────────────┬───────────────────┘    │
│                   │               │                         │
│  ┌────────────────▼───────────────▼───────────────────┐    │
│  │         Application Layer                          │    │
│  │         DainnUser.Application                      │    │
│  │  - Services (Business Logic)                       │    │
│  │  - DTOs & Mappings                                 │    │
│  │  - Validators (FluentValidation)                   │    │
│  │  - Interfaces                                      │    │
│  └────────────────┬───────────────────────────────────┘    │
│                   │                                         │
│  ┌────────────────▼───────────────────────────────────┐    │
│  │         Infrastructure Layer                       │    │
│  │         DainnUser.Infrastructure                   │    │
│  │  - EF Core DbContext                               │    │
│  │  - Repositories                                    │    │
│  │  - Identity Integration                            │    │
│  │  - External Services (Email, SMS, Storage)         │    │
│  └────────────────┬───────────────────────────────────┘    │
│                   │                                         │
│  ┌────────────────▼───────────────────────────────────┐    │
│  │         Core/Domain Layer                          │    │
│  │         DainnUser.Core                             │    │
│  │  - Entities (User, Role, etc.)                     │    │
│  │  - Interfaces (IRepository, IService)              │    │
│  │  - Enums & Constants                               │    │
│  │  - Domain Exceptions                               │    │
│  └────────────────────────────────────────────────────┘    │
│                                                              │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           │ EF Core Provider
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
   ┌────▼────┐      ┌─────▼─────┐      ┌────▼────┐
   │SQL Server│      │PostgreSQL │      │  MySQL  │
   └──────────┘      └───────────┘      └─────────┘
```

## Components

### DainnUser.Core

**Location:** `src/DainnUser.Core/`

**Role:** Domain layer chứa business entities và core interfaces. Không có dependencies external.

**Key files:**
- `Entities/User.cs` — Core user entity
- `Entities/Role.cs` — RBAC role entity
- `Entities/UserProfile.cs` — Extended profile data
- `Entities/LoginHistory.cs` — Audit trail
- `Entities/UserSession.cs` — Session management
- `Interfaces/IUserRepository.cs` — Repository contracts
- `Interfaces/IUserService.cs` — Service contracts
- `Enums/UserStatus.cs` — User status enum
- `Enums/LoginProvider.cs` — OAuth provider enum
- `Exceptions/DainnUserException.cs` — Base exception

**Principles:**
- Pure domain logic
- No infrastructure concerns
- Framework-agnostic
- Highly testable

### DainnUser.Infrastructure

**Location:** `src/DainnUser.Infrastructure/`

**Role:** Infrastructure implementations — database, external services, ASP.NET Core Identity integration.

**Key files:**
- `Data/DainnUserDbContext.cs` — EF Core DbContext
- `Data/EntityConfigurations/` — Fluent API configurations
- `Repositories/UserRepository.cs` — User repository implementation
- `Repositories/RoleRepository.cs` — Role repository implementation
- `Identity/ApplicationUser.cs` — ASP.NET Core Identity user
- `Identity/ApplicationRole.cs` — ASP.NET Core Identity role
- `Services/EmailService.cs` — Email sending
- `Services/SmsService.cs` — SMS sending (2FA)
- `Services/StorageService.cs` — Avatar/file storage
- `Migrations/` — EF Core migrations

**Dependencies:**
- DainnUser.Core
- Entity Framework Core
- ASP.NET Core Identity
- External service SDKs (SendGrid, Twilio, Azure Storage, etc.)

### DainnUser.Application

**Location:** `src/DainnUser.Application/`

**Role:** Business logic layer — orchestrates domain và infrastructure để implement use cases.

**Key files:**
- `Services/AuthenticationService.cs` — Login, register, password reset
- `Services/TwoFactorService.cs` — 2FA logic
- `Services/ProfileService.cs` — Profile management
- `Services/SessionService.cs` — Session management
- `Services/ActivityService.cs` — Activity logging
- `DTOs/Auth/RegisterDto.cs` — Registration DTO
- `DTOs/Auth/LoginDto.cs` — Login DTO
- `DTOs/Profile/ProfileDto.cs` — Profile DTO
- `Validators/RegisterDtoValidator.cs` — FluentValidation validators
- `Mappings/MappingProfile.cs` — AutoMapper profiles

**Dependencies:**
- DainnUser.Core
- DainnUser.Infrastructure
- FluentValidation
- AutoMapper

### DainnUser.Api

**Location:** `src/DainnUser.Api/`

**Role:** API layer — controllers, middleware, filters cho RESTful API.

**Key files:**
- `Controllers/AuthController.cs` — Authentication endpoints
- `Controllers/ProfileController.cs` — Profile endpoints
- `Controllers/UserController.cs` — User management (admin)
- `Controllers/SessionController.cs` — Session management
- `Middleware/RateLimitingMiddleware.cs` — Rate limiting
- `Middleware/ExceptionHandlingMiddleware.cs` — Global error handling
- `Filters/ValidateModelAttribute.cs` — Model validation filter
- `Extensions/ServiceCollectionExtensions.cs` — DI registration

**Dependencies:**
- DainnUser.Application
- ASP.NET Core MVC

### DainnUser.Web

**Location:** `src/DainnUser.Web/`

**Role:** Web UI components — Razor Pages, Blazor components, tag helpers.

**Key files:**
- `Components/LoginForm.razor` — Login form component
- `Components/RegisterForm.razor` — Registration form
- `Components/ProfileEditor.razor` — Profile editor
- `Pages/Account/Login.cshtml` — Login page (Razor)
- `Pages/Account/Register.cshtml` — Registration page
- `TagHelpers/UserAvatarTagHelper.cs` — Avatar tag helper
- `ViewModels/LoginViewModel.cs` — View models

**Dependencies:**
- DainnUser.Application
- ASP.NET Core Razor Pages / Blazor

## Data Flow

### Authentication Flow

```
User → Login Request
  ↓
API Controller (DainnUser.Api)
  ↓
AuthenticationService (DainnUser.Application)
  ↓ Validate credentials
UserRepository (DainnUser.Infrastructure)
  ↓ Query database
EF Core → Database
  ↓ Return user
AuthenticationService
  ↓ Generate JWT token
  ↓ Log login history
  ↓ Create session
API Controller
  ↓
User ← JWT Token + User Info
```

### Registration Flow

```
User → Register Request
  ↓
API Controller (DainnUser.Api)
  ↓ Validate DTO
FluentValidation
  ↓
AuthenticationService (DainnUser.Application)
  ↓ Check email exists
  ↓ Hash password
  ↓ Create user
UserRepository (DainnUser.Infrastructure)
  ↓ Save to database
EF Core → Database
  ↓
EmailService (DainnUser.Infrastructure)
  ↓ Send verification email
External Email Provider
  ↓
API Controller
  ↓
User ← Success Response
```

### 2FA Flow

```
User → Enable 2FA Request
  ↓
API Controller
  ↓
TwoFactorService (DainnUser.Application)
  ↓ Generate secret
  ↓ Generate QR code
  ↓ Save to user
UserRepository → Database
  ↓
User ← QR Code

User → Verify 2FA Code
  ↓
TwoFactorService
  ↓ Validate TOTP code
  ↓ Mark 2FA enabled
UserRepository → Database
  ↓
User ← Success
```

## External Services

### Email Service
- **Purpose:** Email verification, password reset, notifications
- **Providers:** SendGrid, SMTP, AWS SES (configurable)
- **Configuration:** `appsettings.json` → `EmailOptions`

### SMS Service
- **Purpose:** 2FA codes via SMS
- **Providers:** Twilio, AWS SNS (configurable)
- **Configuration:** `appsettings.json` → `SmsOptions`

### Storage Service
- **Purpose:** Avatar/file uploads
- **Providers:** Azure Blob Storage, AWS S3, Local filesystem (configurable)
- **Configuration:** `appsettings.json` → `StorageOptions`

### OAuth Providers
- **Purpose:** Social login
- **Providers:** Google, Facebook, GitHub, Microsoft
- **Configuration:** `appsettings.json` → `OAuthOptions`

### reCAPTCHA
- **Purpose:** Bot protection
- **Provider:** Google reCAPTCHA v2/v3
- **Configuration:** `appsettings.json` → `RecaptchaOptions`

## Environment Variables

Required configuration in `appsettings.json`:

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer", // SqlServer, PostgreSQL, MySQL, SQLite
      "ConnectionString": "..."
    },
    "Jwt": {
      "SecretKey": "...",
      "Issuer": "...",
      "Audience": "...",
      "ExpirationMinutes": 60
    },
    "Email": {
      "Provider": "SendGrid", // SendGrid, Smtp, AwsSes
      "ApiKey": "...",
      "FromEmail": "...",
      "FromName": "..."
    },
    "Sms": {
      "Provider": "Twilio", // Twilio, AwsSns
      "AccountSid": "...",
      "AuthToken": "...",
      "FromNumber": "..."
    },
    "Storage": {
      "Provider": "AzureBlob", // AzureBlob, AwsS3, Local
      "ConnectionString": "...",
      "ContainerName": "..."
    },
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "...",
        "ClientSecret": "..."
      },
      "Facebook": {
        "Enabled": true,
        "AppId": "...",
        "AppSecret": "..."
      },
      "GitHub": {
        "Enabled": false,
        "ClientId": "...",
        "ClientSecret": "..."
      },
      "Microsoft": {
        "Enabled": false,
        "ClientId": "...",
        "ClientSecret": "..."
      }
    },
    "Recaptcha": {
      "Enabled": true,
      "SiteKey": "...",
      "SecretKey": "..."
    },
    "Security": {
      "RequireEmailVerification": true,
      "RequirePhoneVerification": false,
      "EnableTwoFactor": true,
      "PasswordRequirements": {
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireNonAlphanumeric": true,
        "RequiredLength": 12
      },
      "Lockout": {
        "MaxFailedAccessAttempts": 5,
        "LockoutDurationMinutes": 15
      },
      "RateLimiting": {
        "Enabled": true,
        "MaxRequestsPerMinute": 60
      }
    }
  }
}
```

## Security Architecture

### Defense in Depth

**Layer 1: Input Validation**
- FluentValidation cho DTOs
- Model binding validation
- Sanitization

**Layer 2: Authentication**
- JWT tokens với expiration
- Refresh token rotation
- 2FA support
- OAuth integration

**Layer 3: Authorization**
- RBAC với ASP.NET Core Identity
- Policy-based authorization
- Resource-based authorization

**Layer 4: Data Protection**
- Password hashing (PBKDF2/Argon2)
- Data encryption at rest
- TLS/HTTPS enforcement

**Layer 5: Monitoring**
- Login history
- Activity logging
- Failed attempt tracking
- Anomaly detection (planned)

### OWASP Top 10 Mapping

Xem `.claude/skills/security-review.md` cho chi tiết implementation.
