using Kindi.API.Application.Common.Models;
using Kindi.API.Domain.Enums;

namespace Kindi.API.Application.DTOs.requests;

/// <summary>
/// Bộ lọc danh sách người dùng cho màn quản trị.
/// </summary>
public class UserQueryDto : PagedRequest
{
    /// <summary>Từ khoá tìm theo tên đăng nhập, họ tên, số điện thoại, email hoặc mã người dùng.</summary>
    public string? Search { get; set; }

    /// <summary>
    /// Chỉ tìm theo đúng một trường (không OR lan sang cột khác); bỏ trống = tìm nhiều trường như trước.
    /// Giá trị hợp lệ: username, fullName, userCode, referralCode, accountReferrerCode, phone, email.
    /// </summary>
    public UserSearchField? SearchField { get; set; }
}
