namespace Kindi.API.Domain.Enums;

/// <summary>
/// Trường được tìm trong tham số <c>searchField</c> của các danh sách yêu cầu.
/// Client truyền đúng tên (không phân biệt hoa/thường); bỏ trống thì tìm trên mọi trường.
/// </summary>
public enum RequestSearchField
{
    /// <summary>Tên sản phẩm của yêu cầu.</summary>
    ProductName,

    /// <summary>Mã hiển thị của yêu cầu (OFR-… / PRQ-…).</summary>
    Code,

    /// <summary>Mã chia sẻ ghi nhận trên bản ghi (người mang khách tới yêu cầu này).</summary>
    RecordReferrerCode,

    /// <summary>Họ tên người tạo yêu cầu (lấy từ bảng Users).</summary>
    CustomerName,

    /// <summary>Số điện thoại người tạo yêu cầu (lấy từ bảng Users).</summary>
    CustomerPhone,

    /// <summary>Email người tạo yêu cầu (lấy từ bảng Users).</summary>
    CustomerEmail
}
