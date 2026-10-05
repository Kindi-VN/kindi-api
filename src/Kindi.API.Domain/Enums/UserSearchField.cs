namespace Kindi.API.Domain.Enums;

/// <summary>
/// Trường được tìm trong tham số <c>searchField</c> của danh sách người dùng (màn quản trị).
/// Client truyền đúng tên (không phân biệt hoa/thường); bỏ trống thì tìm trên nhiều trường như trước.
/// </summary>
public enum UserSearchField
{
    /// <summary>Tên đăng nhập.</summary>
    Username,

    /// <summary>Họ tên người dùng.</summary>
    FullName,

    /// <summary>Mã người dùng.</summary>
    UserCode,

    /// <summary>Mã chia sẻ riêng của tài khoản (dùng gắn vào link chia sẻ).</summary>
    ReferralCode,

    /// <summary>Mã chia sẻ của người đã mang tài khoản này tới hệ thống.</summary>
    AccountReferrerCode,

    /// <summary>Số điện thoại.</summary>
    Phone,

    /// <summary>Email.</summary>
    Email
}
