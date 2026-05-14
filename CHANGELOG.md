# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-05-14

Initial stable release of the DainnUser library suite.

### Packages

| Package | Description |
|---|---|
| `DainnUser.Core` | Domain models, entities, interfaces, exceptions, enums |
| `DainnUser.Application` | Business-logic services, validators, DI registration |
| `DainnUser.Infrastructure` | EF Core, JWT, email, storage, external-provider implementations |
| `DainnUser.Web` | Blazor/Razor UI component library |
| `DainnUser.OpenIddict` | Optional OpenID Connect module (powered by OpenIddict) |
| `DainnStripe` | Stripe payment integration (checkout, subscriptions, webhooks) |

---

### Authentication & Security

- **JWT authentication** — access-token + refresh-token pair, HS256, configurable expiry
- **Refresh token rotation** — single-use tokens with reuse detection; reuse triggers full session revocation
- **Email verification** — 24-hour token, single-use, resend endpoint
- **Password reset** — 1-hour secure token, PBKDF2 password hashing (ASP.NET Core Identity)
- **Social login** — Google, Facebook, GitHub, Microsoft OAuth 2.0 integration
- **Two-factor authentication** — TOTP (RFC 6238), QR-code setup via `otpauth://` URI, 10 single-use backup codes, "trust this device" tokens, backup-code regeneration
- **Account lockout** — configurable failed-attempt threshold and lock duration, admin-unlock endpoint
- **Rate limiting** — sliding-window per-endpoint with configurable rules; IP whitelist; in-memory registry
- **reCAPTCHA v2/v3** — server-side verification with configurable score threshold
- **Login history** — full audit trail of sign-in attempts with IP, user-agent, success/failure

### User Management

- **Profile management** — first/last name, bio, website, phone; partial updates
- **Avatar upload** — multi-provider storage backend, server-side image processing
- **Address management** — full CRUD, multiple addresses per user, primary-address designation, country/state validation
- **Contact information** — multiple contacts per user (Phone, Email, WhatsApp, Telegram, etc.), primary contact per type, OTP verification with rate limiting, SHA-256 hashed tokens
- **Role-based access control** — role CRUD, role assignment to users, `[Authorize(Roles = "...")]` compatible
- **User management** — admin CRUD, activate/deactivate, search/filter, paginated list
- **Session management** — multi-device active-session tracking, per-session revoke, revoke-all-except-current, last-activity timestamps

### Email Service

- **Multi-provider** — SMTP (via MailKit), SendGrid, AWS SES; selected via `DainnUser:Email:Provider` config
- **HTML templates** — email verification, password reset, 2FA code (per-template expiry messaging)
- **Attachment support** — `EmailAttachment` model, passed through all providers

### Storage Service

- **Multi-provider** — Local filesystem, Azure Blob Storage, AWS S3
- **Pluggable** — `IStorageService` interface, provider selected via configuration

### API Endpoints

#### Authentication
| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/register` | Register with email verification |
| `POST` | `/api/auth/verify-email` | Verify email address |
| `POST` | `/api/auth/resend-verification` | Resend verification email |
| `POST` | `/api/auth/login` | Login, returns JWT + refresh token |
| `POST` | `/api/auth/refresh` | Rotate refresh token |
| `POST` | `/api/auth/logout` | Revoke current session |
| `POST` | `/api/auth/forgot-password` | Send password-reset email |
| `POST` | `/api/auth/reset-password` | Reset password with token |
| `POST` | `/api/auth/social/{provider}` | Initiate social OAuth flow |
| `POST` | `/api/auth/social/{provider}/callback` | OAuth callback |
| `POST` | `/api/auth/admin/unlock-account/{userId}` | Admin: unlock locked account |

#### Two-Factor Authentication
| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/2fa/prepare` | Generate TOTP secret + otpauth URI |
| `POST` | `/api/auth/2fa/enable` | Confirm setup with first code; returns backup codes |
| `POST` | `/api/auth/2fa/disable` | Disable 2FA with confirmation code |
| `POST` | `/api/auth/2fa/verify` | Verify TOTP code at login |
| `POST` | `/api/auth/2fa/backup-codes/regenerate` | Regenerate backup codes |

