using Kindi.API.Domain.Enums;
using Kindi.API.Shared.Common.Interfaces;
using Kindi.API.Shared.Constants;

namespace Kindi.API.Application.Common.Extensions;

public static class CurrentUserExtensions
{
    /// <summary>
    /// Kiểm tra người dùng hiện tại có vai trò <paramref name="role"/> không —
    /// claim vai trò trong JWT lưu dạng số nên so qua <see cref="RoleConstants"/>.
    /// SuperAdmin phải có nhánh riêng: thiếu nhánh này thì mọi lời gọi hỏi SuperAdmin sẽ rơi vào
    /// nhánh mặc định và kiểm tra nhầm role User (đã gặp: cờ bảo vệ tài khoản SuperAdmin không chạy).
    /// </summary>
    public static bool IsInRole(this ICurrentUserService currentUser, UserRole role) => role switch
    {
        UserRole.SuperAdmin => currentUser.IsInRole(RoleConstants.SuperAdmin),
        UserRole.Admin => currentUser.IsInRole(RoleConstants.Admin),
        UserRole.Partner => currentUser.IsInRole(RoleConstants.Partner),
        _ => currentUser.IsInRole(RoleConstants.User)
    };
}
