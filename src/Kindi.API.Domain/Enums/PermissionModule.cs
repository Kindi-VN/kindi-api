namespace Kindi.API.Domain.Enums;

/// <summary>Nhóm chức năng của một quyền — dùng để gom nhóm ở màn quản lý quyền.</summary>
public enum PermissionModule
{
    System = 1,
    User = 2,
    Partner = 3,
    Purchase = 4,
    Group = 5,
    Community = 6,
    Referral = 7,

    /// <summary>Khai doanh thu, cấu hình loại thu/chi — tách khỏi SuperAdmin để cấp được cho quản trị viên.</summary>
    Revenue = 9,

    SuperAdmin = 8
}

/// <summary>
/// Loại quyền, dùng để gom nhóm hành động của một màn hình: xem (View), thêm/sửa (Update),
/// xoá (Delete) và thao tác đặc thù không thuộc ba nhóm trên (Action — duyệt, từ chối, kích hoạt...).
/// </summary>
public enum PermissionKind
{
    View = 1,
    Action = 2,
    Update = 3,
    Delete = 4
}
