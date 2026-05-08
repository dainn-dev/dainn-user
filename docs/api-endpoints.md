# API Endpoints Documentation

Base URL: `https://localhost:5001/api` (Development)

## Authentication Endpoints

### 1. Register User

Creates a new user account and sends a verification email.

**Endpoint:** `POST /auth/register`

**Request Body:**

```json
{
  "email": "user@example.com",
  "username": "johndoe",
  "password": "SecurePass123!@#",
  "confirmPassword": "SecurePass123!@#"
}
```

**Validation Rules:**
- Email: Required, valid email format, max 256 characters
- Username: Required, 3-50 characters, alphanumeric with underscores/hyphens only
- Password: Required, min 8 characters, must contain uppercase, lowercase, digit, and special character
- ConfirmPassword: Must match password

**Success Response (201 Created):**

```json
{
  "success": true,
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "message": "Registration successful. Please check your email to verify your account."
  },
  "message": null,
  "errors": null
}
```

**Error Responses:**

**400 Bad Request** (Validation Failed):
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed.",
  "errors": [
    "Email is required.",
    "Password must be at least 8 characters."
  ]
}
```

**409 Conflict** (Duplicate Email/Username):
```json
{
  "success": false,
  "data": null,
  "message": "Email is already registered.",
  "errors": null
}
```

---

### 2. Verify Email

Verifies a user's email address using the token sent via email.

**Endpoint:** `POST /auth/verify-email`

**Request Body:**

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "token": "AbCdEfGhIjKlMnOpQrStUvWxYz0123456789+/=="
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": "Email verified successfully.",
  "message": "Your account is now active.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Email verification failed. Token may be invalid, expired, or already used.",
  "errors": null
}
```

**Token Expiration:** Verification tokens expire after 24 hours.

---

### 3. Resend Verification Email

Resends the verification email to a user who hasn't verified their email yet.

**Endpoint:** `POST /auth/resend-verification`

**Request Body:**

```json
{
  "email": "user@example.com"
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": "Verification email sent.",
  "message": "Please check your email for the verification link.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Failed to resend verification email. User may not exist or email is already verified.",
  "errors": null
}
```

**Notes:**
- Old verification tokens are automatically revoked when a new one is generated
- Only works for unverified accounts
- Rate limiting recommended in production

---

## Testing with cURL

### Register a User

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "username": "testuser",
    "password": "Test123!@#",
    "confirmPassword": "Test123!@#"
  }'
```

### Verify Email

```bash
curl -X POST https://localhost:5001/api/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "token": "your-verification-token-here"
  }'
```

### Resend Verification

```bash
curl -X POST https://localhost:5001/api/auth/resend-verification \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com"
  }'
```

---

## Testing with Swagger UI

1. Start the API: `dotnet run` in `src/DainnUser.Api`
2. Open browser: https://localhost:5001/swagger
3. Expand an endpoint
4. Click "Try it out"
5. Fill in the request body
6. Click "Execute"
7. View the response

---

## Error Handling

All endpoints follow a consistent error response format:

```json
{
  "success": false,
  "data": null,
  "message": "Error description",
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

**HTTP Status Codes:**
- `200 OK` - Request successful
- `201 Created` - Resource created successfully
- `400 Bad Request` - Validation error or invalid request
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict (e.g., duplicate email)
- `500 Internal Server Error` - Unexpected server error

---

## Security Considerations

1. **HTTPS Only**: Always use HTTPS in production
2. **Rate Limiting**: Implement rate limiting for registration and resend endpoints
3. **CORS**: Configure CORS policy appropriately for your frontend
4. **Password Storage**: Passwords are hashed using ASP.NET Core Identity's PasswordHasher (PBKDF2)
5. **Token Security**: Verification tokens are cryptographically secure (32-byte random)
6. **Email Verification**: Users cannot login until email is verified (Status = Pending)

---

## Email Templates

The system sends HTML emails for:

1. **Email Verification**: Contains verification token, expires in 24 hours
2. **Password Reset**: Contains reset token, expires in 1 hour (coming soon)
3. **Two-Factor Authentication**: Contains 6-digit code, expires in 5 minutes (coming soon)

All emails are sent asynchronously and include proper error handling.
