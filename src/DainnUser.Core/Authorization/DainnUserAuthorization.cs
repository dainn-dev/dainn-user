namespace DainnUser.Core.Authorization;

/// <summary>
/// Claim type constants used by DainnUser tokens.
/// </summary>
public static class DainnUserClaimTypes
{
    /// <summary>The claim type for individual permissions emitted into JWT access tokens.</summary>
    public const string Permission = "permission";
}

/// <summary>
/// Permission string constants that map to operations in the system.
/// These values are stored in <c>Role.Permissions</c> as a comma-separated list.
/// </summary>
public static class DainnUserPermissions
{
    public const string UsersRead   = "users:read";
    public const string UsersWrite  = "users:write";
    public const string UsersDelete = "users:delete";
    public const string RolesRead   = "roles:read";
    public const string RolesWrite  = "roles:write";
    public const string RolesDelete = "roles:delete";
    public const string SettingsRead  = "settings:read";
    public const string SettingsWrite = "settings:write";
    public const string ProfileRead   = "profile:read";
    public const string ProfileWrite  = "profile:write";
}

/// <summary>
/// Authorization policy name constants for use with <c>[Authorize(Policy = "...")]</c>.
/// </summary>
public static class DainnUserPolicies
{
    public const string CanReadUsers    = "CanReadUsers";
    public const string CanWriteUsers   = "CanWriteUsers";
    public const string CanDeleteUser   = "CanDeleteUser";
    public const string CanManageRoles  = "CanManageRoles";
    public const string CanDeleteRole   = "CanDeleteRole";
    public const string CanManageSettings = "CanManageSettings";
}
