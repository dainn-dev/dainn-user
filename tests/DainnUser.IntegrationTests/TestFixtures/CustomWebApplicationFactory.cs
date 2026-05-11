using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace DainnUser.IntegrationTests.TestFixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing with TestServer.
/// Configures in-memory database and test-friendly settings.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration - use SQLite as provider since InMemory is not supported
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Database:ConnectionString"] = "Data Source=:memory:",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "25",
                ["DainnUser:Email:SmtpUsername"] = "test@test.com",
                ["DainnUser:Email:SmtpPassword"] = "testpassword",
                ["DainnUser:Email:FromEmail"] = "noreply@test.com",
                ["DainnUser:Email:FromName"] = "Test",
                ["DainnUser:Email:EnableSsl"] = "false",
                ["DainnUser:Jwt:Secret"] = "test-secret-key-for-integration-tests-minimum-32-characters-long",
                ["DainnUser:Jwt:Issuer"] = "DainnUserTest",
                ["DainnUser:Jwt:Audience"] = "DainnUserTest",
                ["DainnUser:Jwt:ExpirationMinutes"] = "60",
                ["DainnUser:Jwt:RefreshTokenExpirationDays"] = "7",
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
            {
                services.Remove(descriptor);
            }

            // Add EF Core InMemory database for testing
            services.AddDbContext<DainnUserDbContext>(options =>
            {
                options.UseInMemoryDatabase($"InMemoryTestDb_{Guid.NewGuid()}");
            });

            // Replace EmailService with a mock that doesn't actually send emails
            var emailServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));
            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }

            var mockEmailService = new Mock<IEmailService>();
            mockEmailService
                .Setup(x => x.SendEmailVerificationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockEmailService
                .Setup(x => x.SendPasswordResetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockEmailService
                .Setup(x => x.SendPasswordChangedNotificationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(mockEmailService.Object);
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
