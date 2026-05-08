# Testing Skill

## Detect Framework

1. `.csproj` với `<PackageReference Include="xunit"` → dùng xUnit
2. `.csproj` với `<PackageReference Include="nunit"` → dùng NUnit
3. `.csproj` với `<PackageReference Include="MSTest"` → dùng MSTest
4. `package.json` → `vitest` → dùng Vitest
5. `package.json` → `jest` → dùng Jest
6. `pytest.ini` hoặc `pyproject.toml [tool.pytest]` → dùng Pytest
7. `go.mod` → dùng `go test`

## Run Unit Tests

# .NET (xUnit, NUnit, MSTest)
dotnet test

# Specific project
dotnet test tests/DainnUser.UnitTests/

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Vitest
npx vitest run

# Jest
npx jest --runInBand

# Pytest
pytest -v

# Go
go test ./...

## Run Integration Tests

# .NET
dotnet test tests/DainnUser.IntegrationTests/

# With TestServer
dotnet test --filter "Category=Integration"

## Run Security Tests

# .NET Security Tests
dotnet test tests/DainnUser.SecurityTests/

# OWASP ZAP (if configured)
zap-cli quick-scan --self-contained http://localhost:5000

## Test Requirements

**New feature:** Viết tests TRƯỚC implementation (TDD).
**Bug fix:** Viết regression test trước.
**Backend API:** Test happy path + error path + auth + validation.
**Security:** Test OWASP Top 10 scenarios.
**Library:** Test public API surface, không test internals.

## Test Categories

**.NET Test Categories:**
```csharp
[Fact]
public void UnitTest() { }

[Fact]
[Trait("Category", "Integration")]
public void IntegrationTest() { }

[Fact]
[Trait("Category", "Security")]
public void SecurityTest() { }
```

## Sau khi test

Report: "Unit tests: X/X passing. Integration tests: X/X passing. Security tests: X/X passing."
Nếu có failure: fix trước khi tiếp tục.
Nếu coverage < 80%: thêm tests.
