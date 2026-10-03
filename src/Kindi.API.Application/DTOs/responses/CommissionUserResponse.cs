namespace Kindi.API.Application.DTOs.responses;

/// <summary>Tài khoản chọn được khi cấu hình hoa hồng riêng.</summary>
public class CommissionUserResponse
{
    public Guid Id { get; set; }

    /// <summary>Tên đăng nhập.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Họ tên.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Vai trò (giá trị số của <c>UserRole</c>).</summary>
    public string Role { get; set; } = string.Empty;
}
