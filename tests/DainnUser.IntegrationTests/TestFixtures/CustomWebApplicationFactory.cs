using System.Collections.Concurrent;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace DainnUser.IntegrationTests.TestFixtures;

/// <summary>
/// Captures plain tokens passed to the mocked <see cref="IEmailService"/> so integration tests
/// can act on them. The persisted DB column holds the SHA-256 hash, not the plain token, so
/// reading the DB does not yield a usable token.
/// </summary>
public class EmailTokenCapture
{
    private readonly ConcurrentDictionary<string, string> _verificationTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _passwordResetTokens = new(StringComparer.OrdinalIgnoreCase);

    public void RecordVerification(string email, string token) => _verificationTokens[email] = token;
    public void RecordPasswordReset(string email, string token) => _passwordResetTokens[email] = token;

    public string GetVerification(string email) => _verificationTokens.TryGetValue(email, out var t)
        ? t : throw new InvalidOperationException($"No verification token captured for {email}.");

    public string GetPasswordReset(string email) => _passwordResetTokens.TryGetValue(email, out var t)
        ? t : throw new InvalidOperationException($"No password reset token captured for {email}.");
}

/// <summary>
/// Custom WebApplicationFactory for integration testing with TestServer.
/// Configures in-memory database and test-friendly settings.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string DatabaseName = $"InMemoryTestDb_{Guid.NewGuid():N}";
    private const string TestJwtSecret = "test-secret-key-for-integration-tests-minimum-32-chars!!";

    public EmailTokenCapture EmailTokens { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "25",
                ["DainnUser:Email:SmtpUsername"] = "",
                ["DainnUser:Email:SmtpPassword"] = "",
                ["DainnUser:Email:FromEmail"] = "noreply@test.com",
                ["DainnUser:Email:FromName"] = "Test",
                ["DainnUser:Email:EnableSsl"] = "false",
                ["DainnUser:Jwt:Secret"] = TestJwtSecret,
                ["DainnUser:Jwt:Issuer"] = "DainnUserTest",
                ["DainnUser:Jwt:Audience"] = "DainnUserTest",
                ["DainnUser:Jwt:ValidateIssuer"] = "false",
                ["DainnUser:Jwt:ValidateAudience"] = "false",
                ["DainnUser:Jwt:ClockSkewSeconds"] = "300",
                ["DainnUser:RateLimiting:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Register HttpClient for services that depend on it
            services.AddHttpClient();

            // Remove the existing DbContext registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<DainnUserDbContext>)
                         || d.ServiceType == typeof(DainnUserDbContext))
                .ToList();
            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // Add EF Core InMemory database for testing (shared static name)
            services.AddDbContext<DainnUserDbContext>(options =>
            {
                options.UseInMemoryDatabase(DatabaseName);
            });

            // Replace EmailService with a mock that doesn't actually send emails
            var emailServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailServiceDescriptor != null)
                services.Remove(emailServiceDescriptor);

            var mockEmailService = new Mock<IEmailService>();
            mockEmailService
                .Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((email, _, plainToken, _) => EmailTokens.RecordVerification(email, plainToken))
                .Returns(Task.CompletedTask);
            mockEmailService
                .Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((email, _, plainToken, _) => EmailTokens.RecordPasswordReset(email, plainToken))
                .Returns(Task.CompletedTask);
            mockEmailService
                .Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockEmailService
                .Setup(x => x.SendAccountLockoutNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(mockEmailService.Object);

            // Override JWT bearer options to use JwtSecurityTokenHandler so it can
            // validate tokens produced by JwtTokenService (which also uses JwtSecurityTokenHandler).
            // ASP.NET Core 8 defaults to JsonWebTokenHandler which is incompatible with legacy tokens.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret))
                {
                    KeyId = "DainnUserSigningKey"
                };

                options.TokenHandlers.Clear();
                options.TokenHandlers.Add(new JwtSecurityTokenHandler
                {
                    InboundClaimTypeMap = new Dictionary<string, string>()
                });

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });
        });
    }

    /// <summary>
    /// Ensures the InMemory database is created before tests run.
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DainnUserDbContext>();
        db.Database.EnsureCreated();
    }
}
