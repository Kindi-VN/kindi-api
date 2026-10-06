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

    /// <summary>
    /// Tab tài khoản trên màn quản lý: bỏ trống = tất cả; <c>Admin</c> = chỉ tài khoản quản trị;
    /// <c>Customer</c> = tài khoản thường (khách hàng/CTV/đối tác, không gồm quản trị).
    /// </summary>
    public UserAccountScope? Scope { get; set; }
}

/// <summary>Phạm vi tài khoản cho tab ở màn quản lý người dùng.</summary>
public enum UserAccountScope
{
    /// <summary>Tài khoản thường: mọi vai trò trừ quản trị.</summary>
    Customer = 1,

    /// <summary>Tài khoản quản trị (role Admin).</summary>
    Admin = 2
}
