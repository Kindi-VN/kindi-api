namespace Kindi.API.Domain.Enums;

/// <summary>
/// Mã nhóm quyền — giá trị lưu ở <c>Permissions.ParentCode</c> và <c>PermissionGroups.Code</c>.
/// </summary>
public static class PermissionGroupCodes
{
    /// <summary>Quyền của khu vực quản trị: các trang /admin/** và thao tác nghiệp vụ tương ứng.</summary>
    public const string Admin = "ADMIN";

    /// <summary>Quyền của khu vực thành viên (tài khoản Người dùng và Đối tác): các trang /user/**.</summary>
    public const string Member = "MEMBER";

    /// <summary>Quyền được cả hai khu vực cùng dùng. Khai trực tiếp ở enum khi phát sinh; hiện chưa có quyền nào.</summary>
    public const string Shared = "SHARED";
}

/// <summary>Một nhóm quyền trong danh mục.</summary>
public sealed record PermissionGroupDefinition(
    string Code,
    string Name,
    string NameEn,
    int SortOrder);

/// <summary>
/// Danh mục nhóm quyền — nguồn duy nhất để seed bảng <c>PermissionGroups</c>. Nhóm gom theo KHU VỰC sử dụng:
/// quản trị (ADMIN), khu vực thành viên của Người dùng/Đối tác (MEMBER) và quyền dùng chung (SHARED).
/// Mã nhóm do code quyết định, còn TÊN và THỨ TỰ hiển thị sửa được trong DB: seeder chỉ thêm nhóm còn thiếu,
/// không ghi đè nhóm đã có, và ẩn các nhóm cũ không còn trong danh mục.
/// </summary>
public static class PermissionGroupCatalog
{
    public static IReadOnlyList<PermissionGroupDefinition> All { get; } = new List<PermissionGroupDefinition>
    {
        new(PermissionGroupCodes.Admin, "Hệ thống quản trị", "Management system", 10),
        new(PermissionGroupCodes.Member, "Khu vực thành viên", "Member area", 20),
        new(PermissionGroupCodes.Shared, "Dùng chung", "Shared", 40)
    };
}
