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

## Profile Endpoints

All profile endpoints require authentication via Bearer token.

### 4. Get Profile

Gets the authenticated user's profile information.

**Endpoint:** `GET /profile`

**Authentication:** Required (Bearer token)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe",
    "displayName": "John D.",
    "avatarUrl": "https://storage.example.com/avatars/user123.jpg",
    "dateOfBirth": "1990-05-15",
    "gender": "Male",
    "language": "en",
    "timezone": "America/New_York",
    "bio": "Software developer and tech enthusiast.",
    "website": "https://johndoe.com",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-03-20T14:45:00Z"
  },
  "message": null,
  "errors": null
}
```

**Error Response (401 Unauthorized):**

```json
{
  "success": false,
  "data": null,
  "message": "Invalid token.",
  "errors": null
}
```

**Error Response (404 Not Found):**

```json
{
  "success": false,
  "data": null,
  "message": "User not found.",
  "errors": null
}
```

---

### 5. Update Profile

Updates the authenticated user's profile information.

**Endpoint:** `PUT /profile`

**Authentication:** Required (Bearer token)

**Request Body:**

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "displayName": "John D.",
  "dateOfBirth": "1990-05-15",
  "gender": "Male",
  "language": "en",
  "timezone": "America/New_York",
  "bio": "Software developer and tech enthusiast.",
  "website": "https://johndoe.com"
}
```

**Validation Rules:**
- FirstName: Optional, max 100 characters
- LastName: Optional, max 100 characters
- DisplayName: Optional, max 100 characters
- DateOfBirth: Optional, must be valid date, cannot be in future
- Gender: Optional, max 50 characters
- Language: Optional, 2-10 characters (language code)
- Timezone: Optional, valid IANA timezone (e.g., "Asia/Ho_Chi_Minh")
- Bio: Optional, max 500 characters
- Website: Optional, valid URL format

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe",
    "displayName": "John D.",
    "avatarUrl": null,
    "dateOfBirth": "1990-05-15",
    "gender": "Male",
    "language": "en",
    "timezone": "America/New_York",
    "bio": "Software developer and tech enthusiast.",
    "website": "https://johndoe.com",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-03-20T15:00:00Z"
  },
  "message": "Profile updated successfully.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed.",
  "errors": [
    "Website must be a valid URL.",
    "Date of birth cannot be in the future."
  ]
}
```

---

### 6. Upload Avatar

Uploads or updates the authenticated user's avatar image.

**Endpoint:** `POST /profile/avatar`

**Authentication:** Required (Bearer token)

**Request:** `multipart/form-data`
- Field name: `file`
- Supported formats: JPEG, PNG, GIF, WebP
- Max file size: 5MB
- Recommended size: 256x256 pixels

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe",
    "displayName": "John D.",
    "avatarUrl": "https://storage.example.com/avatars/abc123.jpg",
    "dateOfBirth": null,
    "gender": null,
    "language": "en",
    "timezone": "America/New_York",
    "bio": null,
    "website": null,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-03-20T16:00:00Z"
  },
  "message": "Avatar uploaded successfully.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed.",
  "errors": null
}
```

---

### 7. Delete Avatar

Deletes the authenticated user's avatar image.

**Endpoint:** `DELETE /profile/avatar`

