namespace Kindi.API.Domain.Enums;

using System.Reflection;
using Kindi.API.Domain.Attributes;

/// <summary>Một dòng trong danh mục quyền (enum + thông tin mô tả).</summary>
public sealed record PermissionDefinition(
    PermissionCode Code,
    string PermissionCode,
    string Name,
    PermissionModule Module,
    PermissionKind Kind,
    string? Route,
    string? Endpoints);

/// <summary>
/// Danh mục quyền lấy trực tiếp từ <see cref="PermissionCode"/> (nguồn duy nhất là enum) — dùng để
/// seed bảng Permissions, dựng ma trận quyền ở màn quản trị và kiểm tra endpoint thiếu quyền.
/// </summary>
public static class PermissionCatalog
{
    private static readonly IReadOnlyList<PermissionDefinition> _all = BuildAll();

    public static IReadOnlyList<PermissionDefinition> All => _all;

    /// <summary>
    /// Quyền cơ bản của khu vực thành viên — mọi tài khoản đã đăng nhập đều có.
    /// Quyền của tính năng đang thử nghiệm (hoa hồng) KHÔNG nằm ở đây — bật riêng cho từng tài khoản.
    /// </summary>
    private static readonly PermissionCode[] _memberArea =
    {
        PermissionCode.ViewMyReferralStats,
        PermissionCode.ViewMyGroupBuying,
        PermissionCode.ViewMyRequests,
        PermissionCode.ViewMyPosts,
        PermissionCode.ViewMyGroups
    };

    /// <summary>
    /// Quyền mặc định của role: Admin có toàn bộ quyền nghiệp vụ, User và Partner (đối tác là tài khoản
    /// khách hàng đã được duyệt hồ sơ) có quyền cơ bản của khu vực thành viên, nhóm SuperAdmin không gán cho ai.
    /// </summary>
    public static IReadOnlyList<PermissionCode> DefaultFor(UserRole role) => role switch
    {
        UserRole.Admin => _all.Where(x => x.Module != PermissionModule.SuperAdmin).Select(x => x.Code).ToList(),
        UserRole.User or UserRole.Partner => _memberArea,
        _ => Array.Empty<PermissionCode>()
    };

    public static PermissionDefinition Get(PermissionCode code) => _all.First(x => x.Code == code);

    public static bool TryGet(string permissionCode, out PermissionDefinition definition)
    {
        definition = _all.FirstOrDefault(x => string.Equals(x.PermissionCode, permissionCode, StringComparison.OrdinalIgnoreCase))!;
        return definition != null;
    }

    private static IReadOnlyList<PermissionDefinition> BuildAll()
    {
        var result = new List<PermissionDefinition>();
        foreach (var field in typeof(PermissionCode).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var code = (PermissionCode)field.GetValue(null)!;
            var info = field.GetCustomAttribute<PermissionInfoAttribute>();
            if (info == null) continue;

            result.Add(new PermissionDefinition(
                code, code.ToCode(), info.Name, info.Module, info.Kind, info.Route, info.Endpoints));
        }

        return result.OrderBy(x => (int)x.Code).ToList();
    }
}
