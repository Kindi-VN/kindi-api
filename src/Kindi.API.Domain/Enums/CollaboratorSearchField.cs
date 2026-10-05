namespace Kindi.API.Domain.Enums;

/// <summary>
/// Trường được tìm trong tham số <c>searchField</c> của danh sách cộng tác viên (màn quản trị).
/// Client truyền đúng tên (không phân biệt hoa/thường); bỏ trống thì tìm trên nhiều trường như trước.
/// </summary>
public enum CollaboratorSearchField
{
    /// <summary>Họ tên CTV (lấy từ bảng Users).</summary>
    FullName,

    /// <summary>Mã hiển thị của hồ sơ CTV (CTV-…).</summary>
    CollaboratorCode,

    /// <summary>Mã người dùng của tài khoản CTV.</summary>
    UserCode,

    /// <summary>Mã chia sẻ riêng của hồ sơ CTV.</summary>
    ReferralCode,

    /// <summary>Mã chia sẻ của người đã mang tài khoản CTV này tới hệ thống.</summary>
    AccountReferrerCode,

    /// <summary>Số điện thoại CTV (lấy từ bảng Users).</summary>
    Phone,

    /// <summary>Email CTV (lấy từ bảng Users).</summary>
    Email,

    /// <summary>Lĩnh vực kinh doanh — khớp cả cột denormalized lẫn tên trong bảng BusinessFields.</summary>
    BusinessFieldName
}