**Authentication:** Required (Bearer token)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe",
    "displayName": "John D.",
    "avatarUrl": null,
    "dateOfBirth": null,
    "gender": null,
    "language": "en",
    "timezone": "America/New_York",
    "bio": null,
    "website": null,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-03-20T16:30:00Z"
  },
  "message": "Avatar deleted successfully.",
  "errors": null
}
```

---

### 8. Update Profile Settings

Updates the authenticated user's profile settings (language, timezone).

**Endpoint:** `PUT /profile/settings`

**Authentication:** Required (Bearer token)

**Request Body:**

```json
{
  "language": "vi",
  "timezone": "Asia/Ho_Chi_Minh"
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe",
    "displayName": "John D.",
    "avatarUrl": null,
    "dateOfBirth": null,
    "gender": null,
    "language": "vi",
    "timezone": "Asia/Ho_Chi_Minh",
    "bio": null,
    "website": null,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-03-20T17:00:00Z"
  },
  "message": "Profile updated successfully.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed.",
  "errors": [
    "Timezone must be a valid IANA timezone identifier."
  ]
}
```

---

## User Management Endpoints

All user management endpoints require authentication and Administrator role.

### 9. List Users

Gets a paginated list of all users with optional filtering.

**Endpoint:** `GET /user`

**Authentication:** Required (Bearer token, Administrator role)

**Query Parameters:**
- `pageNumber` (optional): Page number, default 1
- `pageSize` (optional): Items per page, default 20, max 100
- `search` (optional): Search by email or username
- `status` (optional): Filter by user status (`Pending`, `Active`, `Suspended`, `Locked`)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "email": "user@example.com",
        "username": "johndoe",
        "status": "Active",
        "emailVerified": true,
        "twoFactorEnabled": false,
        "lastLoginAt": "2024-03-20T10:00:00Z",
        "createdAt": "2024-01-15T10:30:00Z",
        "updatedAt": "2024-03-20T14:45:00Z",
        "roles": ["User"]
      },
      {
        "id": "7c963f66afa63fa85f64-5717-4562-b3fc",
        "email": "admin@example.com",
        "username": "admin",
        "status": "Active",
        "emailVerified": true,
        "twoFactorEnabled": true,
        "lastLoginAt": "2024-03-20T09:00:00Z",
        "createdAt": "2024-01-01T08:00:00Z",
        "updatedAt": "2024-03-19T16:30:00Z",
        "roles": ["Administrator"]
      }
    ],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 8
  },
  "message": null,
  "errors": null
}
```

**Error Response (401 Unauthorized):**

```json
{
  "success": false,
  "data": null,
  "message": "Unauthorized",
  "errors": null
}
```

**Error Response (403 Forbidden):**

```json
{
  "success": false,
  "data": null,
  "message": "Forbidden",
  "errors": null
}
```

---

### 10. Get User

Gets a specific user by ID.

**Endpoint:** `GET /user/{id}`

**Authentication:** Required (Bearer token, Administrator role)

**Path Parameters:**
- `id` (required): User ID (GUID format)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "username": "johndoe",
    "status": "Active",
    "emailVerified": true,
    "twoFactorEnabled": false,
    "lastLoginAt": "2024-03-20T10:00:00Z",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-03-20T14:45:00Z",
    "roles": ["User"]
  },
  "message": null,
  "errors": null
}
```

**Error Response (404 Not Found):**

```json
{
  "success": false,
  "data": null,
  "message": "User not found.",
  "errors": null
}
```

---

### 11. Update User

Updates a specific user's information.

**Endpoint:** `PUT /user/{id}`

**Authentication:** Required (Bearer token, Administrator role)

**Path Parameters:**
- `id` (required): User ID (GUID format)

**Request Body:**

```json
{
  "email": "newemail@example.com",
  "username": "newhandle",
  "status": "Active"
}
```

**Validation Rules:**
- Email: Optional, valid email format, max 256 characters
- Username: Optional, 3-50 characters, alphanumeric with underscores/hyphens only
- Status: Optional, must be one of `Pending`, `Active`, `Suspended`, `Locked`

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "newemail@example.com",
    "username": "newhandle",
    "status": "Active",
    "emailVerified": true,
    "twoFactorEnabled": false,
    "lastLoginAt": "2024-03-20T10:00:00Z",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-03-20T18:00:00Z",
    "roles": ["User"]
  },
  "message": "User updated successfully.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Email is already in use.",
  "errors": null
}
```

**Error Response (404 Not Found):**

```json
{
  "success": false,
  "data": null,
  "message": "User not found.",
  "errors": null
}
```

---

### 12. Delete User

Deletes a specific user.

**Endpoint:** `DELETE /user/{id}`

**Authentication:** Required (Bearer token, Administrator role)

**Path Parameters:**
- `id` (required): User ID (GUID format)

