using DainnUser.Core.Configuration;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Data;
using DainnUser.Infrastructure.Repositories;
using DainnUser.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace DainnUser.SecurityTests.TestFixtures;

/// <summary>
/// Shared per-test fixture wiring an in-memory <see cref="DainnUserDbContext"/>, repositories,
/// password hasher, and JWT token service so security tests can drive the real
/// <see cref="AuthenticationService"/> with controllable options.
/// </summary>
public sealed class SecurityTestFixture : IDisposable
{
    private const string TestJwtSecret = "security-test-secret-must-be-at-least-32-bytes-long-okay";

    public DainnUserDbContext DbContext { get; }
    public DainnUserOptions Options { get; }
    public Mock<IEmailService> EmailServiceMock { get; }
    public IPasswordHasher<User> PasswordHasher { get; }
    public IJwtTokenService JwtTokenService { get; }
    public UserRepository UserRepository { get; }
    public UnitOfWork UnitOfWork { get; }
    public AuthenticationService AuthenticationService { get; }

    public SecurityTestFixture(Action<DainnUserOptions>? configure = null)
    {
        var dbOptions = new DbContextOptionsBuilder<DainnUserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new DainnUserDbContext(dbOptions);
        DbContext.Database.EnsureCreated();

        Options = new DainnUserOptions
        {
            // Defaults: lockout enabled, email verification ON. Individual tests can flip via configure.
            EnableAccountLockout = true,
            MaxFailedLoginAttempts = 5,
            LockoutDurationMinutes = 15,
            RequireEmailVerification = false, // most tests bypass verify; A07 tests will flip back on
            EnableSessionManagement = true,
            JwtExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
        configure?.Invoke(Options);

        EmailServiceMock = new Mock<IEmailService>();
        PasswordHasher = new PasswordHasher<User>();
        JwtTokenService = new JwtTokenService(
            Microsoft.Extensions.Options.Options.Create(new JwtOptions { Secret = TestJwtSecret }),
            Options);
        UserRepository = new UserRepository(DbContext);
        UnitOfWork = new UnitOfWork(DbContext);
        AuthenticationService = new AuthenticationService(
            UserRepository,
            UnitOfWork,
            EmailServiceMock.Object,
            PasswordHasher,
            JwtTokenService,
            Options);
    }

    /// <summary>
    /// Registers + activates a user for tests that need an account already in good standing.
    /// Returns the user id.
    /// </summary>
    public async Task<Guid> RegisterAndActivateAsync(string email, string username, string password)
    {
        var userId = await AuthenticationService.RegisterAsync(email, username, password);
        var user = await DbContext.Users.Include(u => u.Tokens).FirstAsync(u => u.Id == userId);
        user.EmailVerified = true;
        user.Status = Core.Enums.UserStatus.Active;
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return userId;
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
    }
}
