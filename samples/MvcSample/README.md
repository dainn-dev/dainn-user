# MvcSample

Sample ASP.NET Core MVC application with Razor views demonstrating DainnUser integration.

## Features Demonstrated

- User registration and login with cookie authentication
- Profile management with Razor views
- Password reset flow
- Bootstrap 5 UI
- Form validation

## Prerequisites

- .NET 8.0 SDK
- SQLite (included)

## Getting Started

1. **Navigate to the sample directory:**
   ```bash
   cd samples/MvcSample
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Open in browser:**
   Navigate to `https://localhost:5001` (or the URL shown in console)

## Configuration

The sample uses SQLite for simplicity. Configuration is in `appsettings.json`:

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SQLite",
      "ConnectionString": "Data Source=dainnuser.db"
    },
    "Jwt": {
      "Secret": "your-secret-key-must-be-at-least-32-characters-long-for-security",
      "Issuer": "MvcSample",
      "Audience": "MvcSample",
      "ExpirationMinutes": 60
    },
    "Email": {
      "Provider": "Smtp",
      "FromEmail": "noreply@example.com",
      "SmtpHost": "smtp.gmail.com",
      "SmtpPort": 587,
      "SmtpUsername": "your-email@gmail.com",
      "SmtpPassword": "your-app-password"
    }
  }
}
```

**Important:** Change the JWT secret and email settings before deploying to production.

## Pages

### Home
- Landing page with feature overview
- Shows login status

### Account/Login
- Email and password login
- Remember me option
- Links to register and forgot password

### Account/Register
- User registration form
- Username, email, password fields
- Optional first/last name

### Account/Profile
- View and edit profile information
- Update first name and last name
- Shows account creation date

### Account/ForgotPassword
- Request password reset email
- Safe against user enumeration

## Authentication Flow

1. User registers or logs in
2. Cookie authentication stores user session
3. Protected pages require `[Authorize]` attribute
4. Logout clears cookie and DainnUser session

## Database

The SQLite database (`dainnuser.db`) is created automatically on first run. Migrations are applied automatically via `db.Database.MigrateAsync()` in `Program.cs`.

To reset the database, simply delete `dainnuser.db` and restart the application.

## UI Framework

- Bootstrap 5.3 (CDN)
- Responsive design
- Form validation with jQuery Validation

## Project Structure

```
MvcSample/
├── Controllers/
│   ├── HomeController.cs
│   └── AccountController.cs
├── Views/
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   ├── Profile.cshtml
│   │   └── ForgotPassword.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _ValidationScriptsPartial.cshtml
├── Program.cs
└── appsettings.json
```

## Next Steps

- Explore other DainnUser features (2FA, social login, session management)
- Check `appsettings.json` for feature toggles
- Review the [main documentation](../../docs/README.md)
- See [WebApiSample](../WebApiSample) for API integration

## License

MIT
