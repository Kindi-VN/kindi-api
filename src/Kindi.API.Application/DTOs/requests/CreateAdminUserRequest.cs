namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Tạo tài khoản quản trị từ màn quản lý người dùng. Chỉ tạo được role Admin — tài khoản
/// SuperAdmin cấp phát thủ công (xem <c>RoleRules</c>).
/// </summary>
public class CreateAdminUserRequest
{
    /// <summary>Tên đăng nhập (không trùng với tài khoản nào, kể cả tài khoản đã xoá mềm).</summary>
    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    /// <summary>Mật khẩu ban đầu người quản trị nhập tay. Người dùng KHÔNG bị bắt đổi ở lần đăng nhập đầu.</summary>
    public string Password { get; set; } = string.Empty;
}
