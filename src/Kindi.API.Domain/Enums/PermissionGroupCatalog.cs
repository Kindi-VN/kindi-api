namespace Kindi.API.Domain.Enums;

/// <summary>
/// Mã nhóm quyền — giá trị lưu ở <c>Permissions.ParentCode</c> và <c>PermissionGroups.Code</c>.
/// </summary>
public static class PermissionGroupCodes
{
    public const string System = "SYSTEM";
    public const string User = "USER";
    public const string Member = "MEMBER";
    public const string Partner = "PARTNER";
    public const string Purchase = "PURCHASE";
    public const string Group = "GROUP";
    public const string Community = "COMMUNITY";
    public const string Referral = "REFERRAL";
    public const string Commission = "COMMISSION";
    public const string SuperAdmin = "SUPERADMIN";
}

/// <summary>Một nhóm quyền trong danh mục.</summary>
public sealed record PermissionGroupDefinition(
    string Code,
    string Name,
    string NameEn,
    int SortOrder);

/// <summary>
/// Danh mục nhóm quyền — nguồn duy nhất để seed bảng <c>PermissionGroups</c>. Mã nhóm do code quyết định,
/// còn TÊN và THỨ TỰ hiển thị sửa được trong DB: seeder chỉ thêm nhóm còn thiếu, không ghi đè nhóm đã có.
/// </summary>
public static class PermissionGroupCatalog
{
    public static IReadOnlyList<PermissionGroupDefinition> All { get; } = new List<PermissionGroupDefinition>
    {
        new(PermissionGroupCodes.System, "Hệ thống", "System", 10),
        new(PermissionGroupCodes.User, "Người dùng & CTV", "Users & collaborators", 20),
        new(PermissionGroupCodes.Member, "Khu vực thành viên", "Member area", 30),
        new(PermissionGroupCodes.Partner, "Đối tác & công ty", "Partners & companies", 40),
        new(PermissionGroupCodes.Purchase, "Mua chung & tìm hàng", "Sourcing & offers", 50),
        new(PermissionGroupCodes.Group, "Nhóm ngành & hội nhóm", "Business groups", 60),
        new(PermissionGroupCodes.Community, "Cộng đồng", "Community", 70),
        new(PermissionGroupCodes.Referral, "Giới thiệu", "Referral", 80),
        new(PermissionGroupCodes.Commission, "Hoa hồng & giải ngân", "Commission & payouts", 90),
        new(PermissionGroupCodes.SuperAdmin, "Quản trị tối cao", "Super admin", 100)
    };
}
