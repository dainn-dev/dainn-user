using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using DainnUser.Core.Exceptions;
using DainnUser.SecurityTests.TestFixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.SecurityTests.Owasp;

/// <summary>
/// OWASP A03:2021 — Injection.
/// EF Core uses parameterized queries by default, so the realistic risk is misuse (string
/// concatenation in custom queries). These tests pass classic SQL-injection payloads through
/// every public string entry point and verify that (a) the operation completes safely,
/// (b) the payload is treated as data, not code, and (c) input validators block the more
/// obvious payloads at the boundary so they never reach storage.
/// </summary>
public class A03_InjectionTests
{
    public static IEnumerable<object[]> SqlPayloads => new[]
    {
        new object[] { "' OR '1'='1" },
        new object[] { "'; DROP TABLE Users;--" },
        new object[] { "admin'--" },
        new object[] { "\" OR \"\"=\"" },
        new object[] { "1' UNION SELECT NULL--" }
    };

    [Theory]
    [MemberData(nameof(SqlPayloads))]
    public async Task LoginEmail_WithSqlPayload_FailsSafelyWithGenericError(string payload)
    {
        // Sending a SQL-injection payload as the email value must NOT throw a database error
        // and must return the same generic "invalid credentials" used for unknown users.
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("legit@example.com", "legit", "Pass123!@#");

        var act = async () => await fx.AuthenticationService.LoginAsync(payload, "Pass123!@#", null, null);

        await act.Should().ThrowAsync<InvalidCredentialsException>();

        // Database is intact: the legitimate user still exists.
        var stillThere = await fx.DbContext.Users.AsNoTracking().AnyAsync(u => u.Email == "legit@example.com");
        stillThere.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SqlPayloads))]
    public async Task RefreshToken_WithSqlPayload_FailsSafelyWithGenericError(string payload)
    {
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("legit@example.com", "legit", "Pass123!@#");

        var act = async () => await fx.AuthenticationService.RefreshTokenAsync(payload, null, null);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Theory]
    [InlineData("admin' OR '1'='1")]
    [InlineData("user; DROP TABLE Users;")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("../../etc/passwd")]
    public void RegisterUsername_WithMaliciousChars_RejectedByValidator(string username)
    {
        // The validator pattern `^[a-zA-Z0-9_-]+$` blocks every common injection / traversal
        // payload at the boundary, so they never reach storage or output sinks.
        var validator = new RegisterDtoValidator();
        var dto = new RegisterDto
        {
            Email = "x@example.com",
            Username = username,
            Password = "Pass123!@#",
            ConfirmPassword = "Pass123!@#"
        };

        var result = validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterDto.Username));
    }

    [Fact]
    public async Task NullByteInEmail_DoesNotMatchExistingUser()
    {
        // A null-byte in the email must not let an attacker truncate the lookup and match a
        // shorter prefix.
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("legit@example.com", "legit", "Pass123!@#");

        var act = async () => await fx.AuthenticationService.LoginAsync("legit@example.com\0evil", "Pass123!@#", null, null);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }
}
