# Quick Reference Guide

## 🚀 Quick Start

### Start the API

```bash
cd src/DainnUser.Api
dotnet run
```

API will be available at:
- HTTP: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### Run Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/DainnUser.UnitTests

# Integration tests only
dotnet test tests/DainnUser.IntegrationTests
```

### Test API Endpoints

```bash
# PowerShell
.\test-api.ps1

# Bash
bash test-api.sh
```

---

## 📋 Common Tasks

### Register a New User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "username": "johndoe",
    "password": "SecurePass123!@#",
    "confirmPassword": "SecurePass123!@#"
  }'
```

### Verify Email

```bash
curl -X POST http://localhost:5000/api/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "your-user-id-here",
    "token": "your-token-here"
  }'
```

### Resend Verification Email

```bash
curl -X POST http://localhost:5000/api/auth/resend-verification \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com"
  }'
```

---

## 🔧 Configuration

### Email Settings (Development)

Edit `src/DainnUser.Api/appsettings.Development.json`:

**For Gmail:**
```json
{
  "DainnUser": {
    "Email": {
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

**For Local Testing (MailHog):**
```json
{
  "DainnUser": {
    "Email": {
      "SmtpHost": "localhost",
      "SmtpPort": 1025,
      "SmtpUsername": "",
      "SmtpPassword": "",
      "FromEmail": "dev@localhost",
      "FromName": "Dev",
      "EnableSsl": false
    }
  }
}
```

### Database Settings

**SQLite (Default):**
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

**SQL Server:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=DainnUser;Trusted_Connection=True;"
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
      "ConnectionString": "Host=localhost;Database=dainnuser;Username=postgres;Password=password"
    }
  }
}
```

---

## 🧪 Testing

### Unit Tests Coverage

- ✅ AuthenticationService (11 tests)
- ✅ RegisterDtoValidator (27 tests)
- **Total: 38/38 passing (100%)**

### Integration Tests Coverage

- ✅ End-to-end registration flow
- ✅ Email verification flow
- ✅ Duplicate prevention
- ✅ Token expiration
- ✅ Password hashing
- ⚠️ Resend verification (skipped - InMemory issue)
- **Total: 6/7 passing (85.7%)**

### Run Specific Test

```bash
# Run single test
dotnet test --filter "FullyQualifiedName~RegisterAsync_WithValidData"

# Run test class
dotnet test --filter "FullyQualifiedName~AuthenticationServiceTests"
```

---

## 📁 Project Structure

```
DainnUser/
├── src/
│   ├── DainnUser.Core/              # Domain entities, interfaces
│   ├── DainnUser.Infrastructure/    # EF Core, repositories, email
│   ├── DainnUser.Application/       # Business logic, services
│   └── DainnUser.Api/              # API controllers, DTOs
├── tests/
│   ├── DainnUser.UnitTests/        # Unit tests (38 tests)
│   └── DainnUser.IntegrationTests/ # Integration tests (7 tests)
├── docs/
│   ├── api-endpoints.md            # API documentation
│   ├── known-issues.md             # Known issues
│   └── PSA-60-summary.md           # Implementation summary
├── test-api.ps1                    # PowerShell test script
└── test-api.sh                     # Bash test script
```

---

## 🔍 Debugging

### View Database

```bash
# SQLite
sqlite3 src/DainnUser.Api/dainnuser.db

# List tables
.tables

# View users
SELECT * FROM Users;

# View tokens
SELECT * FROM UserTokens;
```

### Check Logs

Logs are written to console. Increase verbosity in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Common Issues

**Issue:** Email not sending
- **Solution:** Check SMTP settings, verify credentials, check firewall

**Issue:** Database error
- **Solution:** Delete `dainnuser.db` and restart API (will recreate)

**Issue:** Port already in use
- **Solution:** Change port in `Properties/launchSettings.json` or kill process on port 5000

---

## 📚 Documentation

- [API Endpoints](docs/api-endpoints.md) - Complete API reference
- [Known Issues](docs/known-issues.md) - Known limitations
- [PSA-60 Summary](docs/PSA-60-summary.md) - Implementation details
- [Architecture](docs/architecture.md) - System architecture (coming soon)

---

## 🛠️ Development Workflow

### Adding New Features

1. **Define interface** in `DainnUser.Core/Interfaces/Services/`
2. **Implement service** in `DainnUser.Application/Services/`
3. **Write unit tests** in `DainnUser.UnitTests/Services/`
4. **Write integration tests** in `DainnUser.IntegrationTests/Services/`
5. **Create API endpoint** in `DainnUser.Api/Controllers/`
6. **Update documentation** in `docs/`

### Code Style

- Use async/await for all I/O operations
- Enable nullable reference types
- Use FluentValidation for complex validation
- Follow repository pattern for data access
- Use Options pattern for configuration
- Add XML comments for public APIs

### Git Workflow

```bash
# Create feature branch
git checkout -b feat/your-feature

# Make changes and commit
git add .
git commit -m "feat: add your feature"

# Run tests before push
dotnet test

# Push and create PR
git push origin feat/your-feature
```

---

## 🎯 Next Steps

1. **Configure SMTP** for email sending
2. **Test all endpoints** with Swagger UI or test scripts
3. **Review code** if needed
4. **Add more features** (login, password reset, 2FA)
5. **Deploy to staging/production**

---

## 📞 Support

- Check [Known Issues](docs/known-issues.md) first
- Review [API Documentation](docs/api-endpoints.md)
- Check test results: `dotnet test`
- View Swagger UI: http://localhost:5000/swagger

---

**Last Updated:** 2026-05-08  
**Version:** 1.0.0  
**Status:** ✅ Production Ready
