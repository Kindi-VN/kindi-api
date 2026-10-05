namespace Kindi.API.Domain.Enums;

/// <summary>
/// Trường được tìm trong tham số <c>searchField</c> của danh sách đối tác (màn quản trị) và
/// nguồn cung công khai. Client truyền đúng tên (không phân biệt hoa/thường); bỏ trống thì tìm
/// trên nhiều trường như trước.
/// </summary>
public enum PartnerSearchField
{
    /// <summary>Họ tên người liên hệ (lấy từ bảng Users).</summary>
    FullName,

    /// <summary>Mã hiển thị của hồ sơ đối tác (PART-…).</summary>
    PartnerCode,

    /// <summary>Mã người dùng của tài khoản đối tác.</summary>
    UserCode,

    /// <summary>Mã chia sẻ của hồ sơ đối tác.</summary>
    ReferralCode,

    /// <summary>Mã chia sẻ của người đã mang tài khoản đối tác này tới hệ thống.</summary>
    AccountReferrerCode,

    /// <summary>Số điện thoại người liên hệ (lấy từ bảng Users).</summary>
    Phone,

    /// <summary>Email người liên hệ (lấy từ bảng Users).</summary>
    Email,

    /// <summary>Tên công ty/doanh nghiệp.</summary>
    CompanyName,

    /// <summary>Mã số thuế công ty (cột CompanyTax).</summary>
    CompanyTaxCode
}
