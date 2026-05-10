using DainnUser.Infrastructure;
using DainnUser.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DainnUser.UnitTests.Infrastructure;

public class DainnUserServiceExtensionsTests
{
    [Fact]
    public void AddDainnUser_WithValidConfiguration_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "1025",
                ["DainnUser:Email:FromEmail"] = "test@example.com",
                ["DainnUser:Jwt:Secret"] = "test-jwt-secret-must-be-at-least-32-bytes-long-okay"
            })
            .Build();

        // Act
        services.AddDainnUser(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();

        // Verify DainnUserOptions is registered
        var options = serviceProvider.GetService<DainnUserOptions>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void AddDainnUser_WithCustomOptions_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "1025",
                ["DainnUser:Email:FromEmail"] = "test@example.com",
                ["DainnUser:Jwt:Secret"] = "test-jwt-secret-must-be-at-least-32-bytes-long-okay"
            })
            .Build();

        // Act
        services.AddDainnUser(configuration, options =>
        {
            options.EnableSocialLogin = true;
            options.EnableTwoFactor = true;
            options.RequireEmailVerification = false;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<DainnUserOptions>();

        options.EnableSocialLogin.Should().BeTrue();
        options.EnableTwoFactor.Should().BeTrue();
        options.RequireEmailVerification.Should().BeFalse();
    }

    [Fact]
    public void AddDainnUser_WithMissingConnectionString_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "1025",
                ["DainnUser:Email:FromEmail"] = "test@example.com",
                ["DainnUser:Jwt:Secret"] = "test-jwt-secret-must-be-at-least-32-bytes-long-okay"
            })
            .Build();

        // Act & Assert
        var act = () => services.AddDainnUser(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*connection string*");
    }

    [Fact]
    public void AddDainnUser_WithMissingProvider_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "1025",
                ["DainnUser:Email:FromEmail"] = "test@example.com",
                ["DainnUser:Jwt:Secret"] = "test-jwt-secret-must-be-at-least-32-bytes-long-okay"
            })
            .Build();

        // Act & Assert
        var act = () => services.AddDainnUser(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*provider*");
    }

    [Fact]
    public void AddDainnUser_WithUnsupportedProvider_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "Oracle",
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "1025",
                ["DainnUser:Email:FromEmail"] = "test@example.com",
                ["DainnUser:Jwt:Secret"] = "test-jwt-secret-must-be-at-least-32-bytes-long-okay"
            })
            .Build();

        // Act & Assert
        var act = () => services.AddDainnUser(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unsupported database provider*");
    }

    [Fact]
    public void AddDainnUser_WithMissingSmtpHost_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpPort"] = "1025",
                ["DainnUser:Email:FromEmail"] = "test@example.com",
                ["DainnUser:Jwt:Secret"] = "test-jwt-secret-must-be-at-least-32-bytes-long-okay"
            })
            .Build();

        // Act & Assert
        var act = () => services.AddDainnUser(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SMTP host*");
    }

    [Fact]
    public void AddDainnUser_WithMissingSmtpPort_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:FromEmail"] = "test@example.com",
                ["DainnUser:Jwt:Secret"] = "test-jwt-secret-must-be-at-least-32-bytes-long-okay"
            })
            .Build();

        // Act & Assert
        var act = () => services.AddDainnUser(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SMTP port*");
    }

    [Fact]
    public void AddDainnUser_WithMissingJwtSecret_ThrowsException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "1025",
                ["DainnUser:Email:FromEmail"] = "test@example.com"
            })
            .Build();

        var act = () => services.AddDainnUser(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT secret*");
    }

    [Fact]
    public void AddDainnUser_WithShortJwtSecret_ThrowsException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "1025",
                ["DainnUser:Email:FromEmail"] = "test@example.com",
                ["DainnUser:Jwt:Secret"] = "too-short"
            })
            .Build();

        var act = () => services.AddDainnUser(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void AddDainnUser_WithMissingFromEmail_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DainnUser:Database:Provider"] = "SQLite",
                ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
                ["DainnUser:Email:SmtpHost"] = "localhost",
                ["DainnUser:Email:SmtpPort"] = "1025"
            })
            .Build();

        // Act & Assert
        var act = () => services.AddDainnUser(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*from address*");
    }
}
