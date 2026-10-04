namespace Kindi.API.Domain.Entities;

using Kindi.API.Domain.Enums;

/// <summary>
/// Một node trong cây quyền đệ quy: Nhóm (group) → Màn hình (screen) → Hành động (action).
/// Bản ghi được seed tự động từ các danh mục trong code (<see cref="Enums.PermissionGroupCatalog"/>,
/// <see cref="Enums.PermissionScreenCatalog"/>, <see cref="Enums.PermissionCatalog"/>) — không sửa tay;
/// muốn thêm node thì thêm vào danh mục tương ứng rồi deploy.
/// Cây dùng tự tham chiếu <see cref="ParentCode"/> → <see cref="Code"/> nên sẵn sàng sâu thêm nhiều tầng.
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>Mã node: hành động dạng P### (ví dụ P020), màn hình/nhóm dùng mã chữ (OFFERS, ADMIN...).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Khoá dịch để UI hiển thị tên node (ví dụ <c>Permission_P020</c>, <c>PermissionScreen_OFFERS</c>).</summary>
    public string? NameKey { get; set; }

    /// <summary>Loại node trong cây: group | screen | action.</summary>
    public PermissionNodeKind NodeKind { get; set; } = PermissionNodeKind.Action;

    public PermissionModule Module { get; set; }

    /// <summary>
    /// Mã cha trong cây (<see cref="Code"/> của node cha) — hành động trỏ tới màn hình, màn hình trỏ tới nhóm,
    /// nhóm là gốc (null). Tự tham chiếu thay cho FK cũ trỏ sang <c>PermissionGroups</c>.
    /// </summary>
    public string? ParentCode { get; set; }

    /// <summary>Node cha tương ứng với <see cref="ParentCode"/>.</summary>
    public Permission? Parent { get; set; }

    /// <summary>Các node con trực tiếp (màn hình của một nhóm, hành động của một màn hình).</summary>
    public ICollection<Permission> Children { get; set; } = new List<Permission>();

    public PermissionKind Kind { get; set; }

    /// <summary>Route UI được quyền này mở (nếu là quyền dạng view).</summary>
    public string? Route { get; set; }

    /// <summary>Các endpoint API được quyền này bảo vệ (mô tả, cách nhau bằng dấu phẩy).</summary>
    public string? Endpoints { get; set; }

    /// <summary>Thứ tự hiển thị trong ma trận quyền (bằng giá trị số của mã).</summary>
    public int SortOrder { get; set; }
}
