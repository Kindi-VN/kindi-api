namespace Kindi.API.Domain.Attributes;

using Kindi.API.Domain.Enums;

/// <summary>
/// Thông tin của một quyền: tên hiển thị, nhóm chức năng, loại (view/thao tác), route UI và các
/// endpoint API mà quyền đó bảo vệ. Mã P### suy ra từ giá trị số của member (xem
/// <see cref="PermissionCodeExtensions.ToCode"/>) nên không thể lệch với danh mục.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class PermissionInfoAttribute : Attribute
{
    public PermissionInfoAttribute(
        string name,
        PermissionModule module,
        PermissionKind kind,
        string? route = null,
        string? endpoints = null)
    {
        Name = name;
        Module = module;
        Kind = kind;
        Route = route;
        Endpoints = endpoints;
    }

    public string Name { get; }

    public PermissionModule Module { get; }

    public PermissionKind Kind { get; }

    /// <summary>Route UI được quyền này mở (quyền dạng view).</summary>
    public string? Route { get; }

    /// <summary>Các endpoint API được quyền này bảo vệ, cách nhau bằng dấu phẩy.</summary>
    public string? Endpoints { get; }

    /// <summary>
    /// Mã nhóm quyền (<see cref="Enums.PermissionGroupCodes"/>) — chỉ khai khi nhóm suy ra từ module
    /// không còn đúng (ví dụ quyền hoa hồng, giải ngân nằm trong nhóm COMMISSION).
    /// </summary>
    public string? ParentCode { get; set; }

    /// <summary>
    /// Mã màn hình (<see cref="Enums.PermissionScreens"/>) mà quyền thuộc về — gom các hành động
    /// Xem/Sửa/Xoá của cùng một màn hình khi render ma trận quyền.
    /// </summary>
    public string? Screen { get; set; }

    /// <summary>
    /// Các mã quyền cũ bị mã này thay thế khi tách nhỏ quyền (ví dụ mã "Xoá / khôi phục" tách thành
    /// mã xoá và mã khôi phục). Seeder dựa vào đây để chuyển quyền đã cấp cho role/tài khoản sang mã mới,
    /// bảo đảm không ai mất quyền đang có.
    /// </summary>
    public string[] Replaces { get; set; } = Array.Empty<string>();
}
