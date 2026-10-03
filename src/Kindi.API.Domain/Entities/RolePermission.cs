namespace Kindi.API.Domain.Entities;

using Kindi.API.Domain.Enums;

/// <summary>
/// Quyền được gán cho một role (ma trận role × quyền). SuperAdmin không lưu ở đây vì luôn có toàn quyền.
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>Role được gán — chỉ nhận User/Partner/Admin.</summary>
    public UserRole Role { get; set; }

    public Guid PermissionId { get; set; }

    public Permission? Permission { get; set; }

    /// <summary>Bật/tắt quyền này cho role (giữ bản ghi để còn vết cấu hình).</summary>
    public bool IsGranted { get; set; } = true;
}
