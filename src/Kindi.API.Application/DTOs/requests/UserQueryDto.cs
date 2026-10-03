using Kindi.API.Application.Common.Models;

namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Bộ lọc danh sách người dùng cho màn quản trị.
/// </summary>
public class UserQueryDto : PagedRequest
{
    /// <summary>Từ khoá tìm theo tên đăng nhập, họ tên, số điện thoại, email hoặc mã người dùng.</summary>
    public string? Search { get; set; }
}
