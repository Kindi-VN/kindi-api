namespace Kindi.API.WebApi.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>Yêu cầu quyền của một endpoint — danh sách mã P### (chỉ cần có 1 mã).</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(IReadOnlyList<string> permissionCodes)
    {
        PermissionCodes = permissionCodes;
    }

    public IReadOnlyList<string> PermissionCodes { get; }
}
