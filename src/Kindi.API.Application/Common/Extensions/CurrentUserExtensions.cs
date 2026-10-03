using Kindi.API.Domain.Enums;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Constants;

namespace Kindi.API.Application.Common.Extensions;

public static class CurrentUserExtensions
{
    /// <summary>
    /// Kiểm tra người dùng hiện tại có vai trò <paramref name="role"/> không —
    /// claim vai trò trong JWT lưu dạng số nên so qua <see cref="RoleConstants"/>.
    /// </summary>
    public static bool IsInRole(this ICurrentUserService currentUser, UserRole role) => role switch
    {
        UserRole.Admin => currentUser.IsInRole(RoleConstants.Admin),
        UserRole.Partner => currentUser.IsInRole(RoleConstants.Partner),
        _ => currentUser.IsInRole(RoleConstants.User)
    };
}
