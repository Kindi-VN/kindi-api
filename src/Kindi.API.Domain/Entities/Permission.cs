namespace Kindi.API.Domain.Entities;

using Kindi.API.Domain.Enums;

/// <summary>
/// Danh mục quyền (mã P###). Bản ghi được seed tự động từ enum <see cref="PermissionCode"/> —
/// không sửa tay; muốn thêm quyền thì thêm member vào enum rồi deploy.
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>Mã quyền dạng P### (ví dụ P020).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public PermissionModule Module { get; set; }

    public PermissionKind Kind { get; set; }

    /// <summary>Route UI được quyền này mở (nếu là quyền dạng view).</summary>
    public string? Route { get; set; }

    /// <summary>Các endpoint API được quyền này bảo vệ (mô tả, cách nhau bằng dấu phẩy).</summary>
    public string? Endpoints { get; set; }

    /// <summary>Thứ tự hiển thị trong ma trận quyền (bằng giá trị số của mã).</summary>
    public int SortOrder { get; set; }
}
