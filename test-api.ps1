# DainnUser API Test Script (PowerShell)
# Tests all authentication endpoints

$BaseUrl = "http://localhost:5000/api/auth"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "DainnUser API Test Suite" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Register User
Write-Host "Test 1: Register User" -ForegroundColor Yellow
Write-Host "----------------------"
$registerBody = @{
    email = "test@example.com"
    username = "testuser"
    password = "Test123!@#"
    confirmPassword = "Test123!@#"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$BaseUrl/register" -Method Post -Body $registerBody -ContentType "application/json"
    $registerResponse | ConvertTo-Json -Depth 10

    if ($registerResponse.success) {
        Write-Host "✓ Registration successful" -ForegroundColor Green
        $userId = $registerResponse.data.userId
        Write-Host "User ID: $userId"
    } else {
        Write-Host "✗ Registration failed" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Registration request failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Test 2: Register with Duplicate Email
Write-Host "Test 2: Register with Duplicate Email (should fail)" -ForegroundColor Yellow
Write-Host "----------------------------------------------------"
$duplicateBody = @{
    email = "test@example.com"
    username = "testuser2"
    password = "Test123!@#"
    confirmPassword = "Test123!@#"
} | ConvertTo-Json

try {
    $duplicateResponse = Invoke-RestMethod -Uri "$BaseUrl/register" -Method Post -Body $duplicateBody -ContentType "application/json"
    $duplicateResponse | ConvertTo-Json -Depth 10
    Write-Host "✗ Duplicate email should have been rejected" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    $errorResponse | ConvertTo-Json -Depth 10
    if (-not $errorResponse.success) {
        Write-Host "✓ Duplicate email correctly rejected" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Test 3: Register with Invalid Email
Write-Host "Test 3: Register with Invalid Email (should fail)" -ForegroundColor Yellow
Write-Host "--------------------------------------------------"
$invalidEmailBody = @{
    email = "invalid-email"
    username = "testuser3"
    password = "Test123!@#"
    confirmPassword = "Test123!@#"
} | ConvertTo-Json

try {
    $invalidEmailResponse = Invoke-RestMethod -Uri "$BaseUrl/register" -Method Post -Body $invalidEmailBody -ContentType "application/json"
    $invalidEmailResponse | ConvertTo-Json -Depth 10
    Write-Host "✗ Invalid email should have been rejected" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    $errorResponse | ConvertTo-Json -Depth 10
    if (-not $errorResponse.success) {
        Write-Host "✓ Invalid email correctly rejected" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Test 4: Register with Weak Password
Write-Host "Test 4: Register with Weak Password (should fail)" -ForegroundColor Yellow
Write-Host "--------------------------------------------------"
$weakPasswordBody = @{
    email = "test4@example.com"
    username = "testuser4"
    password = "weak"
    confirmPassword = "weak"
} | ConvertTo-Json

try {
    $weakPasswordResponse = Invoke-RestMethod -Uri "$BaseUrl/register" -Method Post -Body $weakPasswordBody -ContentType "application/json"
    $weakPasswordResponse | ConvertTo-Json -Depth 10
    Write-Host "✗ Weak password should have been rejected" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    $errorResponse | ConvertTo-Json -Depth 10
    if (-not $errorResponse.success) {
        Write-Host "✓ Weak password correctly rejected" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Test 5: Verify Email with Invalid Token
Write-Host "Test 5: Verify Email with Invalid Token (should fail)" -ForegroundColor Yellow
Write-Host "------------------------------------------------------"
$verifyBody = @{
    userId = $userId
    token = "invalid-token-12345"
} | ConvertTo-Json

try {
    $verifyResponse = Invoke-RestMethod -Uri "$BaseUrl/verify-email" -Method Post -Body $verifyBody -ContentType "application/json"
    $verifyResponse | ConvertTo-Json -Depth 10
    Write-Host "✗ Invalid token should have been rejected" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    $errorResponse | ConvertTo-Json -Depth 10
    if (-not $errorResponse.success) {
        Write-Host "✓ Invalid token correctly rejected" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Test 6: Resend Verification Email
Write-Host "Test 6: Resend Verification Email" -ForegroundColor Yellow
Write-Host "----------------------------------"
$resendBody = @{
    email = "test@example.com"
} | ConvertTo-Json

try {
    $resendResponse = Invoke-RestMethod -Uri "$BaseUrl/resend-verification" -Method Post -Body $resendBody -ContentType "application/json"
    $resendResponse | ConvertTo-Json -Depth 10

    if ($resendResponse.success) {
        Write-Host "✓ Resend verification successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Resend verification failed" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Resend verification request failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Test 7: Resend Verification for Non-existent Email
Write-Host "Test 7: Resend Verification for Non-existent Email (should fail)" -ForegroundColor Yellow
Write-Host "-----------------------------------------------------------------"
$resendInvalidBody = @{
    email = "nonexistent@example.com"
} | ConvertTo-Json

try {
    $resendInvalidResponse = Invoke-RestMethod -Uri "$BaseUrl/resend-verification" -Method Post -Body $resendInvalidBody -ContentType "application/json"
    $resendInvalidResponse | ConvertTo-Json -Depth 10
    Write-Host "✗ Non-existent email should have been rejected" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    $errorResponse | ConvertTo-Json -Depth 10
    if (-not $errorResponse.success) {
        Write-Host "✓ Non-existent email correctly rejected" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Suite Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Note: To fully test email verification, you need to:" -ForegroundColor Yellow
Write-Host "1. Check the email sent to test@example.com"
Write-Host "2. Extract the verification token"
Write-Host "3. Call POST /api/auth/verify-email with the real token"
Write-Host ""
Write-Host "Database location: E:\Projects\DainnUser\src\DainnUser.Api\dainnuser.db" -ForegroundColor Cyan
Write-Host "Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan
