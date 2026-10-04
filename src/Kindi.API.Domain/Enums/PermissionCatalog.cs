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
    string? Endpoints,
    string Group,
    string Screen,
    string ScreenName,
    IReadOnlyList<string> Replaces)
{
    /// <summary>Quyền dạng hành động — luôn là lá của cây quyền.</summary>
    public PermissionNodeKind NodeKind => PermissionNodeKind.Action;

    /// <summary>Khoá dịch để UI hiển thị tên quyền.</summary>
    public string NameKey => PermissionNameKeys.Action(PermissionCode);

    /// <summary>Mã cha trong cây (màn hình chứa hành động này).</summary>
    public string ParentCode => Screen;
}

/// <summary>Cặp mã cũ → mã mới dùng khi chuyển quyền đã cấp sang mã mới sau khi tách nhỏ quyền.</summary>
public sealed record PermissionReplacement(string OldCode, string NewCode);

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

    /// <summary>
    /// Các cặp mã cũ → mã mới suy ra từ thuộc tính <c>Replaces</c> ở enum — seeder dùng để chuyển
    /// quyền đã cấp (role và tài khoản) từ mã gộp cũ sang các mã đã tách, tránh mất quyền.
    /// </summary>
    public static IReadOnlyList<PermissionReplacement> Replacements =>
        _all.SelectMany(newCode => newCode.Replaces.Select(oldCode => new PermissionReplacement(oldCode, newCode.PermissionCode)))
            .ToList();

    public static bool TryGet(string permissionCode, out PermissionDefinition definition)
    {
        definition = _all.FirstOrDefault(x => string.Equals(x.PermissionCode, permissionCode, StringComparison.OrdinalIgnoreCase))!;
        return definition != null;
    }

    /// <summary>
    /// Nhóm mặc định theo khu vực màn hình: quyền gắn trang /user/** thuộc khu vực thành viên (Người dùng, Đối tác),
    /// còn lại — trang /admin/** và thao tác nghiệp vụ không gắn trang riêng — thuộc hệ thống quản trị.
    /// Quyền mà cả hai khu vực cùng dùng thì khai <see cref="PermissionInfoAttribute.ParentCode"/> = SHARED tại enum.
    /// </summary>
    private static string DeriveGroup(PermissionInfoAttribute info)
    {
        if (info.Route is not null && info.Route.StartsWith("/user/", StringComparison.OrdinalIgnoreCase))
            return PermissionGroupCodes.Member;

        return PermissionGroupCodes.Admin;
    }

    private static IReadOnlyList<PermissionDefinition> BuildAll()
    {
        var result = new List<PermissionDefinition>();
        foreach (var field in typeof(PermissionCode).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var code = (PermissionCode)field.GetValue(null)!;
            var info = field.GetCustomAttribute<PermissionInfoAttribute>();
            if (info == null) continue;

            var screen = info.Screen ?? string.Empty;
            result.Add(new PermissionDefinition(
                code, code.ToCode(), info.Name, info.Module, info.Kind, info.Route, info.Endpoints,
                info.ParentCode ?? DeriveGroup(info),
                screen, PermissionScreenCatalog.ResolveName(screen), info.Replaces));
        }

        return result.OrderBy(x => (int)x.Code).ToList();
    }
}
