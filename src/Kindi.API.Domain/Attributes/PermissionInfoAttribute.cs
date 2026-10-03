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
}
