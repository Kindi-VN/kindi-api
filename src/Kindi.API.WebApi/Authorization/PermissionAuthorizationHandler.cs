namespace Kindi.API.WebApi.Authorization;

using System.Security.Claims;
using Kindi.API.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Kiểm quyền từ claim <c>perm</c> của token (danh sách mã P### cách nhau bằng dấu phẩy).
/// SuperAdmin luôn qua.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var user = context.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (user.IsInRole(RoleConstants.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var granted = (user.FindFirst(AuthClaimConstants.Permissions)?.Value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requirement.PermissionCodes.Any(code => granted.Contains(code)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
