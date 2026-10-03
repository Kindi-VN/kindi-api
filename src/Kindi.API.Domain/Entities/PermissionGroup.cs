namespace Kindi.API.Domain.Entities;

/// <summary>
/// Nhóm quyền dùng để gom nhóm ở màn phân quyền. Bản ghi được seed từ
/// <see cref="Enums.PermissionGroupCatalog"/>; tên và thứ tự nhóm sửa được trực tiếp trong DB
/// (seeder chỉ thêm nhóm còn thiếu).
/// </summary>
public class PermissionGroup : BaseEntity
{
    /// <summary>Mã nhóm (SYSTEM, USER, MEMBER…) — quyền trỏ tới nhóm qua cột ParentCode.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên nhóm hiển thị (tiếng Việt).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tên nhóm hiển thị (tiếng Anh).</summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>Thứ tự nhóm trong danh sách quyền.</summary>
    public int SortOrder { get; set; }
}
