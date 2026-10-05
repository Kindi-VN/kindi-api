namespace Kindi.API.Application.Common.Interfaces;

using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;

/// <summary>
/// Quyền của hệ thống: đọc danh mục (enum <see cref="PermissionCode"/>), quyền đang bật của từng role
/// và cấu hình quyền riêng của từng tài khoản. SuperAdmin luôn có toàn quyền — không lưu ở bảng quyền.
/// </summary>
public interface IPermissionService
{
    /// <summary>Danh mục toàn bộ quyền (đọc từ enum) — nguồn cho màn quản lý quyền và seed DB.</summary>
    IReadOnlyList<PermissionDefinition> GetCatalog();

    /// <summary>Danh sách nhóm quyền (đọc từ bảng PermissionGroups) kèm tên hiển thị và thứ tự.</summary>
    Task<IReadOnlyList<PermissionGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cây quyền đệ quy (Nhóm → Màn hình → Hành động) kèm trạng thái cấp của một vai trò
    /// (<paramref name="role"/> null = mặc định Admin; SuperAdmin = toàn bộ). Mỗi node trả <c>isGranted</c>
    /// (tick trực tiếp) và <c>isEffective</c> (hiệu lực sau kế thừa) để UI tô mờ phần con khi cha bị tắt.
    /// </summary>
    Task<IReadOnlyList<PermissionTreeNodeResponse>> GetTreeAsync(int? role, CancellationToken cancellationToken = default);

    /// <summary>Quyền đang bật của một role kèm phiên bản quyền (SuperAdmin: toàn bộ danh mục).</summary>
    Task<RolePermissions> GetRolePermissionsAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật quyền cho một role (chỉ role được gán — SuperAdmin bị từ chối).</summary>
    Task SetRolePermissionsAsync(UserRole role, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);

    /// <summary>Quyền hiệu lực của một tài khoản: quyền của role ghép với cấu hình riêng của tài khoản.</summary>
    Task<UserPermissions> GetUserPermissionsAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>Thông tin phục vụ màn cấu hình quyền riêng cho một tài khoản.</summary>
    Task<UserPermissionDetail?> GetUserPermissionDetailAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cấu hình quyền riêng cho danh sách tài khoản: chỉ lưu phần khác biệt so với quyền của role,
    /// nhờ vậy khi quyền role thay đổi thì tài khoản không bị lệch cấu hình.
    /// </summary>
    Task SetUserPermissionsAsync(IReadOnlyCollection<Guid> userIds, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);

    /// <summary>Tìm tài khoản để chọn khi cấu hình quyền riêng (mặc định bỏ qua tài khoản SuperAdmin).</summary>
    Task<IReadOnlyList<UserPermissionCandidate>> SearchUsersAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Xóa cache quyền (sau khi sửa cấu hình quyền).</summary>
    void InvalidateCache();
}

/// <summary>Quyền của một role: danh sách mã P### được bật và phiên bản quyền hiện tại.</summary>
public sealed record RolePermissions(IReadOnlySet<string> Codes, long Version)
{
    public static readonly RolePermissions Empty = new(new HashSet<string>(), 0);
}

/// <summary>Quyền hiệu lực của một tài khoản và phần cấu hình riêng so với role.</summary>
public sealed record UserPermissions(
    IReadOnlySet<string> Codes,
    IReadOnlySet<string> GrantedCodes,
    IReadOnlySet<string> DeniedCodes,
    long Version)
{
    public static readonly UserPermissions Empty = new(new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), 0);
}

/// <summary>Tài khoản chọn được ở màn cấu hình quyền riêng.</summary>
public sealed record UserPermissionCandidate(Guid Id, string Username, string FullName, UserRole Role);

/// <summary>Chi tiết quyền của một tài khoản: quyền của role, phần bật thêm, phần tắt riêng và quyền hiệu lực.</summary>
public sealed record UserPermissionDetail(
    Guid UserId,
    string Username,
    string FullName,
    UserRole Role,
    IReadOnlySet<string> RoleCodes,
    IReadOnlySet<string> GrantedCodes,
    IReadOnlySet<string> DeniedCodes,
    IReadOnlySet<string> EffectiveCodes);
