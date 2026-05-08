# DainnUser — Memory

**Stack:** .NET/C# Class Library, EF Core | **DB:** Dynamic provider support | **Users:** .NET Developers

**Luôn nhớ:** Library phải flexible để custom theo business logic. Features enable/disable qua appsettings. OWASP Top 10 compliance bắt buộc.

**Mode:** CONSULTANT AGENT

---

## Current State

- Status: freshly initialized by Blueberry Sensei
- Active branch: (chưa có git repo)
- Last task: initial setup

## Key Components

Dự kiến structure:
- `DainnUser.Core/` — Domain models, interfaces
- `DainnUser.Infrastructure/` — EF Core, database implementations
- `DainnUser.Application/` — Business logic, services
- `DainnUser.Api/` — API controllers, DTOs
- `DainnUser.Web/` — Web components (Razor/Blazor)

## Features Planned

**Authentication:**
- Register / Login / Forgot Password
- 2FA (Two-Factor Authentication)
- Social Login: Google, Facebook, GitHub, Microsoft
- Session management
- Login history
- Google reCAPTCHA

**Authorization:**
- RBAC (Role-Based Access Control)

**Profile Management:**
- View/Edit profile
- Avatar upload
- Language & timezone settings
- Activity history
- Address & contact management

**Security:**
- OWASP Top 10 compliance
- Input validation
- SQL injection prevention
- XSS protection
- CSRF protection

## In Progress

(none)

## Recent Decisions

Xem `.claude/memory/decisions.md`

---

_Keep this file under 200 lines. Archive old context with compress-context skill._