#### Profile & User
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/profile` | Get current user profile |
| `PUT` | `/api/profile` | Update profile |
| `POST` | `/api/profile/avatar` | Upload avatar |
| `DELETE` | `/api/profile/avatar` | Remove avatar |
| `PUT` | `/api/profile/settings` | Update account settings |

#### Addresses
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/profile/addresses` | List addresses |
| `GET` | `/api/profile/addresses/{id}` | Get address |
| `POST` | `/api/profile/addresses` | Add address |
| `PUT` | `/api/profile/addresses/{id}` | Update address |
| `DELETE` | `/api/profile/addresses/{id}` | Delete address |
| `PATCH` | `/api/profile/addresses/{id}/set-primary` | Set as primary |

#### Contacts
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/profile/contacts` | List contacts |
| `GET` | `/api/profile/contacts/{id}` | Get contact |
| `POST` | `/api/profile/contacts` | Add contact |
| `PUT` | `/api/profile/contacts/{id}` | Update contact |
| `DELETE` | `/api/profile/contacts/{id}` | Delete contact |
| `PATCH` | `/api/profile/contacts/{id}/set-primary` | Set as primary |
| `POST` | `/api/profile/contacts/{id}/send-verification` | Send OTP |
| `POST` | `/api/profile/contacts/{id}/verify` | Verify OTP |

#### Sessions
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/sessions` | List active sessions |
| `DELETE` | `/api/sessions/{id}` | Revoke session |
| `DELETE` | `/api/sessions` | Revoke all other sessions |

### UI Components (`DainnUser.Web`)

Blazor component library targeting .NET 8. All components use Bootstrap 5, emit actions via `EventCallback` (no direct service calls), and support `IsSubmitting`, `ErrorMessage`, `CssClass` parameters.

| Component | Description |
|---|---|
| `LoginForm` | Email/password + optional RememberMe + SocialLoginButtons |
| `RegisterForm` | Username/email/password with confirmation |
| `ForgotPasswordForm` | Email-only reset trigger |
| `ResetPasswordForm` | Token + new password + confirm |
| `ProfileForm` | First/last name, phone, website, bio (responsive grid) |
| `AvatarUpload` | `InputFile` with client-side type/size validation, avatar preview |
| `TwoFactorCodeForm` | 6-digit OTP input for login 2FA challenge |
| `TwoFactorSetup` | Full 4-state setup flow (Disabled → Setup → BackupCodes → Enabled); `RenderFragment QrCode` slot |
| `SessionList` | Session rows with IP/UA/timestamps; per-session revoke + revoke-all-others |
| `SocialLoginButtons` | Renders configurable list of OAuth provider links |

### Service Registration

```csharp
// Program.cs
builder.Services.AddDainnUser(builder.Configuration, options =>
{
    options.EnableTwoFactor = true;
    options.EnableSocialLogin = true;
    options.RequireEmailVerification = true;
    options.EnableAccountLockout = true;
    options.EnableSessionManagement = true;
    options.EnableRateLimiting = true;
});

builder.Services.AddDainnUserJwtAuthentication();

app.UseDainnUser();
app.UseAuthentication();
app.UseAuthorization();
```

### Database Support

Supported via Entity Framework Core:

- SQL Server
- PostgreSQL (Npgsql)
- MySQL / MariaDB (Pomelo)
- SQLite

Selected via `DainnUser:Database:Provider` configuration key.

### Security

- Passwords: PBKDF2 via ASP.NET Core Identity `PasswordHasher`
- Tokens: 32-byte `RandomNumberGenerator`, SHA-256 hashed at rest
- Refresh tokens: SHA-256 hashed, single-use, rotation with reuse detection
- 2FA backup codes: SHA-256 hashed, single-use
- SQL injection: prevented via EF Core parameterised queries
- OWASP Top 10 compliance validated via automated security test suite (59 tests)
- Rate limiting: sliding window, configurable per-endpoint
- Account lockout: configurable threshold and duration
- Input validation: FluentValidation + DataAnnotations throughout

### Testing

- **Unit tests:** 423 passing
- **Integration tests:** 113 passing, 1 skipped (EF Core InMemory LINQ limitation — does not affect production)
- **Security tests:** 59 passing (OWASP Top 10 coverage)
- **Total:** 595 passing

### Known Issues

- `RecaptchaService` integration tests may be flaky when the test reCAPTCHA keys return inconsistent results from Google's API. This is an external-dependency issue and does not affect library correctness.
- Integration test `ResendVerificationEmail_EndToEnd_RevokesOldTokens` is skipped due to an EF Core InMemory provider limitation with collection modifications. The production code path is covered by unit tests.

---

[1.0.0]: https://github.com/dainn/dainn-user/releases/tag/v1.0.0
