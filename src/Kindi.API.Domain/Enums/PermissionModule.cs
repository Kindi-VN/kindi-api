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

/// <summary>
/// Loại node trong cây quyền đệ quy: Nhóm (group) → Màn hình (screen) → Hành động (action).
/// Cấu trúc sẵn sàng cho nhiều tầng hơn: cha của một node là <c>Permissions.ParentCode</c> (tự tham chiếu
/// <c>Permissions.Code</c>), nên chỉ cần thêm tầng mới là cây tự sâu thêm mà không đổi schema.
/// </summary>
public enum PermissionNodeKind
{
    /// <summary>Nhóm cấp cao nhất (ADMIN/MEMBER/SHARED) — nút gốc của cây.</summary>
    Group = 1,

    /// <summary>Màn hình nghiệp vụ (USERS, OFFERS...) — con của một nhóm, cha của các hành động.</summary>
    Screen = 2,

    /// <summary>Hành động cụ thể (Xem/Sửa/Xoá/Khôi phục) — lá của cây, gác endpoint.</summary>
    Action = 3
}