**Business Rules:**
- Cannot delete your own account (returns 400 Bad Request)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": null,
  "message": "User deleted successfully.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Cannot delete your own account.",
  "errors": null
}
```

**Error Response (404 Not Found):**

```json
{
  "success": false,
  "data": null,
  "message": "User not found.",
  "errors": null
}
```

---

### 13. Lock User

Locks a user account, preventing login.

**Endpoint:** `POST /user/{id}/lock`

**Authentication:** Required (Bearer token, Administrator role)

**Path Parameters:**
- `id` (required): User ID (GUID format)

**Business Rules:**
- Cannot lock your own account (returns 400 Bad Request)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": null,
  "message": "User locked successfully.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Cannot lock your own account.",
  "errors": null
}
```

**Error Response (404 Not Found):**

```json
{
  "success": false,
  "data": null,
  "message": "User not found.",
  "errors": null
}
```

---

### 14. Unlock User

Unlocks a previously locked user account.

**Endpoint:** `POST /user/{id}/unlock`

**Authentication:** Required (Bearer token, Administrator role)

**Path Parameters:**
- `id` (required): User ID (GUID format)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": null,
  "message": "User unlocked successfully.",
  "errors": null
}
```

**Error Response (404 Not Found):**

```json
{
  "success": false,
  "data": null,
  "message": "User not found.",
  "errors": null
}
```

---

### 15. Add Role to User

Adds a role to a user.

**Endpoint:** `POST /user/{id}/roles`

**Authentication:** Required (Bearer token, Administrator role)

**Path Parameters:**
- `id` (required): User ID (GUID format)

**Request Body:**

```json
{
  "roleId": "8fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": null,
  "message": "Role added successfully.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "User already has this role.",
  "errors": null
}
```

**Error Response (404 Not Found):**

```json
{
  "success": false,
  "data": null,
  "message": "User not found.",
  "errors": null
}
```

---

### 16. Remove Role from User

Removes a role from a user.

**Endpoint:** `DELETE /user/{id}/roles/{roleId}`

**Authentication:** Required (Bearer token, Administrator role)

**Path Parameters:**
- `id` (required): User ID (GUID format)
- `roleId` (required): Role ID (GUID format)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": null,
  "message": "Role removed successfully.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Cannot remove the last administrator role.",
  "errors": null
}
```

---

### 4. Login

Authenticates user and returns JWT tokens.

**Endpoint:** `POST /auth/login`

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "SecurePass123!@#",
  "rememberDeviceToken": null
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123def456...",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "user@example.com",
      "username": "johndoe",
      "emailVerified": true,
      "twoFactorEnabled": false
    },
    "requiresTwoFactor": false
  },
  "message": null,
  "errors": null
}
```

**Error Response (401 Unauthorized):**

```json
{
  "success": false,
  "data": null,
  "message": "Invalid email or password.",
  "errors": null
}
```

**Error Response (403 Forbidden - Email Not Verified):**

```json
{
  "success": false,
  "data": null,
  "message": "Email not verified. Please check your email.",
  "errors": null
}
```

**Error Response (423 Locked):**

```json
{
  "success": false,
  "data": null,
  "message": "Account is locked due to too many failed login attempts.",
  "errors": null
}
```

**Authorization:** None (public endpoint)

---

### 5. Logout

Ends current session and revokes refresh token.

**Endpoint:** `POST /auth/logout`

**Authorization:** Required (Bearer token)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": "Logged out.",
  "message": "Session ended.",
  "errors": null
}
```

---

### 6. Refresh Token

Refreshes access token using refresh token. Rotates refresh token.

**Endpoint:** `POST /auth/refresh-token`

**Request Body:**

```json
{
  "refreshToken": "abc123def456..."
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "new-refresh-token...",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "user@example.com",
      "username": "johndoe",
      "emailVerified": true,
      "twoFactorEnabled": false
    },
    "requiresTwoFactor": false
  },
  "message": null,
  "errors": null
}
```

**Error Response (401 Unauthorized):**

```json
{
  "success": false,
  "data": null,
  "message": "Invalid refresh token.",
  "errors": null
}
```

**Authorization:** None (public endpoint)

---

### 7. Forgot Password

Initiates password reset. Always returns 200 to prevent user enumeration.

