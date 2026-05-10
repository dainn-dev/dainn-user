using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for role and permission management.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleService"/> class.
    /// </summary>
    public RoleService(IRoleRepository roleRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateRoleAsync(
        string name,
        string? description,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeRoleName(name);
        if (await _roleRepository.IsNameTakenAsync(normalizedName, null, cancellationToken))
        {
            throw new InvalidOperationException("Role name is already taken.");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Description = NormalizeDescription(description),
            Permissions = SerializePermissions(permissions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return role.Id;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateRoleAsync(
        Guid roleId,
        string name,
        string? description,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        var normalizedName = NormalizeRoleName(name);
        if (await _roleRepository.IsNameTakenAsync(normalizedName, roleId, cancellationToken))
        {
            throw new InvalidOperationException("Role name is already taken.");
        }

        role.Name = normalizedName;
        role.Description = NormalizeDescription(description);
        role.Permissions = SerializePermissions(permissions);
        role.UpdatedAt = DateTime.UtcNow;

        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetWithUsersAsync(roleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        if (role.UserRoles.Any())
        {
            throw new InvalidOperationException("Cannot delete a role assigned to users.");
        }

        _roleRepository.Remove(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        if (await _roleRepository.UserHasRoleAsync(userId, roleId, cancellationToken))
        {
            return true;
        }

        await _roleRepository.AddUserRoleAsync(new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var assignment = await _roleRepository.GetUserRoleAsync(userId, roleId, cancellationToken);
        if (assignment is null)
        {
            return true;
        }

        _roleRepository.RemoveUserRole(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetByUserIdAsync(userId, cancellationToken);
        return roles.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        return role is null ? Array.Empty<string>() : ParsePermissions(role.Permissions);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return roles.ToList();
    }

    /// <inheritdoc/>
    public Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return _roleRepository.GetByIdAsync(roleId, cancellationToken);
    }

    private static string NormalizeRoleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static string SerializePermissions(IEnumerable<string> permissions)
    {
        return string.Join(',', ParsePermissions(permissions));
    }

    private static IReadOnlyCollection<string> ParsePermissions(string? permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions))
        {
            return Array.Empty<string>();
        }

        return ParsePermissions(permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static IReadOnlyCollection<string> ParsePermissions(IEnumerable<string> permissions)
    {
        return permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
