# WebApiSample

Sample ASP.NET Core Web API demonstrating DainnUser integration.

## Features Demonstrated

- User registration and login
- JWT authentication
- Password reset flow
- Profile management
- Token refresh
- Swagger UI for API testing

## Prerequisites

- .NET 8.0 SDK
- SQLite (included)

## Getting Started

1. **Navigate to the sample directory:**
   ```bash
   cd samples/WebApiSample
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Open Swagger UI:**
   Navigate to `https://localhost:5001/swagger` (or the URL shown in console)

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
      "Issuer": "WebApiSample",
      "Audience": "WebApiSample",
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

## API Endpoints

### Authentication

- `POST /api/authsample/register` - Register new user
- `POST /api/authsample/login` - Login with credentials
- `POST /api/authsample/refresh` - Refresh access token
- `POST /api/authsample/logout` - Logout (requires auth)

### Password Management

- `POST /api/authsample/forgot-password` - Request password reset
- `POST /api/authsample/reset-password` - Reset password with token
- `POST /api/authsample/change-password` - Change password (requires auth)

### Profile

- `GET /api/authsample/profile` - Get current user profile (requires auth)

## Testing with Swagger

1. **Register a user:**
   - Use `POST /api/authsample/register`
   - Provide username, email, password

2. **Login:**
   - Use `POST /api/authsample/login`
   - Copy the `accessToken` from response

3. **Authorize:**
   - Click "Authorize" button in Swagger UI
   - Enter: `Bearer {your-access-token}`
   - Click "Authorize"

4. **Access protected endpoints:**
   - Now you can call `/profile`, `/logout`, `/change-password`

## Example Requests

### Register
```json
POST /api/authsample/register
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "SecurePass123!",
  "firstName": "Test",
  "lastName": "User"
}
```

### Login
```json
POST /api/authsample/login
{
  "usernameOrEmail": "testuser",
  "password": "SecurePass123!"
}
```

### Response
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresAt": "2024-01-01T12:00:00Z",
  "user": {
    "id": "...",
    "username": "testuser",
    "email": "test@example.com"
  }
}
```

## Database

The SQLite database (`dainnuser.db`) is created automatically on first run. Migrations are applied automatically via `app.UseDainnUserMigrations()`.

To reset the database, simply delete `dainnuser.db` and restart the application.

## Next Steps

- Explore other DainnUser features (2FA, social login, session management)
- Check `appsettings.json` for feature toggles
- Review the [main documentation](../../docs/README.md)
- See [MvcSample](../MvcSample) for a web UI example

## License

MIT