**Endpoint:** `POST /auth/forgot-password`

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
  "data": "If an account with that email exists, a password reset link has been sent.",
  "message": null,
  "errors": null
}
```

**Authorization:** None (public endpoint)

---

### 8. Reset Password

Completes password reset using token from email. Invalidates all sessions.

**Endpoint:** `POST /auth/reset-password`

**Request Body:**

```json
{
  "token": "reset-token-from-email",
  "newPassword": "NewSecurePass123!@#",
  "confirmPassword": "NewSecurePass123!@#"
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": "Password reset successfully.",
  "message": "All active sessions have been invalidated. Please log in with your new password.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Invalid or expired password reset token.",
  "errors": null
}
```

**Authorization:** None (public endpoint)

---

### 9. Change Password

Changes password for authenticated user. Invalidates all other sessions.

**Endpoint:** `POST /auth/change-password`

**Authorization:** Required (Bearer token)

**Request Body:**

```json
{
  "currentPassword": "OldPass123!@#",
  "newPassword": "NewPass123!@#",
  "confirmPassword": "NewPass123!@#"
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": "Password changed successfully.",
  "message": "All other active sessions have been invalidated.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Current password is incorrect.",
  "errors": null
}
```

---

### 10. Setup Two-Factor Authentication

Initiates 2FA setup. Returns TOTP secret and QR code URI.

**Endpoint:** `POST /auth/2fa/setup`

**Authorization:** Required (Bearer token)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "secret": "JBSWY3DPEHPK3PXP",
    "qrCodeUri": "otpauth://totp/DainnUser:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=DainnUser"
  },
  "message": "Scan the QR code with your authenticator app and confirm with a code.",
  "errors": null
}
```

---

### 11. Enable Two-Factor Authentication

Confirms and activates 2FA. Returns backup codes.

**Endpoint:** `POST /auth/2fa/enable`

**Authorization:** Required (Bearer token)

**Request Body:**

```json
{
  "code": "123456"
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "backupCodes": [
      "12345678",
      "23456789",
      "34567890",
      "45678901",
      "56789012",
      "67890123",
      "78901234",
      "89012345",
      "90123456",
      "01234567"
    ]
  },
  "message": "Two-factor authentication enabled. Store these backup codes securely — they will not be shown again.",
  "errors": null
}
```

**Error Response (400 Bad Request):**

```json
{
  "success": false,
  "data": null,
  "message": "Invalid or expired two-factor code.",
  "errors": null
}
```

---

### 12. Disable Two-Factor Authentication

Disables 2FA. Requires valid TOTP or backup code.

**Endpoint:** `POST /auth/2fa/disable`

**Authorization:** Required (Bearer token)

**Request Body:**

```json
{
  "code": "123456"
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": "Two-factor authentication disabled.",
  "message": null,
  "errors": null
}
```

---

### 13. Complete Two-Factor Login

Completes login requiring 2FA. Verifies TOTP or backup code.

**Endpoint:** `POST /auth/2fa/login`

**Authorization:** None (public endpoint)

**Request Body:**

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "123456",
  "rememberDevice": false
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123def456...",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "user@example.com",
      "username": "johndoe",
      "emailVerified": true,
      "twoFactorEnabled": true
    },
    "requiresTwoFactor": false
  },
  "message": null,
  "errors": null
}
```

---

### 14. Regenerate Backup Codes

Regenerates 2FA backup codes. Requires valid TOTP code (not backup code).

**Endpoint:** `POST /auth/2fa/backup-codes/regenerate`

**Authorization:** Required (Bearer token)

**Request Body:**

```json
{
  "code": "123456"
}
```

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "backupCodes": [
      "11111111",
      "22222222",
      "33333333",
      "44444444",
      "55555555",
      "66666666",
      "77777777",
      "88888888",
      "99999999",
      "00000000"
    ]
  },
  "message": "Backup codes regenerated. All previous codes are now invalid.",
  "errors": null
}
```

---

### 15. Unlock Account (Admin)

Manually unlocks locked user account. Admin only.

**Endpoint:** `POST /auth/admin/unlock-account/{userId}`

**Authorization:** Required (Bearer token, Administrator role)

**Path Parameters:**
- `userId` (required): User ID (GUID format)

**Success Response (200 OK):**

```json
{
  "success": true,
  "data": "Account unlocked.",
  "message": "Failed login counters reset and lockout cleared.",
  "errors": null
}
```

**Error Response (404 Not Found):**

```json
{
  "success": false,
  "data": null,
  "message": "User not found.",
  "errors": null
}
```

