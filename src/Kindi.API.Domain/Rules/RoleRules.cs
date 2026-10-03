namespace Kindi.API.Domain.Rules;

using Kindi.API.Domain.Enums;

/// <summary>
/// Quy tắc gán role: SuperAdmin không nằm trong danh sách được gán — không API/UI/seeder nào được
/// tạo hoặc nâng một tài khoản lên SuperAdmin (tài khoản này cấp phát thủ công, mỗi DB chỉ 1).
/// </summary>
public static class RoleRules
{
    /// <summary>Các role được phép gán qua API/quản trị.</summary>
    public static readonly IReadOnlyList<UserRole> AssignableRoles = new[]
    {
        UserRole.User,
        UserRole.Partner,
        UserRole.Admin
    };

    public static bool IsAssignable(UserRole role) => AssignableRoles.Contains(role);

    /// <summary>Role dùng cho ma trận phân quyền (SuperAdmin luôn toàn quyền, không lưu ở bảng).</summary>
    public static IReadOnlyList<UserRole> ConfigurableRoles => AssignableRoles;
}
