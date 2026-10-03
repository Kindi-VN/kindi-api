namespace Kindi.API.Application.Common.Interfaces;

using Kindi.API.Domain.Enums;

/// <summary>
/// Quyền của hệ thống: đọc danh mục (enum <see cref="PermissionCode"/>) và quyền đang bật của từng role.
/// SuperAdmin luôn có toàn quyền — không lưu ở bảng RolePermissions.
/// </summary>
public interface IPermissionService
{
    /// <summary>Danh mục toàn bộ quyền (đọc từ enum) — nguồn cho màn quản lý quyền và seed DB.</summary>
    IReadOnlyList<PermissionDefinition> GetCatalog();

    /// <summary>Quyền đang bật của một role kèm phiên bản quyền (SuperAdmin: toàn bộ danh mục).</summary>
    Task<RolePermissions> GetRolePermissionsAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật quyền cho một role (chỉ role được gán — SuperAdmin bị từ chối).</summary>
    Task SetRolePermissionsAsync(UserRole role, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);

    /// <summary>Xóa cache quyền (sau khi sửa ma trận).</summary>
    void InvalidateCache();
}

/// <summary>Quyền của một role: danh sách mã P### được bật và phiên bản quyền hiện tại.</summary>
public sealed record RolePermissions(IReadOnlySet<string> Codes, long Version)
{
    public static readonly RolePermissions Empty = new(new HashSet<string>(), 0);
}