---

## Profile Endpoints

All profile endpoints require authentication via Bearer token.

---

## Testing with cURL

### Authentication Endpoints

#### Register a User

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

#### Login

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#"
  }'
```

#### Logout

```bash
curl -X POST https://localhost:5001/api/auth/logout \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Refresh Token

```bash
curl -X POST https://localhost:5001/api/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token"
  }'
```

#### Verify Email

```bash
curl -X POST https://localhost:5001/api/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "token": "your-verification-token-here"
  }'
```

#### Resend Verification

```bash
curl -X POST https://localhost:5001/api/auth/resend-verification \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com"
  }'
```

#### Forgot Password

```bash
curl -X POST https://localhost:5001/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com"
  }'
```

#### Reset Password

```bash
curl -X POST https://localhost:5001/api/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "token": "reset-token-from-email",
    "newPassword": "NewPass123!@#",
    "confirmPassword": "NewPass123!@#"
  }'
```

#### Change Password

```bash
curl -X POST https://localhost:5001/api/auth/change-password \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "OldPass123!@#",
    "newPassword": "NewPass123!@#",
    "confirmPassword": "NewPass123!@#"
  }'
```

#### Setup 2FA

```bash
curl -X POST https://localhost:5001/api/auth/2fa/setup \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Enable 2FA

```bash
curl -X POST https://localhost:5001/api/auth/2fa/enable \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "123456"
  }'
```

#### Disable 2FA

```bash
curl -X POST https://localhost:5001/api/auth/2fa/disable \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "123456"
  }'
```

#### Complete 2FA Login

```bash
curl -X POST https://localhost:5001/api/auth/2fa/login \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "123456",
    "rememberDevice": false
  }'
```

#### Regenerate Backup Codes

```bash
curl -X POST https://localhost:5001/api/auth/2fa/backup-codes/regenerate \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "123456"
  }'
```

#### Unlock Account (Admin)

```bash
curl -X POST https://localhost:5001/api/auth/admin/unlock-account/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

### Profile Endpoints

#### Get Profile

```bash
curl -X GET https://localhost:5001/api/profile \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Update Profile

```bash
curl -X PUT https://localhost:5001/api/profile \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "displayName": "John D.",
    "language": "en",
    "timezone": "America/New_York",
    "bio": "Software developer"
  }'
```

#### Upload Avatar

```bash
curl -X POST https://localhost:5001/api/profile/avatar \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/avatar.jpg"
```

#### Delete Avatar

```bash
curl -X DELETE https://localhost:5001/api/profile/avatar \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Update Settings

```bash
curl -X PUT https://localhost:5001/api/profile/settings \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "language": "vi",
    "timezone": "Asia/Ho_Chi_Minh"
  }'
```

### User Management Endpoints

#### List Users

```bash
curl -X GET "https://localhost:5001/api/user?pageNumber=1&pageSize=20&search=john&status=Active" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

#### Get User

```bash
curl -X GET https://localhost:5001/api/user/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

#### Update User

```bash
curl -X PUT https://localhost:5001/api/user/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newemail@example.com",
    "username": "newhandle",
    "status": "Active"
  }'
```

#### Delete User

```bash
curl -X DELETE https://localhost:5001/api/user/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

#### Lock User

```bash
curl -X POST https://localhost:5001/api/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/lock \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

#### Unlock User

```bash
curl -X POST https://localhost:5001/api/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/unlock \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

#### Add Role to User

```bash
curl -X POST https://localhost:5001/api/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/roles \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "roleId": "8fa85f64-5717-4562-b3fc-2c963f66afa6"
  }'
```

#### Remove Role from User

```bash
curl -X DELETE https://localhost:5001/api/user/3fa85f64-5717-4562-b3fc-2c963f66afa6/roles/8fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

---

## Testing with C#

### Authentication Endpoints

#### Register

```csharp
var registerDto = new RegisterDto
{
    Email = "test@example.com",
    Username = "testuser",
    Password = "Test123!@#",
    ConfirmPassword = "Test123!@#"
};

var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/register", registerDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
```

#### Login

