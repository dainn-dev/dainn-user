#!/bin/bash

# DainnUser API Test Script
# Tests all authentication endpoints

BASE_URL="http://localhost:5000/api/auth"

echo "=================================="
echo "DainnUser API Test Suite"
echo "=================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test 1: Register User
echo "Test 1: Register User"
echo "----------------------"
REGISTER_RESPONSE=$(curl -s -X POST "$BASE_URL/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "username": "testuser",
    "password": "Test123!@#",
    "confirmPassword": "Test123!@#"
  }')

echo "$REGISTER_RESPONSE" | jq '.'

if echo "$REGISTER_RESPONSE" | jq -e '.success == true' > /dev/null; then
    echo -e "${GREEN}✓ Registration successful${NC}"
    USER_ID=$(echo "$REGISTER_RESPONSE" | jq -r '.data.userId')
    echo "User ID: $USER_ID"
else
    echo -e "${RED}✗ Registration failed${NC}"
fi

echo ""
echo "=================================="
echo ""

# Test 2: Register with Duplicate Email
echo "Test 2: Register with Duplicate Email (should fail)"
echo "----------------------------------------------------"
DUPLICATE_RESPONSE=$(curl -s -X POST "$BASE_URL/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "username": "testuser2",
    "password": "Test123!@#",
    "confirmPassword": "Test123!@#"
  }')

echo "$DUPLICATE_RESPONSE" | jq '.'

if echo "$DUPLICATE_RESPONSE" | jq -e '.success == false' > /dev/null; then
    echo -e "${GREEN}✓ Duplicate email correctly rejected${NC}"
else
    echo -e "${RED}✗ Duplicate email should have been rejected${NC}"
fi

echo ""
echo "=================================="
echo ""

# Test 3: Register with Invalid Email
echo "Test 3: Register with Invalid Email (should fail)"
echo "--------------------------------------------------"
INVALID_EMAIL_RESPONSE=$(curl -s -X POST "$BASE_URL/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "invalid-email",
    "username": "testuser3",
    "password": "Test123!@#",
    "confirmPassword": "Test123!@#"
  }')

echo "$INVALID_EMAIL_RESPONSE" | jq '.'

if echo "$INVALID_EMAIL_RESPONSE" | jq -e '.success == false' > /dev/null; then
    echo -e "${GREEN}✓ Invalid email correctly rejected${NC}"
else
    echo -e "${RED}✗ Invalid email should have been rejected${NC}"
fi

echo ""
echo "=================================="
echo ""

# Test 4: Register with Weak Password
echo "Test 4: Register with Weak Password (should fail)"
echo "--------------------------------------------------"
WEAK_PASSWORD_RESPONSE=$(curl -s -X POST "$BASE_URL/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test4@example.com",
    "username": "testuser4",
    "password": "weak",
    "confirmPassword": "weak"
  }')

echo "$WEAK_PASSWORD_RESPONSE" | jq '.'

if echo "$WEAK_PASSWORD_RESPONSE" | jq -e '.success == false' > /dev/null; then
    echo -e "${GREEN}✓ Weak password correctly rejected${NC}"
else
    echo -e "${RED}✗ Weak password should have been rejected${NC}"
fi

echo ""
echo "=================================="
echo ""

# Test 5: Verify Email with Invalid Token
echo "Test 5: Verify Email with Invalid Token (should fail)"
echo "------------------------------------------------------"
VERIFY_RESPONSE=$(curl -s -X POST "$BASE_URL/verify-email" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$USER_ID\",
    \"token\": \"invalid-token-12345\"
  }")

echo "$VERIFY_RESPONSE" | jq '.'

if echo "$VERIFY_RESPONSE" | jq -e '.success == false' > /dev/null; then
    echo -e "${GREEN}✓ Invalid token correctly rejected${NC}"
else
    echo -e "${RED}✗ Invalid token should have been rejected${NC}"
fi

echo ""
echo "=================================="
echo ""

# Test 6: Resend Verification Email
echo "Test 6: Resend Verification Email"
echo "----------------------------------"
RESEND_RESPONSE=$(curl -s -X POST "$BASE_URL/resend-verification" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com"
  }')

echo "$RESEND_RESPONSE" | jq '.'

if echo "$RESEND_RESPONSE" | jq -e '.success == true' > /dev/null; then
    echo -e "${GREEN}✓ Resend verification successful${NC}"
else
    echo -e "${RED}✗ Resend verification failed${NC}"
fi

echo ""
echo "=================================="
echo ""

# Test 7: Resend Verification for Non-existent Email
echo "Test 7: Resend Verification for Non-existent Email (should fail)"
echo "-----------------------------------------------------------------"
RESEND_INVALID_RESPONSE=$(curl -s -X POST "$BASE_URL/resend-verification" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "nonexistent@example.com"
  }')

echo "$RESEND_INVALID_RESPONSE" | jq '.'

if echo "$RESEND_INVALID_RESPONSE" | jq -e '.success == false' > /dev/null; then
    echo -e "${GREEN}✓ Non-existent email correctly rejected${NC}"
else
    echo -e "${RED}✗ Non-existent email should have been rejected${NC}"
fi

echo ""
echo "=================================="
echo ""
echo "Test Suite Complete!"
echo ""
echo -e "${YELLOW}Note: To fully test email verification, you need to:${NC}"
echo "1. Check the email sent to test@example.com"
echo "2. Extract the verification token"
echo "3. Call POST /api/auth/verify-email with the real token"
echo ""
echo -e "${YELLOW}Database location:${NC} E:\\Projects\\DainnUser\\src\\DainnUser.Api\\dainnuser.db"
echo -e "${YELLOW}Swagger UI:${NC} http://localhost:5000/swagger"
