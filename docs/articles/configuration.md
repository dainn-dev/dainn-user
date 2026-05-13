# Configuration Reference — DainnUser

Complete configuration guide for DainnUser library.

## Overview

DainnUser configuration is managed through `appsettings.json` and can be overridden via environment variables. All settings are under the `DainnUser` root key.

**Configuration Hierarchy:**
1. `appsettings.json` (default values)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables (highest priority)
4. Code-based configuration via `AddDainnUser()` options

---

## Database Configuration

Configure database provider and connection string.

### Schema

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=MyApp;Trusted_Connection=True;"
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Provider` | string | Yes | `"SqlServer"` | Database provider: `SqlServer`, `PostgreSQL`, `MySQL`, `SQLite` |
| `ConnectionString` | string | Yes | - | Database connection string |

### Examples

**SQL Server:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=MyApp;Trusted_Connection=True;TrustServerCertificate=True;"
    }
  }
}
```

**PostgreSQL:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=localhost;Database=myapp;Username=postgres;Password=password;Port=5432"
    }
  }
}
```

**MySQL:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "MySQL",
      "ConnectionString": "Server=localhost;Database=myapp;User=root;Password=password;Port=3306"
    }
  }
}
```

**SQLite (Development):**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SQLite",
      "ConnectionString": "Data Source=dainnuser.db"
    }
  }
}
```

### Environment Variables

```bash
DainnUser__Database__Provider=PostgreSQL
DainnUser__Database__ConnectionString="Host=localhost;Database=myapp;Username=postgres;Password=password"
```

---

## JWT Configuration

Configure JSON Web Token authentication.

### Schema

```json
{
  "DainnUser": {
    "Jwt": {
      "SecretKey": "your-secret-key-min-32-characters-long",
      "Issuer": "https://yourdomain.com",
      "Audience": "https://yourdomain.com",
      "ExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 7,
      "RotateRefreshTokens": true
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `SecretKey` | string | Yes | - | Secret key for signing tokens (min 32 characters) |
| `Issuer` | string | Yes | - | Token issuer (your domain) |
| `Audience` | string | Yes | - | Token audience (your domain) |
| `ExpirationMinutes` | int | No | `60` | Access token expiration (15-60 recommended) |
| `RefreshTokenExpirationDays` | int | No | `7` | Refresh token expiration (7-30 recommended) |
| `RotateRefreshTokens` | bool | No | `true` | Rotate refresh tokens on use (recommended) |

### Security Notes

- **SecretKey:** Must be at least 32 characters. Use cryptographically secure random string.
- **ExpirationMinutes:** Shorter is more secure (15-60 minutes). Use refresh tokens for longer sessions.
- **RotateRefreshTokens:** Always `true` in production for security.

### Example

```json
{
  "DainnUser": {
    "Jwt": {
      "SecretKey": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6",
      "Issuer": "https://api.myapp.com",
      "Audience": "https://myapp.com",
      "ExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7,
      "RotateRefreshTokens": true
    }
  }
}
```

### Environment Variables

```bash
DainnUser__Jwt__SecretKey="your-secret-key-here"
DainnUser__Jwt__Issuer="https://yourdomain.com"
DainnUser__Jwt__Audience="https://yourdomain.com"
DainnUser__Jwt__ExpirationMinutes=15
```

---

## Email Configuration

Configure email provider for verification, password reset, and notifications.

### Schema

```json
{
  "DainnUser": {
    "Email": {
      "Provider": "Smtp",
      "SmtpHost": "smtp.gmail.com",
      "SmtpPort": 587,
      "SmtpUsername": "your-email@gmail.com",
      "SmtpPassword": "your-app-password",
      "FromEmail": "noreply@yourdomain.com",
      "FromName": "Your App",
      "EnableSsl": true
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Provider` | string | Yes | `"Smtp"` | Email provider: `Smtp`, `SendGrid`, `Mailgun` |
| `SmtpHost` | string | Yes* | - | SMTP server hostname |
| `SmtpPort` | int | Yes* | `587` | SMTP server port (587 for TLS, 465 for SSL) |
| `SmtpUsername` | string | No | - | SMTP authentication username |
| `SmtpPassword` | string | No | - | SMTP authentication password |
| `FromEmail` | string | Yes | - | Sender email address |
| `FromName` | string | No | `"DainnUser"` | Sender display name |
| `EnableSsl` | bool | No | `true` | Enable SSL/TLS encryption |

*Required for SMTP provider

### Examples

**Gmail (Production):**
```json
{
  "DainnUser": {
    "Email": {
      "Provider": "Smtp",
      "SmtpHost": "smtp.gmail.com",
      "SmtpPort": 587,
      "SmtpUsername": "your-email@gmail.com",
      "SmtpPassword": "your-app-password",
      "FromEmail": "noreply@yourdomain.com",
      "FromName": "My App",
      "EnableSsl": true
    }
  }
}
```

**MailHog (Development):**
```json
{
  "DainnUser": {
    "Email": {
      "Provider": "Smtp",
      "SmtpHost": "localhost",
      "SmtpPort": 1025,
      "SmtpUsername": "",
      "SmtpPassword": "",
      "FromEmail": "dev@localhost",
      "FromName": "DainnUser Dev",
      "EnableSsl": false
    }
  }
}
```

**SendGrid:**
```json
{
  "DainnUser": {
    "Email": {
      "Provider": "SendGrid",
      "ApiKey": "your-sendgrid-api-key",
      "FromEmail": "noreply@yourdomain.com",
      "FromName": "My App"
    }
  }
}
```

### Environment Variables

```bash
DainnUser__Email__SmtpHost="smtp.gmail.com"
DainnUser__Email__SmtpPort=587
DainnUser__Email__SmtpUsername="your-email@gmail.com"
DainnUser__Email__SmtpPassword="your-app-password"
DainnUser__Email__FromEmail="noreply@yourdomain.com"
```

---

## SMS Configuration

Configure SMS provider for two-factor authentication.

### Schema

```json
{
  "DainnUser": {
    "Sms": {
      "Provider": "Twilio",
      "AccountSid": "your-twilio-account-sid",
      "AuthToken": "your-twilio-auth-token",
      "FromPhoneNumber": "+1234567890"
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Provider` | string | Yes | `"Twilio"` | SMS provider: `Twilio`, `Nexmo` |
| `AccountSid` | string | Yes* | - | Twilio account SID |
| `AuthToken` | string | Yes* | - | Twilio auth token |
| `FromPhoneNumber` | string | Yes | - | Sender phone number (E.164 format) |

*Required for Twilio provider

### Example

```json
{
  "DainnUser": {
    "Sms": {
      "Provider": "Twilio",
      "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
      "AuthToken": "your-auth-token",
      "FromPhoneNumber": "+15551234567"
    }
  }
}
```

### Environment Variables

```bash
DainnUser__Sms__Provider="Twilio"
DainnUser__Sms__AccountSid="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
DainnUser__Sms__AuthToken="your-auth-token"
DainnUser__Sms__FromPhoneNumber="+15551234567"
```

---

## Storage Configuration

Configure file storage for avatars and uploads.

### Schema

```json
{
  "DainnUser": {
    "Storage": {
      "Provider": "Local",
      "LocalPath": "wwwroot/uploads/avatars",
      "MaxFileSizeMB": 5,
      "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"]
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Provider` | string | Yes | `"Local"` | Storage provider: `Local`, `AzureBlob`, `S3` |
| `LocalPath` | string | Yes* | `"wwwroot/uploads"` | Local storage path |
| `MaxFileSizeMB` | int | No | `5` | Max file size in MB |
| `AllowedExtensions` | string[] | No | `[".jpg", ".jpeg", ".png", ".gif", ".webp"]` | Allowed file extensions |

*Required for Local provider

### Examples

**Local Storage:**
```json
{
  "DainnUser": {
    "Storage": {
      "Provider": "Local",
      "LocalPath": "wwwroot/uploads/avatars",
      "MaxFileSizeMB": 5,
      "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"]
    }
  }
}
```

**Azure Blob Storage:**
```json
{
  "DainnUser": {
    "Storage": {
      "Provider": "AzureBlob",
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=...",
      "ContainerName": "avatars",
      "MaxFileSizeMB": 5,
      "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"]
    }
  }
}
```

**AWS S3:**
```json
{
  "DainnUser": {
    "Storage": {
      "Provider": "S3",
      "BucketName": "my-app-avatars",
      "Region": "us-east-1",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "MaxFileSizeMB": 5,
      "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"]
    }
  }
}
```

---

## OAuth Configuration

Configure social login providers.

### Schema

```json
{
  "DainnUser": {
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret",
        "CallbackPath": "/signin-google"
      },
      "Facebook": {
        "Enabled": true,
        "AppId": "your-facebook-app-id",
        "AppSecret": "your-facebook-app-secret",
        "CallbackPath": "/signin-facebook"
      },
      "GitHub": {
        "Enabled": true,
        "ClientId": "your-github-client-id",
        "ClientSecret": "your-github-client-secret",
        "CallbackPath": "/signin-github"
      },
      "Microsoft": {
        "Enabled": true,
        "ClientId": "your-microsoft-client-id",
        "ClientSecret": "your-microsoft-client-secret",
        "CallbackPath": "/signin-microsoft"
      }
    }
  }
}
```

### Properties (per provider)

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Enabled` | bool | No | `false` | Enable this OAuth provider |
| `ClientId` / `AppId` | string | Yes* | - | OAuth client ID |
| `ClientSecret` / `AppSecret` | string | Yes* | - | OAuth client secret |
| `CallbackPath` | string | No | `/signin-{provider}` | OAuth callback path |

*Required if `Enabled` is `true`

### Example

```json
{
  "DainnUser": {
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "123456789-abcdefghijklmnop.apps.googleusercontent.com",
        "ClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxx",
        "CallbackPath": "/signin-google"
      },
      "Facebook": {
        "Enabled": false
      },
      "GitHub": {
        "Enabled": true,
        "ClientId": "Iv1.1234567890abcdef",
        "ClientSecret": "1234567890abcdef1234567890abcdef12345678",
        "CallbackPath": "/signin-github"
      }
    }
  }
}
```

### Environment Variables

```bash
DainnUser__OAuth__Google__Enabled=true
DainnUser__OAuth__Google__ClientId="your-client-id"
DainnUser__OAuth__Google__ClientSecret="your-client-secret"
```

---

## reCAPTCHA Configuration

Configure Google reCAPTCHA for bot protection.

### Schema

```json
{
  "DainnUser": {
    "Recaptcha": {
      "Enabled": true,
      "SiteKey": "your-recaptcha-site-key",
      "SecretKey": "your-recaptcha-secret-key",
      "Version": "v3",
      "MinimumScore": 0.5
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Enabled` | bool | No | `false` | Enable reCAPTCHA |
| `SiteKey` | string | Yes* | - | reCAPTCHA site key |
| `SecretKey` | string | Yes* | - | reCAPTCHA secret key |
| `Version` | string | No | `"v3"` | reCAPTCHA version: `v2`, `v3` |
| `MinimumScore` | double | No | `0.5` | Minimum score for v3 (0.0-1.0) |

*Required if `Enabled` is `true`

### Example

```json
{
  "DainnUser": {
    "Recaptcha": {
      "Enabled": true,
      "SiteKey": "6LcXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
      "SecretKey": "6LcXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
      "Version": "v3",
      "MinimumScore": 0.5
    }
  }
}
```

---

## Security Configuration

Configure security features and password requirements.

### Schema

```json
{
  "DainnUser": {
    "Security": {
      "RequireEmailVerification": true,
      "RequireHttps": true,
      "PasswordHasher": "Argon2",
      "PasswordRequirements": {
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireNonAlphanumeric": true,
        "RequiredLength": 12,
        "RequiredUniqueChars": 4
      },
      "Lockout": {
        "Enabled": true,
        "MaxFailedAccessAttempts": 5,
        "LockoutDurationMinutes": 15
      },
      "RateLimiting": {
        "Enabled": true,
        "MaxRequestsPerMinute": 60,
        "Rules": [
          {
            "Endpoint": "/api/auth/login",
            "MaxRequests": 5,
            "WindowSeconds": 60
          },
          {
            "Endpoint": "/api/auth/register",
            "MaxRequests": 3,
            "WindowSeconds": 300
          }
        ]
      }
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `RequireEmailVerification` | bool | No | `true` | Require email verification before login |
| `RequireHttps` | bool | No | `true` | Enforce HTTPS (always `true` in production) |
| `PasswordHasher` | string | No | `"Argon2"` | Password hasher: `Argon2`, `PBKDF2` |

### Password Requirements

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `RequireDigit` | bool | No | `true` | Require at least one digit |
| `RequireLowercase` | bool | No | `true` | Require at least one lowercase letter |
| `RequireUppercase` | bool | No | `true` | Require at least one uppercase letter |
| `RequireNonAlphanumeric` | bool | No | `true` | Require at least one special character |
| `RequiredLength` | int | No | `12` | Minimum password length (8-128) |
| `RequiredUniqueChars` | int | No | `4` | Minimum unique characters |

### Lockout Configuration

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Enabled` | bool | No | `true` | Enable account lockout |
| `MaxFailedAccessAttempts` | int | No | `5` | Max failed login attempts before lockout |
| `LockoutDurationMinutes` | int | No | `15` | Lockout duration in minutes |

### Rate Limiting

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Enabled` | bool | No | `true` | Enable rate limiting |
| `MaxRequestsPerMinute` | int | No | `60` | Global max requests per minute per IP |
| `Rules` | array | No | `[]` | Endpoint-specific rate limit rules |

### Example

```json
{
  "DainnUser": {
    "Security": {
      "RequireEmailVerification": true,
      "RequireHttps": true,
      "PasswordHasher": "Argon2",
      "PasswordRequirements": {
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireNonAlphanumeric": true,
        "RequiredLength": 12,
        "RequiredUniqueChars": 4
      },
      "Lockout": {
        "Enabled": true,
        "MaxFailedAccessAttempts": 5,
        "LockoutDurationMinutes": 15
      },
      "RateLimiting": {
        "Enabled": true,
        "MaxRequestsPerMinute": 60,
        "Rules": [
          {
            "Endpoint": "/api/auth/login",
            "MaxRequests": 5,
            "WindowSeconds": 60
          }
        ]
      }
    }
  }
}
```

---

## Session Configuration

Configure session management.

### Schema

```json
{
  "DainnUser": {
    "Session": {
      "Enabled": true,
      "TimeoutMinutes": 30,
      "SlidingExpiration": true,
      "MaxActiveSessions": 5
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `Enabled` | bool | No | `true` | Enable session management |
| `TimeoutMinutes` | int | No | `30` | Session timeout in minutes |
| `SlidingExpiration` | bool | No | `true` | Reset timeout on activity |
| `MaxActiveSessions` | int | No | `5` | Max concurrent sessions per user (0 = unlimited) |

### Example

```json
{
  "DainnUser": {
    "Session": {
      "Enabled": true,
      "TimeoutMinutes": 30,
      "SlidingExpiration": true,
      "MaxActiveSessions": 5
    }
  }
}
```

---

## Feature Flags

Enable or disable features.

### Schema

```json
{
  "DainnUser": {
    "Features": {
      "EnableRegistration": true,
      "EnableSocialLogin": true,
      "EnableTwoFactor": true,
      "EnableEmailVerification": true,
      "EnablePhoneVerification": false,
      "EnableRecaptcha": true,
      "EnableAccountLockout": true,
      "EnableSessionManagement": true,
      "EnableActivityLogging": true
    }
  }
}
```

### Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `EnableRegistration` | bool | No | `true` | Allow new user registration |
| `EnableSocialLogin` | bool | No | `false` | Enable OAuth social login |
| `EnableTwoFactor` | bool | No | `false` | Enable 2FA functionality |
| `EnableEmailVerification` | bool | No | `true` | Require email verification |
| `EnablePhoneVerification` | bool | No | `false` | Enable phone verification |
| `EnableRecaptcha` | bool | No | `false` | Enable reCAPTCHA |
| `EnableAccountLockout` | bool | No | `true` | Enable account lockout |
| `EnableSessionManagement` | bool | No | `true` | Enable session tracking |
| `EnableActivityLogging` | bool | No | `true` | Enable activity audit logs |

### Example

```json
{
  "DainnUser": {
    "Features": {
      "EnableRegistration": true,
      "EnableSocialLogin": true,
      "EnableTwoFactor": true,
      "EnableEmailVerification": true,
      "EnableRecaptcha": true,
      "EnableAccountLockout": true,
      "EnableSessionManagement": true,
      "EnableActivityLogging": true
    }
  }
}
```

---

## Complete Configuration Example

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=localhost;Database=myapp;Username=postgres;Password=password"
    },
    "Jwt": {
      "SecretKey": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6",
      "Issuer": "https://api.myapp.com",
      "Audience": "https://myapp.com",
      "ExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7,
      "RotateRefreshTokens": true
    },
    "Email": {
      "Provider": "Smtp",
      "SmtpHost": "smtp.gmail.com",
      "SmtpPort": 587,
      "SmtpUsername": "noreply@myapp.com",
      "SmtpPassword": "app-password-here",
      "FromEmail": "noreply@myapp.com",
      "FromName": "My App",
      "EnableSsl": true
    },
    "Storage": {
      "Provider": "Local",
      "LocalPath": "wwwroot/uploads/avatars",
      "MaxFileSizeMB": 5,
      "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"]
    },
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret"
      }
    },
    "Security": {
      "RequireEmailVerification": true,
      "RequireHttps": true,
      "PasswordHasher": "Argon2",
      "PasswordRequirements": {
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireNonAlphanumeric": true,
        "RequiredLength": 12,
        "RequiredUniqueChars": 4
      },
      "Lockout": {
        "Enabled": true,
        "MaxFailedAccessAttempts": 5,
        "LockoutDurationMinutes": 15
      },
      "RateLimiting": {
        "Enabled": true,
        "MaxRequestsPerMinute": 60
      }
    },
    "Session": {
      "Enabled": true,
      "TimeoutMinutes": 30,
      "SlidingExpiration": true,
      "MaxActiveSessions": 5
    },
    "Features": {
      "EnableRegistration": true,
      "EnableSocialLogin": true,
      "EnableTwoFactor": true,
      "EnableEmailVerification": true,
      "EnableRecaptcha": false,
      "EnableAccountLockout": true,
      "EnableSessionManagement": true,
      "EnableActivityLogging": true
    }
  }
}
```

---

## Environment-Specific Configuration

### Development

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SQLite",
      "ConnectionString": "Data Source=dainnuser.db"
    },
    "Email": {
      "SmtpHost": "localhost",
      "SmtpPort": 1025,
      "EnableSsl": false
    },
    "Security": {
      "RequireHttps": false,
      "PasswordRequirements": {
        "RequiredLength": 8
      }
    }
  }
}
```

### Production

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=prod-db.example.com;Database=myapp;Username=appuser;Password=***"
    },
    "Security": {
      "RequireHttps": true,
      "RequireEmailVerification": true,
      "PasswordRequirements": {
        "RequiredLength": 12
      },
      "RateLimiting": {
        "Enabled": true
      }
    }
  }
}
```

---

## Code-Based Configuration

Override settings in `Program.cs`:

```csharp
builder.Services.AddDainnUser(builder.Configuration, options =>
{
    options.EnableSocialLogin = true;
    options.EnableTwoFactor = true;
    options.RequireEmailVerification = true;
    options.EnableAccountLockout = true;
    options.EnableSessionManagement = true;
    options.EnableActivityLogging = true;
});
```

---

## Validation

DainnUser validates configuration on startup. Invalid configuration throws `InvalidOperationException` with details.

**Common validation errors:**
- Missing required fields (Database.ConnectionString, Jwt.SecretKey)
- Invalid JWT secret key (< 32 characters)
- Invalid database provider
- Invalid password requirements (RequiredLength < 8)
- OAuth enabled but missing credentials

---

## See Also

- [Getting Started](getting-started.md)
- [Security Guide](security.md)
- [Troubleshooting](troubleshooting.md)