```csharp
var loginDto = new LoginDto
{
    Email = "test@example.com",
    Password = "Test123!@#"
};

var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/login", loginDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

if (result.Success)
{
    var accessToken = result.Data.AccessToken;
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}
```

#### Logout

```csharp
var response = await client.PostAsync("https://localhost:5001/api/auth/logout", null);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
```

#### Refresh Token

```csharp
var refreshDto = new RefreshTokenDto
{
    RefreshToken = "your-refresh-token"
};

var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/refresh-token", refreshDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
```

#### Forgot Password

```csharp
var forgotPasswordDto = new ForgotPasswordDto
{
    Email = "test@example.com"
};

var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/forgot-password", forgotPasswordDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
```

#### Reset Password

```csharp
var resetPasswordDto = new ResetPasswordDto
{
    Token = "reset-token-from-email",
    NewPassword = "NewPass123!@#",
    ConfirmPassword = "NewPass123!@#"
};

var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/reset-password", resetPasswordDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
```

#### Change Password

```csharp
var changePasswordDto = new ChangePasswordDto
{
    CurrentPassword = "OldPass123!@#",
    NewPassword = "NewPass123!@#",
    ConfirmPassword = "NewPass123!@#"
};

var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/change-password", changePasswordDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
```

#### Setup 2FA

```csharp
var response = await client.PostAsync("https://localhost:5001/api/auth/2fa/setup", null);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<TwoFactorSetupResponse>>();

Console.WriteLine($"Secret: {result.Data.Secret}");
Console.WriteLine($"QR Code URI: {result.Data.QrCodeUri}");
```

#### Enable 2FA

```csharp
var twoFactorCodeDto = new TwoFactorCodeDto
{
    Code = "123456"
};

var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/2fa/enable", twoFactorCodeDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<BackupCodesResponse>>();

foreach (var code in result.Data.BackupCodes)
{
    Console.WriteLine($"Backup code: {code}");
}
```

#### Complete 2FA Login

```csharp
var completeTwoFactorDto = new CompleteTwoFactorLoginDto
{
    UserId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    Code = "123456",
    RememberDevice = false
};

var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/2fa/login", completeTwoFactorDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
```

### Profile Endpoints

#### Get Profile

```csharp
using System.Net.Http.Headers;

var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", "YOUR_JWT_TOKEN");

var response = await client.GetAsync("https://localhost:5001/api/profile");
var profile = await response.Content.ReadFromJsonAsync<ApiResponse<ProfileResponse>>();
```

#### Update Profile

```csharp
var updateDto = new UpdateProfileDto
{
    FirstName = "John",
    LastName = "Doe",
    DisplayName = "John D.",
    Language = "en",
    Timezone = "America/New_York",
    Bio = "Software developer"
};

var response = await client.PutAsJsonAsync("https://localhost:5001/api/profile", updateDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProfileResponse>>();
```

#### Upload Avatar

```csharp
using var form = new MultipartFormDataContent();
using var fileStream = File.OpenRead("avatar.jpg");
using var streamContent = new StreamContent(fileStream);
streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
form.Add(streamContent, "file", "avatar.jpg");

var response = await client.PostAsync("https://localhost:5001/api/profile/avatar", form);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProfileResponse>>();
```

### User Management Endpoints

#### List Users

```csharp
var response = await client.GetAsync(
    "https://localhost:5001/api/user?pageNumber=1&pageSize=20&search=john&status=Active");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>();

foreach (var user in result.Data.Items)
{
    Console.WriteLine($"{user.Username} - {user.Email}");
}
```

#### Update User

```csharp
var updateDto = new UpdateUserDto
{
    Email = "newemail@example.com",
    Username = "newhandle",
    Status = UserStatus.Active
};

var userId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
var response = await client.PutAsJsonAsync($"https://localhost:5001/api/user/{userId}", updateDto);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
```

#### Lock User

```csharp
var userId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
var response = await client.PostAsync($"https://localhost:5001/api/user/{userId}/lock", null);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
```

#### Add Role to User

```csharp
var userId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
var roleId = Guid.Parse("8fa85f64-5717-4562-b3fc-2c963f66afa6");

var request = new AddRoleRequest { RoleId = roleId };
var response = await client.PostAsJsonAsync($"https://localhost:5001/api/user/{userId}/roles", request);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
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
