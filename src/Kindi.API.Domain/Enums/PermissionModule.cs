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

/// <summary>Loại quyền: màn hình (View) hay thao tác (Action).</summary>
public enum PermissionKind
{
    View = 1,
    Action = 2
}
