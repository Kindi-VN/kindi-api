namespace Kindi.API.Domain.Entities;

/// <summary>
/// Cấu hình quyền riêng cho một tài khoản: bật thêm hoặc tắt bớt so với quyền của role.
/// Chỉ tạo dòng khi khác với quyền của role — nhờ vậy khi quyền role thay đổi, tài khoản vẫn đi theo role.
/// </summary>
public class UserPermission : BaseEntity
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid PermissionId { get; set; }

    public Permission? Permission { get; set; }

    /// <summary>true = bật thêm cho tài khoản, false = tắt riêng tài khoản này.</summary>
    public bool IsGranted { get; set; } = true;
}
