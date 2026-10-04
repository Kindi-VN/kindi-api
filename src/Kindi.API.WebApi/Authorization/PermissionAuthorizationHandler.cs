namespace Kindi.API.WebApi.Authorization;

using System.Security.Claims;
using Kindi.API.Domain.Enums;
using Kindi.API.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Kiểm quyền từ claim <c>perm</c> của token (danh sách mã node cách nhau bằng dấu phẩy).
/// Quyền áp KẾ THỪA theo cây: hiệu lực = chính node được cấp VÀ mọi tổ tiên đều được cấp
/// (tắt màn hình/nhóm ⇒ mọi hành động con mất hiệu lực). Thuần tính trên danh mục tĩnh nên không query DB.
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

        // Token phát hành trước khi có cây quyền không mang mã node cha (màn hình/nhóm) → bỏ qua kế thừa
        // trong giai đoạn chuyển tiếp để không chặn oan; token mới luôn có mã node cha nên áp kế thừa đầy đủ.
        var hasStructural = granted.Any(code => PermissionTreeCatalog.IsGroup(code) || PermissionTreeCatalog.IsScreen(code));
        var effective = hasStructural ? PermissionTreeCatalog.ApplyInheritance(granted) : granted;

        if (requirement.PermissionCodes.Any(code => effective.Contains(code)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
