namespace Kindi.API.Application.Common.Helpers;

/// <summary>
/// Quy tắc hiển thị thông tin cá nhân lấy từ bảng <c>Users</c>.
/// </summary>
public static class UserInfo
{
    /// <summary>
    /// Email tạm mà hệ thống sinh cho tài khoản tạo tự động (<c>{sđt}@temp.com</c>) —
    /// coi như người dùng chưa nhập email, không trả ra UI.
    /// </summary>
    public static bool IsPlaceholderEmail(string? email, string? phone)
        => !string.IsNullOrWhiteSpace(email)
           && !string.IsNullOrWhiteSpace(phone)
           && email.Trim().Equals($"{phone.Trim()}@temp.com", StringComparison.OrdinalIgnoreCase);

    /// <summary>Email để trả ra UI (email tạm trả về null).</summary>
    public static string? DisplayEmail(string? email, string? phone)
        => IsPlaceholderEmail(email, phone) ? null : email;
}
