using System.Linq.Expressions;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for administrative user management.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserManagementService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(IEnumerable<UserDto> Items, int TotalCount)> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        UserStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        IEnumerable<User> users;
        int totalCount;

        if (!string.IsNullOrWhiteSpace(search) || status.HasValue)
        {
            var predicate = PredicateBuilder.True<User>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLowerInvariant();
                predicate = predicate.And(u =>
                    u.Email.ToLower().Contains(term) ||
                    u.Username.ToLower().Contains(term));
            }

            if (status.HasValue)
            {
                predicate = predicate.And(u => u.Status == status.Value);
            }

            var result = await _userRepository.GetPagedAsync(predicate, pageNumber, pageSize, cancellationToken);
            users = result.Items;
            totalCount = result.TotalCount;
        }
        else
        {
            var result = await _userRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
            users = result.Items;
            totalCount = result.TotalCount;
        }

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _roleRepository.GetByUserIdAsync(user.Id, cancellationToken);
            userDtos.Add(MapToDto(user, roles));
        }

        return (userDtos, totalCount);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAsync(userId, cancellationToken);
        if (user == null) return null;

        var roles = await _roleRepository.GetByUserIdAsync(userId, cancellationToken);
        return MapToDto(user, roles);
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            if (await _userRepository.IsEmailTakenAsync(dto.Email, userId, cancellationToken))
            {
                throw new InvalidOperationException($"Email '{dto.Email}' is already taken.");
            }
            user.Email = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
        {
            if (await _userRepository.IsUsernameTakenAsync(dto.Username, userId, cancellationToken))
            {
                throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
            }
            user.Username = dto.Username;
        }

        if (dto.Status.HasValue)
        {
            user.Status = dto.Status.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var roles = await _roleRepository.GetByUserIdAsync(userId, cancellationToken);
        return MapToDto(user, roles);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        _userRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        user.Status = UserStatus.Locked;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        user.Status = UserStatus.Active;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role == null)
        {
            throw new InvalidOperationException($"Role with ID '{roleId}' not found.");
        }

        if (await _roleRepository.UserHasRoleAsync(userId, roleId, cancellationToken))
        {
            return; // Already has the role
        }

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };

        await _roleRepository.AddUserRoleAsync(userRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _roleRepository.GetUserRoleAsync(userId, roleId, cancellationToken);
        if (userRole == null)
        {
            throw new InvalidOperationException($"User does not have the specified role.");
        }

        _roleRepository.RemoveUserRole(userRole);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static UserDto MapToDto(User user, IEnumerable<Role> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            Status = user.Status,
            EmailVerified = user.EmailVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = roles.Select(r => r.Name).ToList()
        };
    }
}

/// <summary>
/// Helper for building dynamic predicates.
/// </summary>
internal static class PredicateBuilder
{
    public static Expression<Func<T, bool>> True<T>() => _ => true;
    public static Expression<Func<T, bool>> False<T>() => _ => false;

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(left, parameter),
            Expression.Invoke(right, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
